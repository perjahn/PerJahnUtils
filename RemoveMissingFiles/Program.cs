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
            int result = RemoveFiles(args);

            if (Environment.UserInteractive && !string.IsNullOrEmpty(Environment.GetEnvironmentVariable("DontPrompt")))
            {
                Console.WriteLine(Environment.NewLine + "Press any key to continue...");
                Console.ReadKey(true);
            }

            return result;
        }

        static int RemoveFiles(string[] args)
        {
            string usage = @"RemoveMissingFiles 1.0

Usage: RemoveMissingFiles [-s] <solution file>

Example: RemoveMissingFiles hello.sln

-s:  Perform a simulated removal without any side effects.";


            bool simulate = args.Any(a => a == "-s");

            args = args.Except(new string[] { "-s" }).ToArray();

            if (args.Length != 1)
            {
                Console.WriteLine(usage);
                return 1;
            }

            string solutionfile = args[0];


            Solution s = new Solution(solutionfile);

            try
            {
                ConsoleHelper.deferredLine = solutionfile;
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
