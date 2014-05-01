using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading;

namespace Deployer.Logic
{
    public class TransferController : BaseController
    {
        private FileSystemWatcher _fswTransfer;

        public TransferController()
        {
            string watchPath = Properties.Settings.Default.TransferPath;
            var handler = new FileSystemEventHandler(_fswTransfer_Created);

            _fswTransfer = new FileSystemWatcher(watchPath);
            _fswTransfer.Created += handler;
            _fswTransfer.IncludeSubdirectories = true;
            _fswTransfer.Filter = "*.transfer";

            processExisting(watchPath, _fswTransfer.Filter, handler);

            _fswTransfer.EnableRaisingEvents = true;
        }

        private void _fswTransfer_Created(object sender, FileSystemEventArgs e)
        {
            processPackage(e.FullPath);
        }

        private void processPackage(string packagePath)
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

            onNoticeEvent("TRANSFER START '{0}'", packagePath);

            var instructions = File.ReadAllLines(packagePath);
            var finfo = new FileInfo(packagePath);

            finfo.Delete();

            FileSystemUtil.CopyFolder(finfo.DirectoryName, instructions[0]);

            if (finfo.DirectoryName.TrimEnd('\\') != Sett.TransferPath.TrimEnd('\\'))
            {
                finfo.Directory.Delete(true);
            }
            else
            {
                FileSystemUtil.ClearFolder(finfo.Directory);
            }

            onNoticeEvent("TRANSFER END '{0}'", packagePath);
        }
    }
}
