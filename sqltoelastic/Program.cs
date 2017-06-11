using System;
using System.ComponentModel;
using System.Collections.Generic;
using System.Linq;
using System.ServiceProcess;
using System.Text;
using System.IO;
using System.Configuration;

namespace sqltoelastic
{
    static class Program
    {
        static void Main(string[] args)
        {
            if (args.Length == 1 && args[0] == "run")
            {
                string filename = ConfigurationManager.AppSettings["logfile"];
                using (StreamWriter logfile = new StreamWriter(filename, true))
                {
                    CopyData._logfile = logfile;
                    SqlServer._logfile = logfile;
                    Elastic._logfile = logfile;

                    CopyData.DoStuff();
                }
            }
            else
            {
                ServiceBase.Run(new CopyData());
            }
        }
    }

    [RunInstaller(true)]
    public class Installer : System.Configuration.Install.Installer
    {
        public Installer()
        {
            var process = new ServiceProcessInstaller { Account = ServiceAccount.LocalSystem };

            var serviceAdmin = new ServiceInstaller
            {
                StartType = ServiceStartMode.Automatic,
                ServiceName = "sqltoelastic",
                DisplayName = "sqltoelastic",
                Description = "sqltoelastic"
            };

            Installers.Add(process);
            Installers.Add(serviceAdmin);
        }
    }
}
