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
        public string fullname { get; set; }
        public List<string> projrows { get; set; }
    }

    class Program
    {
        static bool _verbose;

        static int Main(string[] args)
        {
            string[] parsedArgs = ParseArgs(args);

            if (parsedArgs.Length < 3)
            {
                Console.WriteLine(
@"MergeSolutions 1.0

Usage: MergeSolutions [-v] <outputfile> <inputfile1> <inputfile2> ...

-v:    Verbose logging.

Example: MergeSolutions all.sln sol1.sln sol2.sln sol3.sln");

                return 1;
            }

            int result = MergeSolutions(parsedArgs[0], parsedArgs.Skip(1).ToArray());

            return result;
        }

        static string[] ParseArgs(string[] args)
        {
            _verbose = args.Contains("-v");
            return args.Where(a => a != "-v").ToArray();
        }

        static int MergeSolutions(string outputfile, string[] inputfiles)
        {
            Console.WriteLine("Merging: '" + string.Join("' + '", inputfiles) + "' -> '" + outputfile + "'");

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
                    Project project = new Project();

                    string name = row.Split('=')[1].Split(',')[0].Trim().Trim('\"');
                    project.fullname = name;

                    if (name.Contains('(') && name.EndsWith(")"))
                    {
                        name = name.Substring(0, name.IndexOf('('));
                    }
                    project.name = name;

                    List<string> projrows = new List<string>();
                    project.projrows = projrows;

                    projects.Add(project);

                    inproject = true;
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
    }
}
