using System;
using System.Collections.Generic;
using System.IO;
using System.ServiceProcess;
using System.Windows.Forms;

using Deployer.Logic;
using System.Threading;

namespace Deployer
{
    static class Program
    {
        private static Controller _ctrl;

        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main()
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);

            _fileSystemMutex = new Mutex(false, "DeployerMutexxx");

            _ctrl = new Controller();
            _ctrl.NoticeEvent += new Controller.NoticeDelegate(_ctrl_NoticeEvent);

#if DEBUG
            Application.Run(new MainForm());
#else
            ServiceBase.Run(new DeployerService());
#endif
        }

        private static Mutex _fileSystemMutex;
        static void _ctrl_NoticeEvent(string message)
        {
            try
            {
                _fileSystemMutex.WaitOne();

                string logPath = string.Format("{0}\\{1}.txt", Properties.Settings.Default.LogPath, DateTime.Now.ToString("yyyy-MM-dd"));

                using (StreamWriter sw = new StreamWriter(logPath, true))
                    sw.WriteLine("{0} - {1}", DateTime.Now, message);
            }
            finally
            {
                _fileSystemMutex.ReleaseMutex();
            }
        }
    }
}