using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace RemoveSolutionConfigs
{
    class Program
    {
        static void Main(string[] args)
        {
            string[] filteredArgs = args;

            var verbose = filteredArgs.Contains("-v");
            filteredArgs = [.. filteredArgs.Where(a => a != "-v")];

            if (filteredArgs.Length < 2)
            {
                Console.WriteLine(
@"RemoveSolutionConfigs 1.0

Usage: RemoveSolutionConfigs [-v] <solutionfile> <remove_config_1> <remove_config_2> ...

-v:  Verbose logging.");
            }

            var solutionfile = filteredArgs[0];

            if (!File.Exists(solutionfile))
            {
                Console.WriteLine($"Couldn't find file: {solutionfile}");
                return;
            }

            string[] excludes = [.. filteredArgs.Skip(1)];

            var rows = File.ReadAllLines(solutionfile);
            List<string> rows2 = [];

            foreach (var row in rows)
            {
                if (row.StartsWith("\t\t") && excludes.Any(e => row.Length > 3 + e.Length && string.Compare(row.Substring(2, e.Length + 1), e + "|", true) == 0))
                {
                    if (verbose)
                    {
                        Console.WriteLine(row[2..]);
                    }
                }
                else if (row.StartsWith("\t\t") && excludes.Any(e => row.Length > 42 + e.Length && string.Compare(row.Substring(41, e.Length + 1), e + "|", true) == 0))
                {
                    if (verbose)
                    {
                        Console.WriteLine(row[41..]);
                    }
                }
                else
                {
                    rows2.Add(row);
                }
            }

            // UTF8 BOM: 239, 187, 191
            File.WriteAllLines(solutionfile, rows2, Encoding.UTF8);
        }
    }
}
