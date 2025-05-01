using System;
using System.Linq;

namespace RemoveMissingFiles
{
    class Program
    {
        static int Main(string[] args)
        {
            ConsoleHelper.HasWritten = false;

            var result = RemoveFiles(args);

            if (ConsoleHelper.HasWritten &&
                Environment.UserInteractive &&
                string.IsNullOrEmpty(Environment.GetEnvironmentVariable("DontPrompt")))
            {
                Console.WriteLine($"{Environment.NewLine}Press any key to continue...");
                _ = Console.ReadKey(true);
            }

            return result;
        }

        static int RemoveFiles(string[] args)
        {
            var usage = @"RemoveMissingFiles 1.1

Usage: RemoveMissingFiles [-s] <solution file>

Example: RemoveMissingFiles hello.sln

-s:  Perform a simulated removal without any side effects.";

            var parsedArgs = args;

            var simulate = parsedArgs.Any(a => a == "-s");

            parsedArgs = [.. parsedArgs.Except(["-s"])];

            if (parsedArgs.Length != 1)
            {
                ConsoleHelper.WriteLine(usage);
                return 1;
            }

            var solutionfile = parsedArgs[0];

            Solution s = new(solutionfile);

            try
            {
                ConsoleHelper.DeferredLine = solutionfile;
                s.LoadProjects();
            }
            catch (ApplicationException)
            {
                return 2;
            }

            var removedfiles = s.FixProjects();

            s.WriteProjects(simulate);

            return 0;
        }
    }
}
