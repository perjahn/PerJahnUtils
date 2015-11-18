using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Xml.Linq;

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
            System.Threading.Thread.CurrentThread.CurrentCulture = System.Globalization.CultureInfo.InvariantCulture;

            string usage =
@"CreateBuildfile 1.0 - Creates MSBuild file.

Usage: CreateBuildfile <path> <buildfile> [excludesolution...]

Example: CreateBuildfile . all.build mysol1 mysol2";

            if (args.Length < 2)
            {
                Console.WriteLine(usage);
                return;
            }


            string path = args[0];
            string solutionfile = args[1];
            List<string> excludeSolutions = args.Skip(2).ToList();

            CreateBuildfile(path, solutionfile, excludeSolutions);
        }

        static void CreateBuildfile(string path, string buildfile, List<string> excludeSolutions)
        {
            List<string> files;

            try
            {
                files = Directory.GetFiles(path, "*.sln", SearchOption.AllDirectories)
                    .Select(f => f.StartsWith(@".\") ? f.Substring(2) : f)
                    .ToList();
            }
            catch (DirectoryNotFoundException ex)
            {
                Console.WriteLine(ex.Message);
                return;
            }

            files.Sort();

            bool first = true;
            foreach (string filename in files.ToList())  // Create tmp list
            {
                foreach (string excludePattern in excludeSolutions)
                {
                    List<string> excludeFiles = Directory.GetFiles(Path.GetDirectoryName(filename), excludePattern, SearchOption.TopDirectoryOnly).ToList();

                    if (excludeFiles.Any(f => Path.GetFileName(f) == Path.GetFileName(filename)))
                    {
                        files.Remove(filename);
                        if (first)
                        {
                            Console.WriteLine("Excluding projects:");
                            first = false;
                        }
                        Console.WriteLine("  '" + filename + "'");
                    }
                }
            }

            StringBuilder sb = new StringBuilder();

            foreach (string solutionfile in files)
            {
                sb.AppendLine(
                    @"    <MSBuild BuildInParallel=""true"" Properties=""Configuration=Release"" ContinueOnError=""true"" Projects=""" +
                    GetRelativePath(buildfile, solutionfile) + @""" />");
            }

            string s = sb.ToString();

            Console.WriteLine("Writing " + files.Count() + " solutions to " + buildfile + ".");
            using (StreamWriter sw = new StreamWriter(buildfile))
            {
                s =
                    @"<Project xmlns=""http://schemas.microsoft.com/developer/msbuild/2003"">" + Environment.NewLine +
                    @"  <Target Name=""Build"">" + Environment.NewLine +
                    s +
                    @"  </Target>" + Environment.NewLine +
                    @"</Project>" + Environment.NewLine;

                sw.Write(s);
            }


            return;
        }

        static string GetRelativePath(string pathFrom, string pathTo)
        {
            string s = pathFrom;

            int pos = 0, dirs = 0;
            while (!pathTo.StartsWith(s) && s.Length > 0)
            {
                pos = s.LastIndexOf(Path.DirectorySeparatorChar);
                if (pos == -1)
                {
                    s = string.Empty;
                }
                else
                {
                    s = s.Substring(0, pos);
                }

                dirs++;
            }

            dirs--;

            string s2 = string.Join(string.Empty, Enumerable.Repeat(".." + Path.DirectorySeparatorChar, dirs).ToArray());
            string s3 = pathTo.Substring(pos + 1);
            string s4 = s2 + s3;

            return s4;
        }
    }
}
