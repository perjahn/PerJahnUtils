using System;
using System.ComponentModel;
using System.Linq;
using System.ServiceProcess;
using System.Threading;

namespace Patcher
{
    public class Patcher : ServiceBase
    {
        protected override void OnStart(string[] args)
        {
            base.OnStart(args);
            Patch patch = new Patch();
            new Thread(patch.InstallPatches) { IsBackground = true }.Start();
        }

        protected override void OnStop()
        {
        }
    }

    internal class Program
    {
        static void Main()
        {
            Patcher.Run(new Patcher());
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
                ServiceName = "Patcher",
                DisplayName = "Patcher",
                Description = "Patcher"
            };

            Installers.Add(process);
            Installers.Add(serviceAdmin);
        }
    }
}
