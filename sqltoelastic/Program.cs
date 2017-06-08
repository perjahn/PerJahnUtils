using System;
using System.ComponentModel;
using System.Collections.Generic;
using System.Linq;
using System.ServiceProcess;
using System.Text;

namespace sqltoelastic
{
    static class Program
    {
        static void Main()
        {
            ServiceBase.Run(new CopyData());
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
