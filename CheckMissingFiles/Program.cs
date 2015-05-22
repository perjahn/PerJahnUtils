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
            string usage = @"CheckMissingFiles 2.0

Usage: CheckMissingFiles [-r] [-t] <solution file>

-r:  Reverse check - warn if files exists in file system and but missing in project files.
-t:  Teamcity error and warning messages.

Example: CheckMissingFiles hello.sln";


            bool reverseCheck = args.Any(a => a == "-r");
            args = args.Except(new string[] { "-r" }).ToArray();

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
                s = new Solution(solutionfile, teamcityErrorMessage, reverseCheck);
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
