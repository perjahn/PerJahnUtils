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
            string usage = @"CheckMissingFiles 1.3

Usage: CheckMissingFiles [-t] <solution file>

-t:  Teamcity error and warning messages.

Example: CheckMissingFiles hello.sln";


            bool teamcityErrorMessage = args.Any(a => a == "-t");

            args = args.Except(new string[] { "-t" }).ToArray();


            if (args.Length != 1)
            {
                ConsoleHelper.WriteLine(usage);
                return 1;
            }

            string solutionfile = args[0];


            Solution s;

            try
            {
                ConsoleHelper.DeferredLine = solutionfile;
                s = new Solution(solutionfile, teamcityErrorMessage);
            }
            catch (ApplicationException ex)
            {
                ConsoleHelper.WriteLineColor(ex.Message, ConsoleColor.Red);
                return 2;
            }

            return s.CheckProjects();
        }
    }
}
