using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading;

namespace Deployer.Logic
{
    public class BaseController
    {
        protected static Properties.Settings Sett
        {
            get { return Properties.Settings.Default; }
        }

        public delegate void NoticeDelegate(string message);
        public event NoticeDelegate NoticeEvent;

        protected void onNoticeEvent(string message)
        {
            LogNotice(message);

            if (NoticeEvent != null)
                NoticeEvent(message);
        }

        protected void onNoticeEvent(string format, params object[] args)
        {
            onNoticeEvent(string.Format(format, args));
        }

        protected void processExisting(string watchPath, string filter, FileSystemEventHandler handler)
        {
            foreach (string s in Directory.GetFiles(watchPath, filter, SearchOption.AllDirectories))
            {
                var finfo = new FileInfo(s);
                handler.Invoke(s, new FileSystemEventArgs(WatcherChangeTypes.Created, finfo.DirectoryName, finfo.Name));
            }
        }


        private static Mutex _fileSystemMutex = new Mutex(false, "DeployerMutexxx");
        private static void LogNotice(string message)
        {
            try
            {
                _fileSystemMutex.WaitOne();

                string logPath = string.Format("{0}\\{1}.txt", Properties.Settings.Default.LogPath, DateTime.Now.ToString("yyyy-MM-dd"));

                using (var sw = new StreamWriter(logPath, true))
                    sw.WriteLine("{0} - {1}", DateTime.Now, message);
            }
            finally
            {
                _fileSystemMutex.ReleaseMutex();
            }
        }
    }
}
