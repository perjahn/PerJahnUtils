using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace CheckNamespace
{
    class Program
    {
        static int Main(string[] args)
        {
            var result = CheckNamespace(args);

            return result;
        }

        static int CheckNamespace(string[] args)
        {
            var usage = @"CheckNamespace 1.1

Usage: CheckNamespace <solution files...>

Example: powershell CheckNamespace (dir C:\git\mycode -i *.sln -r)";

            if (args.Length == 0)
            {
                Console.WriteLine(usage);
                return 1;
            }

            List<Solution> solutions = [.. LoadSolutions(args)];

            List<Project> projects = [.. solutions.SelectMany(s => s.Projects)];

            Console.WriteLine($"Total projects: {projects.Count}");

            projects = [.. projects
                .GroupBy(p => Path.Combine(Path.GetDirectoryName(p.Solutionfile), p.Sln_path))
                .Select(g => g.First())];

            Console.WriteLine($"Unique projects: {projects.Count}");

            var failcount = 0;

            foreach (var p in projects.OrderBy(p => Path.Combine(Path.GetDirectoryName(p.Solutionfile), p.Sln_path)))
            {
                failcount += p.CheckNamespace();
            }

            ConsoleHelper.WriteLineColor($"Total inconsistencies: {failcount}", ConsoleColor.Cyan);

            return 0;
        }

        private static IEnumerable<Solution> LoadSolutions(string[] solpaths)
        {
            foreach (var path in solpaths)
            {
                Solution s;

                try
                {
                    s = new Solution(path);
                }
                catch (ApplicationException ex)
                {
                    ConsoleHelper.WriteLineColor(ex.Message, ConsoleColor.Red);
                    continue;
                }

                yield return s;
            }
        }
    }
}
