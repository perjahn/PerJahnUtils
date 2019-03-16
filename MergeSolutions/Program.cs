using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

// ToDo 1: Automatically exclude conflicting projects (projects with same name, but different guids/paths).
// ToDo 2: Rewrite paths to projects. All solutions must currently exist in the same folder.
// But really, the recommended approach is to use the CreateSolution util instead.

namespace MergeSolutions
{
    class Program
    {
        static bool _globalsection;
        static bool _verbose;

        static int Main(string[] args)
        {
            string[] parsedArgs = ParseArgs(args);

            parsedArgs = GetSolutions(parsedArgs, out string[] excludeProjects);

            if (parsedArgs.Length < 3)
            {
                Console.WriteLine(
@"MergeSolutions 1.3

Usage: MergeSolutions [-g] [-v] <outputfile> <inputfile1> <inputfile2> ... <-excludeproj1> ...

-g:    Add global section.
-v:    Verbose logging.

Example: MergeSolutions all.sln sol1.sln sol2.sln -proj1");

                return 1;
            }

            int result;
            try
            {
                result = MergeSolutions(parsedArgs[0], parsedArgs.Skip(1).ToArray(), excludeProjects);
            }
            catch (ApplicationException ex)
            {
                Console.Write(ex.Message);
                return 1;
            }

            return result;
        }

        static string[] ParseArgs(string[] args)
        {
            _globalsection = args.Contains("-g");
            _verbose = args.Contains("-v");

            return args
                .Where(a => !(new[] { "-g", "-v" }).Contains(a))
                .ToArray();
        }

        static string[] GetSolutions(string[] args, out string[] excludeProjects)
        {
            excludeProjects = args
                .Where(a => a.StartsWith("-"))
                .Select(a => a.Substring(1))
                .ToArray();

            return args
                .Where(a => !a.StartsWith("-"))
                .ToArray();
        }

        static int MergeSolutions(string outputSolution, string[] solutionPatterns, string[] excludeProjects)
        {
            string[] solutionfiles = GetSolutionFiles(solutionPatterns);

            Console.WriteLine("Merging: '" + string.Join("' + '", solutionfiles) + "' -> '" + outputSolution + "'");

            if (excludeProjects.Length == 0)
            {
                Console.WriteLine("Excluding no projects.");
            }
            else
            {
                Console.WriteLine("Excluding projects: '" + string.Join("', '", excludeProjects) + "'");
            }


            Solution[] solutions = solutionfiles
                .Select(s =>
                {
                    Console.Write($"Reading solution file: '{s}': ");
                    string[] filerows = File.ReadAllLines(s);
                    Console.WriteLine($"Got: {filerows.Length} rows.");

                    return new Solution()
                    {
                        Filename = s,
                        Rows = filerows
                    };
                })
                .ToArray();


            string[] projguids = {
                "FAE04EC0-301F-11D3-BF4B-00C04F79EFBC",  // c#
                "E24C65DC-7377-472B-9ABA-BC803B73C61A",  // website
                "F2A71F9B-5D33-465A-A702-920D77279786",  // f#
                "F184B08F-C81C-45F6-A57F-5ABD9991F28F",  // vb
                "8BC9CEB8-8B4A-11D0-8D11-00A0C91BC942"   // c++
            };


            List<Project> projects = new List<Project>();

            bool inproject = false;
            foreach (string row in solutions.SelectMany(s => s.Rows))
            {
                if (projguids.Any(p => row.StartsWith("Project(\"{" + p + "}\")")))
                {
                    if (row.Split(',').Length != 3)
                    {
                        Console.WriteLine($"Malformed solution file, ignoring project: '{row}'");
                    }
                    else
                    {
                        Project project = new Project();

                        string name = row.Split('=')[1].Split(',')[0].Trim().Trim('\"');
                        project.Fullname = name;

                        if (name.Contains('(') && name.EndsWith(")"))
                        {
                            name = name.Substring(0, name.IndexOf('('));
                        }
                        project.Name = name;

                        string guid = row.Split('=')[1].Split(',')[2].Trim().Trim('\"');
                        project.Guid = guid;

                        List<string> projrows = new List<string>();
                        project.Projrows = projrows;

                        if (_verbose)
                        {
                            Console.WriteLine($"Adding project: '{project.Name}'");
                        }
                        projects.Add(project);

                        inproject = true;
                    }
                }
                if (inproject)
                {
                    projects.Last().Projrows.Add(row);
                }
                if (row == "EndProject")
                {
                    inproject = false;
                }
            }

            projects = CompactProjects(projects);


            projects = ExcludeProjects(projects, excludeProjects);


            foreach (Project project in projects.OrderBy(p => p.Name))
            {
                if (_verbose)
                {
                    Console.WriteLine($"Keeping unique project: '{project.Name}'");
                }
            }

            List<string> allprojrows = new List<string>
            {
                "Microsoft Visual Studio Solution File, Format Version 12.00",
                "# Visual Studio 15"
            };
            allprojrows.AddRange(projects.OrderBy(p => p.Name).SelectMany(r => r.Projrows));

            if (_globalsection)
            {
                allprojrows.AddRange(GenerateGlobalSection(projects.Select(p => p.Guid).ToArray(), new[] { "Debug", "Release" }));
            }

            Console.WriteLine($"Writing file: '{outputSolution}': {projects.Count()} projects, {allprojrows.Count()} rows.");
            File.WriteAllLines(outputSolution, allprojrows);

            return 0;
        }

        static string[] GetSolutionFiles(string[] solutionPatterns)
        {
            List<string> inputFiles2 = new List<string>();

            List<string> allfiles = new List<string>();
            List<string> errors = new List<string>();

            foreach (string inputPath in solutionPatterns)
            {
                string path, pattern;
                if (inputPath.Contains(Path.DirectorySeparatorChar))
                {
                    path = Path.GetDirectoryName(inputPath);
                    pattern = Path.GetFileName(inputPath);
                }
                else
                {
                    path = ".";
                    pattern = inputPath;
                }

                string[] files;
                try
                {
                    files = Directory.GetFiles(path, pattern);
                }
                catch (IOException ex)
                {
                    errors.Add($"'{inputPath}' -> path: '{path}', pattern: '{pattern}' -> {ex.Message}");
                    continue;
                }

                if (files.Length == 0)
                {
                    errors.Add($"'{inputPath}' -> path: '{path}', pattern: '{pattern}' -> No files found.");
                    continue;
                }

                foreach (string solutionfile in files)
                {
                    allfiles.Add(solutionfile.StartsWith(@".\") ? solutionfile.Substring(2) : solutionfile);
                }
            }

            if (errors.Count() > 0)
            {
                StringBuilder message = new StringBuilder();
                message.AppendLine("Couldn't find solution files:");
                foreach (string error in errors)
                {
                    message.AppendLine(error);
                }
                throw new ApplicationException(message.ToString());
            }


            return allfiles
                .Distinct()
                .OrderBy(s => s)
                .ToArray();
        }

        static List<Project> CompactProjects(List<Project> projects)
        {
            List<Project> uniqueprojects = new List<Project>();

            foreach (Project project in projects)
            {
                if (uniqueprojects.Any(p => p.Name == project.Name))
                {
                    if (_verbose)
                    {
                        Console.WriteLine($"Ignoring redundant project: '{project.Fullname}'");
                    }
                }
                else
                {
                    uniqueprojects.Add(project);
                }
            }

            return uniqueprojects;
        }

        static List<Project> ExcludeProjects(List<Project> projects, string[] excludeProjects)
        {
            List<Project> includeprojects = new List<Project>();

            foreach (Project project in projects)
            {
                if (excludeProjects.Contains(project.Name))
                {
                    if (_verbose)
                    {
                        Console.WriteLine($"Excluding project: '{project.Fullname}'");
                    }
                }
                else
                {
                    includeprojects.Add(project);
                }
            }

            return includeprojects;
        }

        static string[] GenerateGlobalSection(string[] projectguids, string[] configs)
        {
            List<string> rows = new List<string>
            {
                "Global",
                "\tGlobalSection(SolutionConfigurationPlatforms) = preSolution"
            };

            foreach (string config in configs)
            {
                rows.Add($"\t\t{config}|Any CPU = {config}|Any CPU");
                rows.Add($"\t\t{config}|x64 = {config}|x64");
                rows.Add($"\t\t{config}|x86 = {config}|x86");
            }

            rows.Add("\tEndGlobalSection");


            rows.Add("\tGlobalSection(ProjectConfigurationPlatforms) = postSolution");

            foreach (string guid in projectguids)
            {
                foreach (string config in configs)
                {
                    rows.Add($"\t\t{guid}.{config}|Any CPU.ActiveCfg = {config}|Any CPU");
                    rows.Add($"\t\t{guid}.{config}|Any CPU.Build.0 = {config}|Any CPU");
                    rows.Add($"\t\t{guid}.{config}|x64.ActiveCfg = {config}|Any CPU");
                    rows.Add($"\t\t{guid}.{config}|x64.Build.0 = {config}|Any CPU");
                    rows.Add($"\t\t{guid}.{config}|x86.ActiveCfg = {config}|Any CPU");
                    rows.Add($"\t\t{guid}.{config}|x86.Build.0 = {config}|Any CPU");
                }
            }

            rows.Add("\tEndGlobalSection");
            rows.Add("EndGlobal");


            return rows.ToArray();
        }
    }
}
