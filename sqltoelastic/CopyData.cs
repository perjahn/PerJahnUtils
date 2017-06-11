using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Configuration;
using System.IO;
using System.Linq;
using System.ServiceProcess;
using System.Text;
using System.Timers;

namespace sqltoelastic
{
    public partial class CopyData : ServiceBase
    {
        private Timer _timer = new Timer(750);
        public static StreamWriter _logfile;

        public CopyData()
        {
            InitializeComponent();
        }

        protected override void OnStart(string[] args)
        {
            _timer.Elapsed += new ElapsedEventHandler(OnTimer);
            _timer.Enabled = true;

            System.Threading.Thread.Sleep(1000);
            _timer.Interval = 60 * 60 * 1000;
        }

        protected override void OnStop()
        {
            _timer.Enabled = false;
        }

        private static void OnTimer(object source, ElapsedEventArgs e)
        {
            string filename = ConfigurationManager.AppSettings["logfile"];
            using (StreamWriter logfile = new StreamWriter(filename, true))
            {
                _logfile = logfile;
                SqlServer._logfile = logfile;
                Elastic._logfile = logfile;

                DoStuff();
            }
        }

        public static void DoStuff()
        {
            Log("Starting...");

            string dbprovider = ConfigurationManager.AppSettings["dbprovider"];
            string connstr = ConfigurationManager.AppSettings["connstr"];
            string sql = ConfigurationManager.AppSettings["sql"];

            string[] toupperfields = ConfigurationManager.AppSettings["toupperfields"].Split(',');
            string[] tolowerfields = ConfigurationManager.AppSettings["tolowerfields"].Split(',');

            string addconstantfield = ConfigurationManager.AppSettings["addconstantfield"];
            string[] escapefields = ConfigurationManager.AppSettings["escapefields"].Split(',');

            JObject[] jsonrows;
            try
            {
                SqlServer sqlserver = new SqlServer();
                jsonrows = sqlserver.DumpTable(dbprovider, connstr, sql, toupperfields, tolowerfields, addconstantfield, escapefields);
            }
            catch (Exception ex)
            {
                Log($"Exception: >>>{ex.ToString()}<<<");
                return;
            }

            Log($"Got {jsonrows.Length} rows.");

            //File.WriteAllLines(@"C:\data.txt", jsonrows.Select(r => r.ToString()));

            string serverurl = ConfigurationManager.AppSettings["serverurl"];
            string username = ConfigurationManager.AppSettings["username"];
            string password = ConfigurationManager.AppSettings["password"];
            string indexname = ConfigurationManager.AppSettings["indexname"];
            string typename = ConfigurationManager.AppSettings["typename"];
            string timestampfield = ConfigurationManager.AppSettings["timestampfield"];
            string idprefix = ConfigurationManager.AppSettings["idprefix"];
            string idfield = ConfigurationManager.AppSettings["idfield"];

            try
            {
                Elastic elastic = new Elastic();
                elastic.PutIntoIndex(serverurl, username, password, indexname, typename, timestampfield, idprefix, idfield, jsonrows);
            }
            catch (Exception ex)
            {
                Log($"Exception: >>>{ex.ToString()}<<<");
            }
        }

        private static void Log(string message)
        {
            string date = DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss");
            _logfile.WriteLine($"{date}: {message}");
            _logfile.Flush();
        }
    }
}
