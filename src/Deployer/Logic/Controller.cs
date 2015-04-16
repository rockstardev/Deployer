using System;
using System.IO;
using System.Threading;
using System.Xml;
using UsefulHeap.Zip;
using UsefulHeap.Rar;
using Deployer.DeployTypes;

namespace Deployer.Logic
{
    class Controller : BaseController
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


        /// <summary>
        /// Processes existing zips and starts listener
        /// </summary>
        /// <param name="o">foo</param>
        private void init(object o)
        {
            string watchPath = Sett.DropPath;
            var handler = new FileSystemEventHandler(_fsw_Created);

            _fswZip = new FileSystemWatcher(watchPath);
            _fswZip.Created += handler;
            _fswZip.Filter = "*.zip";

            _fswRar = new FileSystemWatcher(watchPath);
            _fswRar.Created += handler;
            _fswRar.Filter = "*.rar";

            processExisting(watchPath, _fswZip.Filter, handler);
            processExisting(watchPath, _fswRar.Filter, handler);

            _fswZip.EnableRaisingEvents = true;
            _fswRar.EnableRaisingEvents = true;
        }

        /// <summary>
        /// Initializes deployer directories
        /// </summary>
        private void initPaths()
        {
            initPaths(
                Sett.DropPath,
                Sett.ArchivePath,
                Sett.DllCache,
                Sett.LogPath,
                Sett.BackupPath,
                Sett.ErrorPath,
                Sett.TransferPath);
        }

        private void initPaths(params string[] paths)
        {
            for (int i = 0; i < paths.Length; i++)
            {
                var path = paths[i];
                if (!Directory.Exists(path))
                    Directory.CreateDirectory(path);
            }
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
                        Sett.ErrorPath, 
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
                string extractPath = String.Format("{0}\\{1}-{2}", Sett.ArchivePath, fi.Name.Replace(fi.Extension, ""), DateTime.Now.ToString("yyyy-MM-dd-HH-mm-ss"));


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
                            deploy = new XCopy(node);
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

                    var movePath = String.Format("{0}\\{1}", Sett.ArchivePath, fi.Name);
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
