using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading;
using System.Windows.Forms;

namespace Deployer.Logic
{
    class AppOfflineMethods
    {
        private static string _appOfflineHtm = Application.StartupPath + "\\!SupportFiles\\AppOffline\\app_offline.htm";
        private static string _webConfig = Application.StartupPath + "\\!SupportFiles\\AppOffline\\web.config";

        public static void Copy(DirectoryInfo target)
        {
            FileSystemUtil.CopyFile(_appOfflineHtm, target.FullName + "\\app_offline.htm");
            FileSystemUtil.CopyFile(_webConfig, target.FullName + "\\web.config");
            // Allowing timeout for app to shut down 
            Thread.Sleep(2000);

            // for testing of copy
            //Thread.Sleep(30000);
        }

        public static void Delete(DirectoryInfo target)
        {
            FileSystemUtil.DeleteFile(target.FullName + "\\app_offline.htm");
        }
    }
}
