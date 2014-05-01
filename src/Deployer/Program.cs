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
        private static TransferController _ctrlTransfer;

        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main()
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);

            _ctrl = new Controller();
            _ctrlTransfer = new TransferController();

#if DEBUG
            Application.Run(new MainForm());
#else
            ServiceBase.Run(new DeployerService());
#endif
        }
    }
}