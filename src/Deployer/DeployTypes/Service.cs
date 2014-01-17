using System;
using System.Collections.Generic;
using System.Text;
using System.Configuration.Install;
using System.ServiceProcess;
using System.Xml;
using System.IO;
using System.Threading;
using Deployer.Logic;

namespace Deployer.DeployTypes
{
    class Service : XCopy
    {
        public Service(XmlNode configNode) : base(configNode)
        {
            _serviceName = configNode.SelectSingleNode("serviceName").InnerXml;
            _serviceMachine = XmlUtil.ParseStringNode(configNode.SelectSingleNode("serviceMachine"), ".");

            // find service
            foreach (ServiceController sc in ServiceController.GetServices(_serviceMachine))
            {
                if (sc.ServiceName == _serviceName)
                    _serviceController = sc;
            }
        }

        private ServiceController _serviceController;
        private string _serviceName;
        private string _serviceMachine;
        private bool LocalMachineService
        {
            get { return _serviceMachine == "." || _serviceMachine.ToLower() == Environment.MachineName.ToLower(); }
        }

        public override void Execute(System.IO.DirectoryInfo sourceDirectory)
        {
            // if service was found during initialization stop it
			if (_serviceController != null && _serviceController.CanStop)
			{
				_serviceController.Stop();
				_serviceController.WaitForStatus(ServiceControllerStatus.Stopped);

				// it seems that wait for status doesn't work properly when stopping service on remote machine
				// in any case to make sure that it is stopped we wait a bit
				Thread.Sleep(5000);
			}
			else
			{
				if (_serviceController != null)
				{
					onDeployNotice("Can't stop service '{0}'.", _serviceName);
				}
				else
				{
                    onDeployNotice("Service '{0}' does not exist.", _serviceName);
				}
			}

            base.Execute(sourceDirectory);

            // if service was not found install it
            if (_serviceController == null && LocalMachineService)
            {
                onDeployNotice("Instaling service '{0}'.", _serviceName);

                ManagedInstallerClass.InstallHelper(new string[] { "/i", FindServiceExe(TargetDirectory).FullName });
                _serviceController = new ServiceController(_serviceName);
            }
            else if (_serviceController == null && !LocalMachineService)
            {
                onDeployNotice("Can't install and start service '{0}', on remote machine '{1}'. You need to register service by hand :(", _serviceName, _serviceMachine);
            }
            // TODO: if service was found but target path is different, reinstall it


            if (_serviceController != null)
                _serviceController.Start();
        }

        private FileInfo FindServiceExe(DirectoryInfo directory)
        {
            foreach (FileInfo fi in directory.GetFiles("*.exe"))
            {
                // return first exe file
                if (fi.Name.ToLower().IndexOf(".vshost.") == -1)
                    return fi;
            }

            throw new Exception("Could not find exe file... Service deploy must have .exe file");
        }
    }
}
