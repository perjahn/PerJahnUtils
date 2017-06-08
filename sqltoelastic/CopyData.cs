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
        private static string _logfile;

        public CopyData()
        {
            InitializeComponent();
        }

        protected override void OnStart(string[] args)
        {
            _timer.Elapsed += new ElapsedEventHandler(DoStuff);
            _timer.Enabled = true;

            System.Threading.Thread.Sleep(1000);
            _timer.Interval = 60 * 60 * 1000;
        }

        protected override void OnStop()
        {
            _timer.Enabled = false;
        }

        public static void DoStuff(object source, ElapsedEventArgs e)
        {
            _logfile = ConfigurationManager.AppSettings["logfile"];

            string dbprovider = ConfigurationManager.AppSettings["dbprovider"];
            string connstr = ConfigurationManager.AppSettings["connstr"];
            string sql = ConfigurationManager.AppSettings["sql"];

            SqlServer sqlserver = new SqlServer();
            JObject[] jsonrows = sqlserver.DumpTable(dbprovider, connstr, sql);

            Log($"Got {jsonrows.Length} rows.");

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
                Elastic._logfile = _logfile;
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
            File.AppendAllText(_logfile, $"{date}: {message}{Environment.NewLine}");
        }
    }
}
