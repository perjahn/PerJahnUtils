using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CheckMissingFiles
{
    class Program
    {
        static int Main(string[] args)
        {
            ConsoleHelper.HasWritten = false;

            int result = RemoveFiles(args);

            if (ConsoleHelper.HasWritten &&
                Environment.UserInteractive &&
                string.IsNullOrEmpty(Environment.GetEnvironmentVariable("DontPrompt")))
            {
                Console.WriteLine(Environment.NewLine + "Press any key to continue...");
                Console.ReadKey(true);
            }

            return result;
        }

        static int RemoveFiles(string[] args)
        {
            string usage = @"CheckMissingFiles 1.1

Usage: CheckMissingFiles <solution file>

Example: CheckMissingFiles hello.sln";


            if (args.Length != 1)
            {
                ConsoleHelper.WriteLine(usage);
                return 1;
            }

            string solutionfile = args[0];


            Solution s = new Solution(solutionfile);

            try
            {
                ConsoleHelper.DeferredLine = solutionfile;
                s.LoadProjects();
            }
            catch (ApplicationException)
            {
                return 2;
            }

            return s.CheckProjects();
        }

    }
}
