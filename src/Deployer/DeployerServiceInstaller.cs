using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Configuration.Install;
using System.ServiceProcess;

namespace Deployer
{
    [RunInstaller(true)]
    public partial class DeployerServiceInstaller : Installer
    {
        public DeployerServiceInstaller()
        {
            InitializeComponent();

            serviceInstaller1.AfterInstall += serviceInstaller1_AfterInstall;
        }

        void serviceInstaller1_AfterInstall(object sender, InstallEventArgs e)
        {
            using (var sc = new ServiceController(serviceInstaller1.ServiceName))
            {
                sc.Start();
            }
        }
    }
}