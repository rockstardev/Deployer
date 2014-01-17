using System;
using System.Collections.Generic;
using System.Text;

namespace Deployer.DeployTypes.DomainClasses
{
    class ConfigReplacesEntry
    {
        public ConfigReplacesEntry(string find, string replace)
        {
            Find = find;
            Replace = replace;
        }

        public ConfigReplacesEntry(string findStart, string findEnd, string replace)
        {
            FindStart = findStart;
            FindEnd = findEnd;
            Replace = replace;
        }


        public bool IsSimpleFindReplace
        {
            get { return String.IsNullOrEmpty(FindStart); }
        }

        public string FindStart { get; set; }
        public string FindEnd { get; set; }

        public string Find { get; set; }
        public string Replace { get; set; }
    }
}
