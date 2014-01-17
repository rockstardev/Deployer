using System;
using System.Collections.Generic;
using System.Text;

namespace Deployer.DeployTypes.DomainClasses
{
    class ConfigReplacesRoot
    {
        public ConfigReplacesRoot(string searchExpression)
        {
            SearchExpression = searchExpression;
            Entries = new List<ConfigReplacesEntry>();
        }

        public bool ReplaceOnSource { get; set; }

        private string _searchExpression;
        public string SearchExpression
        {
            get { return _searchExpression; }
            set { _searchExpression = value; }
        }

        private List<ConfigReplacesEntry> _entries;
        internal List<ConfigReplacesEntry> Entries
        {
            get { return _entries; }
            set { _entries = value; }
        }


    }
}
