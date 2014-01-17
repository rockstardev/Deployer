using System;
using System.Collections.Generic;
using System.Text;

namespace Deployer.DeployTypes.DomainClasses
{
    class DllCacheEntry
    {
        public DllCacheEntry(DllCacheEntryEnum type, string name, string copyTo)
        {
            Type = type;
            Name = name;
            CopyTo = copyTo;
        }

        private DllCacheEntryEnum _type;
        public DllCacheEntryEnum Type
        {
            get { return _type; }
            set { _type = value; }
        }

        private string _name;
        public string Name
        {
            get { return _name; }
            set { _name = value; }
        }

        private string _copyTo;
        public string CopyTo
        {
            get { return _copyTo; }
            set { _copyTo = value; }
        }
    }

    public enum DllCacheEntryEnum
    {
        Dll, Folder
    }
}
