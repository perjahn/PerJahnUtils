using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

// ToDo 1: Support wildcards.
// ToDo 2: Rewrite paths to projects. All solutions must currently exist in the same folder.
// But really, the recommended approach is to use the CreateSolution util instead.

namespace MergeSolutions
{
    class Project
    {
        public string name { get; set; }
        public string guid { get; set; }
        public string fullname { get; set; }
        public List<string> projrows { get; set; }
    }

    class Program
    {
        static bool _globalsection;
        static bool _verbose;

        static int Main(string[] args)
        {
            string[] parsedArgs = ParseArgs(args);

            string[] excludeProjects;
            parsedArgs = GetExcludeProjects(parsedArgs, out excludeProjects);

            if (parsedArgs.Length < 3)
            {
                Console.WriteLine(
@"MergeSolutions 1.2

Usage: MergeSolutions [-g] [-v] <outputfile> <inputfile1> <inputfile2> ... <-excludeproj1> ...

-g:    Create global section.
-v:    Verbose logging.

Example: MergeSolutions all.sln sol1.sln sol2.sln -proj1");

                return 1;
            }

            int result = MergeSolutions(parsedArgs[0], parsedArgs.Skip(1).ToArray(), excludeProjects);

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

        static string[] GetExcludeProjects(string[] args, out string[] excludeProjects)
        {
            excludeProjects = args
                .Where(a => a.StartsWith("-"))
                .Select(a => a.Substring(1))
                .ToArray();

            return args
                .Where(a => !a.StartsWith("-"))
                .ToArray();
        }

        static int MergeSolutions(string outputfile, string[] inputfiles, string[] excludeProjects)
        {
            Console.WriteLine("Merging: '" + string.Join("' + '", inputfiles) + "' -> '" + outputfile + "'");

            Console.WriteLine("Excluding projects: '" + string.Join("', '", excludeProjects) + "'");

            bool missingfiles = false;
            foreach (string inputfile in inputfiles)
            {
                if (!File.Exists(inputfile))
                {
                    if (!missingfiles)
                    {
                        Console.WriteLine("Couldn't find inputfiles:");
                    }
                    Console.WriteLine("'" + inputfile + "'");
                    missingfiles = true;
                }
            }
            if (missingfiles)
            {
                return 1;
            }


            List<string> rows = new List<string>();
            foreach (string inputfile in inputfiles)
            {
                Console.Write("Reading file: '" + inputfile + "': ");
                string[] filerows = File.ReadAllLines(inputfile);
                Console.WriteLine("Got: " + filerows.Length + " rows.");
                rows.AddRange(filerows);
            }

            string[] projguids = {
                "FAE04EC0-301F-11D3-BF4B-00C04F79EFBC",  // c#
                "E24C65DC-7377-472B-9ABA-BC803B73C61A",  // website
                "F2A71F9B-5D33-465A-A702-920D77279786",  // f#
                "F184B08F-C81C-45F6-A57F-5ABD9991F28F"  // vb
            };


            List<Project> projects = new List<Project>();

            bool inproject = false;
            foreach (string row in rows)
            {
                if (projguids.Any(p => row.StartsWith("Project(\"{" + p + "}\")")))
                {
                    if (row.Split(',').Length != 3)
                    {
                        Console.WriteLine("Malformed solution file, ignoring project: '" + row + "'");
                    }
                    else
                    {
                        Project project = new Project();

                        string name = row.Split('=')[1].Split(',')[0].Trim().Trim('\"');
                        project.fullname = name;

                        if (name.Contains('(') && name.EndsWith(")"))
                        {
                            name = name.Substring(0, name.IndexOf('('));
                        }
                        project.name = name;

                        string guid = row.Split('=')[1].Split(',')[2].Trim().Trim('\"');
                        project.guid = guid;

                        List<string> projrows = new List<string>();
                        project.projrows = projrows;

                        projects.Add(project);

                        inproject = true;
                    }
                }
                if (inproject)
                {
                    projects.Last().projrows.Add(row);
                }
                if (row == "EndProject")
                {
                    inproject = false;
                }
            }

            projects = CompactProjects(projects);


            projects = ExcludeProjects(projects, excludeProjects);


            foreach (Project project in projects.OrderBy(p => p.name))
            {
                if (_verbose)
                {
                    Console.WriteLine("Keeping unique project: '" + project.name + "'");
                }
            }

            List<string> allprojrows = new List<string>();
            allprojrows.Add("Microsoft Visual Studio Solution File, Format Version 12.00");
            allprojrows.Add("# Visual Studio 14");
            allprojrows.AddRange(projects.OrderBy(p => p.name).SelectMany(r => r.projrows));

            if (_globalsection)
            {
                allprojrows.AddRange(GenerateGlobalSection(projects.Select(p => p.guid).ToArray(), new[] { "Debug", "Release" }));
            }

            Console.WriteLine("Writing file: '" + outputfile + "': " + projects.Count() + " projects, " + allprojrows.Count() + " rows.");
            File.WriteAllLines(outputfile, allprojrows);

            return 0;
        }

        static List<Project> CompactProjects(List<Project> projects)
        {
            List<Project> uniqueprojects = new List<Project>();

            foreach (Project project in projects)
            {
                if (uniqueprojects.Any(p => p.name == project.name))
                {
                    if (_verbose)
                    {
                        Console.WriteLine("Ignoring redundant project: '" + project.fullname + "'");
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
                if (excludeProjects.Contains(project.name))
                {
                    if (_verbose)
                    {
                        Console.WriteLine("Excluding project: '" + project.fullname + "'");
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
            List<string> rows = new List<string>();


            rows.Add("Global");
            rows.Add("\tGlobalSection(SolutionConfigurationPlatforms) = preSolution");

            foreach (string config in configs)
            {
                rows.Add("\t\t" + config + "|Any CPU = " + config + "|Any CPU");
                rows.Add("\t\t" + config + "|x64 = " + config + "|x64");
                rows.Add("\t\t" + config + "|x86 = " + config + "|x86");
            }

            rows.Add("\tEndGlobalSection");


            rows.Add("\tGlobalSection(ProjectConfigurationPlatforms) = postSolution");

            foreach (string guid in projectguids)
            {
                foreach (string config in configs)
                {
                    rows.Add("\t\t" + guid + "." + config + "|Any CPU.ActiveCfg = " + config + "|Any CPU");
                    rows.Add("\t\t" + guid + "." + config + "|Any CPU.Build.0 = " + config + "|Any CPU");
                    rows.Add("\t\t" + guid + "." + config + "|x64.ActiveCfg = " + config + "|Any CPU");
                    rows.Add("\t\t" + guid + "." + config + "|x64.Build.0 = " + config + "|Any CPU");
                    rows.Add("\t\t" + guid + "." + config + "|x86.ActiveCfg = " + config + "|Any CPU");
                    rows.Add("\t\t" + guid + "." + config + "|x86.Build.0 = " + config + "|Any CPU");
                }
            }

            rows.Add("\tEndGlobalSection");
            rows.Add("EndGlobal");


            return rows.ToArray();
        }
    }
}
