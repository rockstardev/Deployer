using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
using System.Xml;
using Deployer.Logic;
using Deployer.DeployTypes.DomainClasses;

namespace Deployer.DeployTypes
{
    class XCopy : BaseDeployType
    {
        public XCopy() : base()
        {
            _backupInfo = new List<string>();
            _configReplaces = new List<ConfigReplacesRoot>();
            _dllCache = new List<DllCacheEntry>();
        }

        public XCopy(XmlNode configNode) : this()
        {
            XmlNode targetPathNode = configNode.SelectSingleNode("targetPath");
            TargetPath = targetPathNode.InnerXml.TrimEnd('\\');
            ClearFolderBeforeCopy = XmlUtil.ParseBoolAttribute(targetPathNode.Attributes["clear"], false);
            BackupFolderBeforeCopy = XmlUtil.ParseBoolAttribute(targetPathNode.Attributes["backup"], false);

            XmlNode backupInfoNode = configNode.SelectSingleNode("backupInfo");
            if (backupInfoNode != null)
            {
                foreach (XmlNode backupInfo in backupInfoNode.ChildNodes)
                {
                    BackupInfo.Add(backupInfo.Attributes["path"].InnerXml.TrimEnd('\\').Replace("~", TargetPath).ToLower());
                }
            }

            foreach (XmlNode configReplacesNode in configNode.SelectNodes("configReplaces"))
            {
                string searchExpression =
                    XmlUtil.ParseStringAttribute(configReplacesNode.Attributes["searchExpression"], "*.config");
                ConfigReplacesRoot crr = new ConfigReplacesRoot(searchExpression);
                crr.ReplaceOnSource = XmlUtil.ParseBoolAttribute(configReplacesNode.Attributes["replaceOnSource"], false);

                foreach (XmlNode node in configReplacesNode.ChildNodes)
                {
                    if (node.Attributes == null)
                        continue;

                    ConfigReplacesEntry entry = null;

                    if (node.Attributes["findStart"] != null && node.Attributes["findEnd"] != null)
                    {
                        entry = new ConfigReplacesEntry(
                            node.Attributes["findStart"].Value, node.Attributes["findEnd"].Value, node.InnerText);
                    }
                    else
                    {
                        entry = new ConfigReplacesEntry(
                            node.ChildNodes[0].InnerText, node.ChildNodes[1].InnerText);
                    }
                    crr.Entries.Add(entry);
                }

                this.ConfigReplaces.Add(crr);
            }

            foreach (XmlNode node in configNode.SelectNodes("dllCache/dll"))
            {
                DllCacheEntry entry = new DllCacheEntry(DllCacheEntryEnum.Dll,
                    node.Attributes["name"].InnerXml, node.Attributes["copyTo"].InnerXml);

                this.DllCache.Add(entry);
            }

            foreach (XmlNode node in configNode.SelectNodes("dllCache/folder"))
            {
                DllCacheEntry entry = new DllCacheEntry(DllCacheEntryEnum.Folder,
                    node.Attributes["name"].InnerXml, node.Attributes["copyTo"].InnerXml);

                this.DllCache.Add(entry);

            }
        }

        private string _targetPath;
        public string TargetPath
        {
            get { return _targetPath; }
            set { _targetPath = value; }
        }

        public DirectoryInfo TargetDirectory
        {
            get { return new DirectoryInfo(TargetPath); }
        }

        private bool _clearFolderBeforeCopy;
        public bool ClearFolderBeforeCopy
        {
            get { return _clearFolderBeforeCopy; }
            set { _clearFolderBeforeCopy = value; }
        }

        private bool _backupFolderBeforeCopy;
        public bool BackupFolderBeforeCopy
        {
            get { return _backupFolderBeforeCopy; }
            set { _backupFolderBeforeCopy = value; }
        }

        private List<string> _backupInfo;
        public List<string> BackupInfo
        {
            get { return _backupInfo; }
            set { _backupInfo = value; }
        }

        private List<ConfigReplacesRoot> _configReplaces;
        internal List<ConfigReplacesRoot> ConfigReplaces
        {
            get { return _configReplaces; }
            set { _configReplaces = value; }
        }

        private List<DllCacheEntry> _dllCache;
        internal List<DllCacheEntry> DllCache
        {
            get { return _dllCache; }
            set { _dllCache = value; }
        }

        public override void Execute(DirectoryInfo sourceDirectory)
        {
            Execute(sourceDirectory, true, true);
        }

        protected void Execute(DirectoryInfo sourceDirectory, bool replaceConfigValues, bool copyDllCache)
        {
			onDeployNotice("Source directory: '{0}'", sourceDirectory.FullName);
			onDeployNotice("Target directory: '{0}'", TargetDirectory.FullName);

            if (!TargetDirectory.Exists)
                TargetDirectory.Create();
            else if (ClearFolderBeforeCopy)
                FileSystemUtil.ClearFolder(TargetDirectory);

            if (BackupFolderBeforeCopy)
            {
                //foreach (var bi in BackupInfo)
                //    bi.Replace("~", TargetPath).ToLower();

                FileSystemUtil.CopyFolderWithExclude(TargetPath,
                    string.Format("{0}\\{1}", Properties.Settings.Default.BackupPath, sourceDirectory.Name),
                    BackupInfo);
            } 
            
            if (replaceConfigValues)
            {
                foreach (ConfigReplacesRoot crr in ConfigReplaces)
                {
                    if (crr.ReplaceOnSource)
                        ReplaceConfigValues(sourceDirectory, crr);
                }
            }

            FileSystemUtil.CopyFolder(sourceDirectory.FullName, TargetPath);

            if (replaceConfigValues)
            {
                foreach (ConfigReplacesRoot crr in ConfigReplaces)
                {
                    if (!crr.ReplaceOnSource)
                        ReplaceConfigValues(TargetDirectory, crr);
                }
            }

            if (copyDllCache)
                CopyDllsFromCache();

            File.Delete(TargetPath + "\\deployer.xml");
        }

        protected virtual void CopyDllsFromCache()
        {
            foreach (DllCacheEntry dce in DllCache)
                dce.CopyTo = dce.CopyTo.Replace("{targetDirectory}", TargetPath);

            foreach (DllCacheEntry dce in DllCache)
            {
                string source = string.Format("{0}\\{1}", Properties.Settings.Default.DllCache, dce.Name);
                string destination = string.Format("{0}\\{1}", dce.CopyTo, dce.Name);
                if (dce.Type == DllCacheEntryEnum.Dll)
                {
                    if (!File.Exists(source))
                    {
                        throw new Exception(string.Format(
                            "Specified dll [{0}] does not exist in cache. Can't continue with deployment", dce.Name));
                    }
                    
                    FileSystemUtil.CopyFile(source, destination);
                }
                else if (dce.Type == DllCacheEntryEnum.Folder)
                {
                    if (!Directory.Exists(source))
                    {
                        throw new Exception(string.Format(
                               "Specified folder [{0}] does not exist in cache. Can't continue with deployment", dce.Name));
                    }

                    FileSystemUtil.CopyFolder(source, destination);
                }
            }
        }

        // Configuration methods

        //public void ReplaceConfigValues()
        //{
        //    ReplaceConfigValues(TargetDirectory);
        //}

        public void ReplaceAllConfigValuesInDirectory(DirectoryInfo directory)
        {
            foreach (ConfigReplacesRoot crr in ConfigReplaces)
                ReplaceConfigValues(directory, crr);
        }

        public void ReplaceConfigValues(DirectoryInfo directory, ConfigReplacesRoot configReplaces)
        {
            foreach (string f in Directory.GetFiles(directory.FullName, configReplaces.SearchExpression, SearchOption.AllDirectories))
            {
                StringBuilder file = null;
                using (StreamReader sr = new StreamReader(f))
                {
                    file = new StringBuilder(sr.ReadToEnd());
                }

                foreach (ConfigReplacesEntry cre in configReplaces.Entries)
                {
                    if (cre.IsSimpleFindReplace)
                        file.Replace(cre.Find, cre.Replace);
                    else
                    {
                        string operate = file.ToString();

                        int start = operate.IndexOf(cre.FindStart);
                        if (start > -1)
                        {
                            int end = operate.IndexOf(cre.FindEnd);

                            file.Remove(start, end + cre.FindEnd.Length - start);
                            file.Insert(start, cre.Replace);
                        }
                    }
                }

                using (StreamWriter sw = new StreamWriter(f, false))
                {
                    sw.Write(file.ToString());
                }
            }
        }
    }
}
