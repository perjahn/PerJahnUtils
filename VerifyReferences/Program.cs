using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VerifyReferences
{
    class Program
    {
        static int Main(string[] args)
        {
            int result = CheckFiles(args);

            if (Environment.UserInteractive &&
                string.IsNullOrEmpty(Environment.GetEnvironmentVariable("DontPrompt")))
            {
                Console.WriteLine(Environment.NewLine + "Press any key to continue...");
                Console.ReadKey();
            }

            return result;
        }

        static int CheckFiles(string[] args)
        {
            string usage =
@"VerifyReferences 0.2 - Verifies that refereces in projects are to same assemblies.

Usage: VerifyReferences [-r] [-t] <path>

-r:  Recurse subdirectories.
-t:  Teamcity error and warning messages.

Example: VerifyReferences -r .";

            bool parseSubdirs = args.Any(a => a == "-r");
            args = args.Where(a => a != "-r").ToArray();

            bool teamcityErrorMessage = args.Any(a => a == "-t");
            args = args.Where(a => a != "-t").ToArray();

            if (args.Length != 1)
            {
                Console.WriteLine(usage);
                return 1;
            }

            string path = args[0];

            string[] projectpaths = Directory.GetFiles(path, "*.*proj",
                    parseSubdirs ? SearchOption.AllDirectories : SearchOption.TopDirectoryOnly)
                .Select(p => p.StartsWith(@".\") ? p.Substring(2) : p)
                .ToArray();

            Console.WriteLine("Loading " + projectpaths.Length + " projects...");

            List<Project> projects = new List<Project>();

            foreach (string projectpath in projectpaths)
            {
                projects.Add(new Project(projectpath, teamcityErrorMessage));
            }


            return Validate(projects) ? 0 : 1;
        }

        static bool Validate(List<Project> projects)
        {
            var refs = projects
                 .SelectMany(p => p.references, (p, r) =>
                    new
                    {
                        project = p,
                        reference = r
                    })
                .GroupBy(r => r.reference.shortinclude, (shortinclude, references) =>
                    new
                    {
                        shortinclude = shortinclude,
                        paths = references
                            .Select(rr =>
                                new
                                {
                                    path = rr.reference.path,
                                    projectfiles = references
                                        .Select(p => p.project.projectFile)
                                })
                                .GroupBy(r => r.path, (path, projects2) =>
                                     new
                                     {
                                         path = path,
                                         projectfiles = projects2
                                            .SelectMany(p => p.projectfiles)
                                            .Distinct()
                                            .OrderBy(f => f)
                                            .ToArray()
                                     })
                                .OrderBy(r => r.path)
                                .ToArray()
                    })
                .OrderBy(r => r.shortinclude)
                .ToList();


            var failrefs = refs
                .Where(r => r.paths.Count() > 1)
                .ToArray();

            // .projects.Any(p => p..path != r.projects.First()..path)

            Console.WriteLine("Found " + refs.Count() + " references, " + failrefs.Count() + " inconsistent.");

            foreach (var failref in failrefs)
            {
                int count1 = failref.paths.Count();
                int count2 = failref.paths.Count();

                ConsoleHelper.WriteLineColor(failref.shortinclude + " (" + count1 + "/" + count2 + ")", ConsoleColor.Cyan);

                foreach (var path in failref.paths)
                {
                    Console.WriteLine("  " + path.path);
                    foreach(var projectfile in path.projectfiles)
                    {
                        Console.WriteLine("    " + projectfile);
                    }
                }

                //Console.WriteLine("Non-unique hint path: '" + string.Join("', '", failref.Select(r => r.path)) + "'");
            }


            if (failrefs.Count() > 0)
            {
                ConsoleHelper.WriteLineColor("Inconsistencies found in " + failrefs.Count() + " projects.", ConsoleColor.Red);
                return false;
            }

            ConsoleHelper.WriteLineColor("All good!", ConsoleColor.Green);
            return true;
        }


    }
}
