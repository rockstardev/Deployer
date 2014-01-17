using System;
using System.Collections.Generic;
using System.Text;
using System.Xml;
using System.IO;
using System.Diagnostics;
using System.Windows.Forms;
using System.Threading;
using Deployer.Logic;
using Deployer.DeployTypes.DomainClasses;

namespace Deployer.DeployTypes
{
    class ClickOnce : XCopy
    {
        public ClickOnce(XmlNode configNode) : base(configNode)
        {
            ProviderUrl = XmlUtil.ParseStringNode(configNode.SelectSingleNode("providerUrl"), "").TrimEnd('\\');
            XmlNode usePackedKeyNode = configNode.SelectSingleNode("usePackedKey");

            if (usePackedKeyNode != null)
            {
                UsePackedKey = bool.Parse(usePackedKeyNode.InnerXml);

                if (UsePackedKey)
                {
                    if (usePackedKeyNode.Attributes["keyPassword"] != null)
                        KeyPassword = usePackedKeyNode.Attributes["keyPassword"].InnerXml;
                    else
                        throw new Exception("You must specify key password if you UsePackedKey is set to True");
                }
            }
        }

        private bool _usePackedKey;
        public bool UsePackedKey
        {
            get { return _usePackedKey; }
            set { _usePackedKey = value; }
        }

        private string _keyPassword = "123";
        public string KeyPassword
        {
            get { return _keyPassword; }
            set { _keyPassword = value; }
        }

        private string _providerUrl;
        public string ProviderUrl
        {
            get { return _providerUrl; }
            set { _providerUrl = value; }
        }

        private string _keyPath = string.Format("\"{0}\\{1}\"", Application.StartupPath, "clickOnceKey.pfx");
        private string _magePath = string.Format("\"{0}\\{1}\"", Application.StartupPath, "mage.exe");

        public override void Execute(DirectoryInfo sourceDirectory)
        {
            // remove .deploy extension so that we can change files
            DirectoryInfo di = FindManifest(sourceDirectory).Directory;
            RemoveDeployFilesExtension(di);

            FileInfo targetManifest = FindManifest(sourceDirectory);
            if (UsePackedKey)
            {
                _keyPath = FindPackedKey(sourceDirectory).FullName;
            }

            // replace config values and copy common dlls
            ReplaceAllConfigValuesInDirectory(sourceDirectory);
            foreach (DllCacheEntry dce in DllCache)
                dce.CopyTo = dce.CopyTo.Replace("{manifestDirectory}", targetManifest.Directory.FullName);
            CopyDllsFromCache();

            // refresh hashes in manifest: mage -u MyApp.exe.manifest
            StartProcess(_magePath, string.Format("-u \"{0}\"", targetManifest.FullName));
            // sign manifest: mage -s MyApp.exe.manifest -cf mycertifcate.pfx -pwd password
            StartProcess(_magePath, string.Format("-s \"{0}\" -cf {1} -pwd {2}", targetManifest.FullName, _keyPath, KeyPassword));
            
            FileInfo targetApplication = FindAppManifest(sourceDirectory);
            // update deployment manifest: mage -u ..\MyApp.application -appm MyApp.exe.manifest -pu \\networkshare\app\main.application
            StartProcess(_magePath, string.Format("-u \"{0}\" -appm \"{1}\" -pu \"{2}\\{3}\"", targetApplication.FullName, targetManifest.FullName, ProviderUrl, targetApplication.Name));
            // sign deployment manifest: mage -s ..\MyApp.application -cf mycertifcate.pfx -pwd password
            StartProcess(_magePath, string.Format("-s \"{0}\" -cf {1} -pwd {2}", targetApplication.FullName, _keyPath, KeyPassword));

            // update setup.exe specifically
            UpdateBootstrapperIfExists(sourceDirectory, ProviderUrl);
            
            // restore .deploy extensions so everything will work ok            
            AddDeployFilesExtension(targetManifest.Directory);

            base.Execute(sourceDirectory, false, false);
        }

        private void UpdateBootstrapperIfExists(DirectoryInfo sourceDirectory, string ProviderUrl)
        {
            FileInfo[] fi = sourceDirectory.GetFiles("setup.exe", SearchOption.AllDirectories);

            if (fi.Length == 0)
                return;

            ProcessStartInfo psi = new ProcessStartInfo(fi[0].FullName, string.Format("-url=\"{0}\"", ProviderUrl));
            Process p = Process.Start(psi);

            // TODO: Find better way to confirm signing
            Thread.Sleep(1000);
            SendKeysExtended.SendKeys(p.Handle, "{ENTER}");
            Thread.Sleep(1000);
        }

        private void StartProcess(string exe, string arguments)
        {
            ProcessStartInfo psi = new ProcessStartInfo(exe, arguments);
            psi.WindowStyle = ProcessWindowStyle.Hidden;
            Process p = Process.Start(psi);

            p.WaitForExit(5000);
        }

        private void RemoveDeployFilesExtension(DirectoryInfo di)
        {
            foreach (FileInfo fi in di.GetFiles())
            {
                if (fi.Name.EndsWith(".deploy"))
                    fi.MoveTo(fi.DirectoryName +"\\"+ fi.Name.Replace(".deploy", ""));
            }
        }

        private void AddDeployFilesExtension(DirectoryInfo di)
        {
            foreach (FileInfo fi in di.GetFiles())
            {
                if (fi.Name.EndsWith(".manifest") || fi.Name.EndsWith(".application"))
                    continue;

                fi.MoveTo(fi.FullName + ".deploy");
            }

            foreach (DirectoryInfo childDir in di.GetDirectories())
                AddDeployFilesExtension(childDir);
        }

        private FileInfo FindManifest(DirectoryInfo directory)
        {
            FileInfo[] fi = directory.GetFiles("*.manifest", SearchOption.AllDirectories);

            // return only first
            return fi[0];
        }

        private FileInfo FindAppManifest(DirectoryInfo directory)
        {
            FileInfo[] fi = directory.GetFiles("*.application", SearchOption.AllDirectories);

            // return only first
            return fi[0];
        }

        private FileInfo FindPackedKey(DirectoryInfo directory)
        {
            FileInfo[] fi = directory.GetFiles("*.pfx", SearchOption.AllDirectories);

            // return only first
            if (fi.Length == 0)
                throw new Exception("pfx key must be packed within zip if UsePackedKey is set true");
            else
                return fi[0];
        }
    }
}
