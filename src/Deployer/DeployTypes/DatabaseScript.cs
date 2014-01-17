using System;
using System.Collections.Generic;
using System.Text;
using System.Xml;
using System.Data.SqlClient;
using System.IO;
using System.Text.RegularExpressions;
using System.Data;
using Deployer.Logic;

namespace Deployer.DeployTypes
{
    class DatabaseScript : BaseDeployType
    {
        public DatabaseScript()
        {
            _isolationLevel = IsolationLevel.ReadCommitted;
        }

        public DatabaseScript(XmlNode node) : this()
        {
            _connectionString = node.SelectSingleNode("connectionString").InnerXml;
            string isoLvl = XmlUtil.ParseStringNode(node.SelectSingleNode("isolationLevel"), "ReadCommitted");
            _isolationLevel = (IsolationLevel)Enum.Parse(typeof(IsolationLevel), isoLvl);
            _commandTimeout = XmlUtil.ParseIntNode(node.SelectSingleNode("commandTimeout"), 30);
        }

        private string _connectionString;
        private IsolationLevel _isolationLevel;
        private int _commandTimeout;

        public override void Execute(System.IO.DirectoryInfo sourceDirectory)
        {
            foreach (FileInfo fi in sourceDirectory.GetFiles("*.sql"))
            {
                ExecuteScript(File.ReadAllText(fi.FullName));
            }
        }

        private void ExecuteScript(string sqlText)
        {
            Regex regex = new Regex("^GO", RegexOptions.IgnoreCase | RegexOptions.Multiline);

            string[] sqlLines = regex.Split(sqlText);

            SqlConnection conn = new SqlConnection(_connectionString);
            conn.Open();
            SqlTransaction trans = conn.BeginTransaction(_isolationLevel);

            try
            {
                SqlCommand cmd = new SqlCommand();
                cmd.CommandType = CommandType.Text;
                cmd.CommandTimeout = _commandTimeout;
                cmd.Connection = conn;
                cmd.Transaction = trans;

                foreach (string line in sqlLines)
                {
                    if (line.Trim().Length > 0)
                    {
                        cmd.CommandText = line;
                        cmd.ExecuteNonQuery();
                    }
                }

                trans.Commit();
            }
            catch (Exception ex)
            {
                trans.Rollback();
                throw ex;
            }
            finally
            {
                conn.Close();
            }
        }
    }
}
