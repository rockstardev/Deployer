using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Configuration.Install;

namespace Deployer
{
    [RunInstaller(true)]
    public partial class DeployerServiceInstaller : Installer
    {
        public DeployerServiceInstaller()
        {
            InitializeComponent();
        }
    }
}