using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
using System.Xml;

namespace Deployer.DeployTypes
{
    abstract class BaseDeployType
    {
        public BaseDeployType()
        {
        }

        public delegate void DeployNoticeHandler(string message);
        public static event DeployNoticeHandler DeployNotice;
        protected static void onDeployNotice(string message)
        {
            if (DeployNotice != null)
                DeployNotice(message);
        }

        protected static void onDeployNotice(string format, params object[] args)
        {
            onDeployNotice(string.Format(format, args));
        }

        public abstract void Execute(DirectoryInfo sourceDirectory);
    }
}
