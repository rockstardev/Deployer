using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
using System.Threading;
using System.Xml;
using UsefulHeap.Zip;
using UsefulHeap.Rar;
using Deployer.DeployTypes;

namespace Deployer.Logic
{
    class Controller
    {
        public Controller()
        {
            initPaths();

            BaseDeployType.DeployNotice += new BaseDeployType.DeployNoticeHandler(BaseDeployType_DeployNotice);

            // invoke async so that main thread is not blocked
            ThreadPool.QueueUserWorkItem(new WaitCallback(init));
        }

        void BaseDeployType_DeployNotice(string message)
        {
            onNoticeEvent(message);
        }

        //atribs
        private FileSystemWatcher _fswZip;
        private FileSystemWatcher _fswRar;
        private object _deploySync = new object();
        public delegate void NoticeDelegate(string message);
        public event NoticeDelegate NoticeEvent;

        private void onNoticeEvent(string message)
        {
            if (NoticeEvent != null)
                NoticeEvent(message);
        }

        private void onNoticeEvent(string format, params object[] args)
        {
            onNoticeEvent(string.Format(format, args));
        }


        /// <summary>
        /// Processes existing zips and starts listener
        /// </summary>
        /// <param name="o">foo</param>
        private void init(object o)
        {
            string watchPath = Properties.Settings.Default.DropPath;

            _fswZip = new FileSystemWatcher(watchPath);
            _fswZip.Created += new FileSystemEventHandler(_fsw_Created);
            _fswZip.Filter = "*.zip";

            _fswRar = new FileSystemWatcher(watchPath);
            _fswRar.Created += new FileSystemEventHandler(_fsw_Created);
            _fswRar.Filter = "*.rar";

            _fswZip.EnableRaisingEvents = true;
            _fswRar.EnableRaisingEvents = true;

            processExisting(watchPath);
        }

        /// <summary>
        /// Processes install zips waiting in dump location
        /// </summary>
        /// <param name="watchPath"></param>
        private void processExisting(string watchPath)
        {
            foreach (string s in Directory.GetFiles(watchPath, "*.zip"))
                processPackage(s);
            foreach (string s in Directory.GetFiles(watchPath, "*.rar"))
                processPackage(s);
        }

        /// <summary>
        /// Initializes deployer directories
        /// </summary>
        private void initPaths()
        {
            string path = Properties.Settings.Default.DropPath;
            if (!Directory.Exists(path))
                Directory.CreateDirectory(path);

            path = Properties.Settings.Default.ArchivePath;
            if (!Directory.Exists(path))
                Directory.CreateDirectory(path);

            path = Properties.Settings.Default.DllCache;
            if (!Directory.Exists(path))
                Directory.CreateDirectory(path);

            path = Properties.Settings.Default.LogPath;
            if (!Directory.Exists(path))
                Directory.CreateDirectory(path);

            path = Properties.Settings.Default.BackupPath;
            if (!Directory.Exists(path))
                Directory.CreateDirectory(path);

            path = Properties.Settings.Default.ErrorPath;
            if (!Directory.Exists(path))
                Directory.CreateDirectory(path);

        }

        void _fsw_Created(object sender, FileSystemEventArgs e)
        {
            processPackage(e.FullPath);
        }

        /// <summary>
        /// Processes package from drop path
        /// </summary>
        /// <param name="packagePath">full path to package to process</param>
        private void processPackage(string packagePath)
        {
            try { processPackage(packagePath, null); }
            catch (Exception ex) {
                onNoticeEvent("ERROR: {0}\n", ex.ToString());
                FileInfo fi = FileSystemUtil.TryToReadFile(packagePath);
                if (fi != null)
                {
                    fi.MoveTo(string.Format("{0}\\{1}-{2}{3}", 
                        Properties.Settings.Default.ErrorPath, 
                        fi.Name.Replace(fi.Extension, ""), 
                        DateTime.Now.ToString("yyyy-MM-dd-HH-mm"),
                        fi.Extension)
                    );
                }
            }
        }

        private void processPackage(string packagePath, object foo)
        {
            lock (_deploySync)
            {
                // File System Watcher is raising event when file is still locked
                try
                {
                    File.ReadAllBytes(packagePath);
                }
                catch
                {
                    Thread.Sleep(5000);
                    processPackage(packagePath);
                    return;
                }

				onNoticeEvent("STARTING deploy '{0}'", packagePath);

                FileInfo fi = new FileInfo(packagePath);
                string extractPath = String.Format("{0}\\{1}-{2}", Properties.Settings.Default.ArchivePath, fi.Name.Replace(fi.Extension, ""), DateTime.Now.ToString("yyyy-MM-dd-HH-mm-ss"));


                if (fi.Extension == ".zip")
                {
                    ZipDirectory.DecompressAtDirectory(fi.FullName, extractPath);
                }
                else if (fi.Extension == ".rar")
                {
                    RarDirectory.DecompressAtDirectory(fi.FullName, extractPath);
                }

                BaseDeployType deploy = null;
				FileInfo[] deployerFiles = new DirectoryInfo(extractPath).GetFiles("deployer.xml*", SearchOption.AllDirectories);

				if (deployerFiles == null || deployerFiles.Length == 0)
				{
					onNoticeEvent("There are no deployer.xml files!");
                    FileSystemUtil.DeleteFile(packagePath);
					return;
				}

                foreach (FileInfo deployerXml in deployerFiles)
                {
                    XmlDocument xd = new XmlDocument();
                    xd.LoadXml(File.ReadAllText(deployerXml.FullName));


                    string xPath = string.Format("/deployer/deployInfo[not(@machineName)] | /deployer/deployInfo[@machineName='{0}']", Environment.MachineName.ToLower());
                    foreach (XmlNode node in xd.SelectNodes(xPath))
                    {
                        string deployType = node.Attributes["type"].InnerXml.ToLower();

                        if (deployType == "xcopy")
                        {
                            var xcopy = new XCopy(node);
                            if (xcopy.TargetPath.Contains(","))
                            {
                                string[] paths = xcopy.TargetPath.Split(',');

                                for (int i = 1; i < paths.Length; i++)
                                {
                                    xcopy.TargetPath = paths[i];
                                    xcopy.Execute(deployerXml.Directory);
                                }

                                xcopy.TargetPath = paths[0];
                            }
                            deploy = xcopy;
                        }
                        else if (deployType == "service")
                            deploy = new Service(node);
                        else if (deployType == "clickonce")
                            deploy = new ClickOnce(node);
                        else if (deployType == "databasescript")
                            deploy = new DatabaseScript(node);
                        else
                            throw new Exception("Unknown deploy type");

                        

                        // deploy just once if lookFurther is false
                        bool lookFurther = XmlUtil.ParseBoolAttribute(node.Attributes["lookFurther"], true);

                        // if you set Copy To Output Directory true for ClickOnce deployments 
                        // your deployer.xml file will not be in root but in child directory 
                        // with other deployment files; 
                        // e.g. ClickOnceRoot/ClickOnceApp_1_0_0_21/deployer.xml.deploy
                        if (deployerXml.Name.EndsWith(".deploy"))
                        {
                            // "old" clickOnce
                            if (deployerXml.Directory.Parent.GetFiles("*.application").Length > 0)
                                deploy.Execute(deployerXml.Directory.Parent);
                            // VS2008 clickOnce
                            else if (deployerXml.Directory.Parent.Parent.GetFiles("*.application").Length > 0)
                                deploy.Execute(deployerXml.Directory.Parent.Parent);
                            else
                                throw new Exception("ClickOnce deployment must contain .application file");
                        }
                        else
                            deploy.Execute(deployerXml.Directory);


                        if (!lookFurther)
                            break;
                    }

                    var movePath = String.Format("{0}\\{1}", Properties.Settings.Default.ArchivePath, fi.Name);
                    if (File.Exists(movePath))
                        File.Delete(movePath);
                    
                    fi.MoveTo(movePath);
                }

                onNoticeEvent("DEPLOYED {0}\n", packagePath);
                // move instead of delete FileSystemUtil.DeleteFile(packagePath);

            }
        }
    }
}
