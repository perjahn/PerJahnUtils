using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RemoveMissingFiles
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
            string usage = @"RemoveMissingFiles 1.1

Usage: RemoveMissingFiles [-s] <solution file>

Example: RemoveMissingFiles hello.sln

-s:  Perform a simulated removal without any side effects.";


            bool simulate = args.Any(a => a == "-s");

            args = args.Except(new string[] { "-s" }).ToArray();

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

            int removedfiles = s.FixProjects();

            s.WriteProjects(simulate);


            return 0;
        }

    }
}
