using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;

namespace CreateBuildfile
{
    class Program
    {
        static void Main(string[] args)
        {
            ParseArguments(args);
            if (Environment.UserInteractive && string.IsNullOrEmpty(Environment.GetEnvironmentVariable("DontPrompt")))
            {
                Console.WriteLine("Press any key to continue...");
                Console.ReadKey();
            }
        }

        static void ParseArguments(string[] args)
        {
            // Make all string comparisons (and sort/order) invariant of current culture
            // Thus, build output file is written in a consistent manner
            Thread.CurrentThread.CurrentCulture = CultureInfo.InvariantCulture;

            var usage =
@"CreateBuildfile 1.0 - Creates MSBuild file.

Usage: CreateBuildfile <path> <buildfile> [excludesolution...]

Example: CreateBuildfile . all.build mysol1 mysol2";

            if (args.Length < 2)
            {
                Console.WriteLine(usage);
                return;
            }

            var path = args[0];
            var solutionfile = args[1];
            string[] excludeSolutions = [.. args.Skip(2)];

            CreateBuildfile(path, solutionfile, excludeSolutions);
        }

        static void CreateBuildfile(string path, string buildfile, string[] excludeSolutions)
        {
            List<string> files;

            try
            {
                files = [.. Directory.GetFiles(path, "*.sln", SearchOption.AllDirectories)
                    .Select(f => f.StartsWith(@".\") ? f[2..] : f)];
            }
            catch (DirectoryNotFoundException ex)
            {
                Console.WriteLine(ex.Message);
                return;
            }

            files.Sort();

            var first = true;
            foreach (var filename in files.ToList())  // Create tmp list
            {
                foreach (var excludePattern in excludeSolutions)
                {
                    List<string> excludeFiles = [.. Directory.GetFiles(Path.GetDirectoryName(filename), excludePattern, SearchOption.TopDirectoryOnly)];

                    if (excludeFiles.Any(f => Path.GetFileName(f) == Path.GetFileName(filename)))
                    {
                        files.Remove(filename);
                        if (first)
                        {
                            Console.WriteLine("Excluding projects:");
                            first = false;
                        }
                        Console.WriteLine($"  '{filename}'");
                    }
                }
            }

            StringBuilder sb = new();

            foreach (var solutionfile in files)
            {
                sb.AppendLine($"    <MSBuild BuildInParallel=\"true\" Properties=\"Configuration=Release\" ContinueOnError=\"true\" Projects=\"{GetRelativePath(buildfile, solutionfile)}\" />");
            }

            var s = sb.ToString();

            Console.WriteLine($"Writing {files.Count} solutions to {buildfile}.");
            using StreamWriter sw = new(buildfile);
            s =
                $"<Project xmlns=\"http://schemas.microsoft.com/developer/msbuild/2003\">{Environment.NewLine}" +
                $"  <Target Name=\"Build\">{Environment.NewLine}" +
                s +
                $"  </Target>{Environment.NewLine}" +
                $"</Project>{Environment.NewLine}";

            sw.Write(s);
        }

        static string GetRelativePath(string pathFrom, string pathTo)
        {
            var s = pathFrom;

            int pos = 0, dirs = 0;
            while (!pathTo.StartsWith(s) && s.Length > 0)
            {
                pos = s.LastIndexOf(Path.DirectorySeparatorChar);
                s = pos == -1 ? string.Empty : s[..pos];
                dirs++;
            }

            dirs--;

            var s2 = string.Join(string.Empty, Enumerable.Repeat($"..{Path.DirectorySeparatorChar}", dirs));
            var s3 = pathTo[(pos + 1)..];
            var s4 = s2 + s3;

            return s4;
        }
    }
}
