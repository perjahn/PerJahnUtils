using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace VerifyReferences
{
    class Program
    {
        static int Main(string[] args)
        {
            var result = CheckFiles(args);

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
            var usage =
@"VerifyReferences 0.2 - Verifies that refereces in projects are to same assemblies.

Usage: VerifyReferences [-r] [-t] <path>

-r:  Recurse subdirectories.
-t:  Teamcity error and warning messages.

Example: VerifyReferences -r .";

            var parsedArgs = args;

            var parseSubdirs = parsedArgs.Any(a => a == "-r");
            parsedArgs = [.. parsedArgs.Where(a => a != "-r")];

            var teamcityErrorMessage = parsedArgs.Any(a => a == "-t");
            parsedArgs = [.. parsedArgs.Where(a => a != "-t")];

            if (parsedArgs.Length != 1)
            {
                Console.WriteLine(usage);
                return 1;
            }

            var path = parsedArgs[0];

            string[] projectpaths = [.. Directory.GetFiles(path, "*.*proj", parseSubdirs ? SearchOption.AllDirectories : SearchOption.TopDirectoryOnly)
                .Select(p => p.StartsWith(@".\") ? p[2..] : p)];

            Console.WriteLine("Loading " + projectpaths.Length + " projects...");

            List<Project> projects = [];

            foreach (var projectpath in projectpaths)
            {
                projects.Add(new Project(projectpath, teamcityErrorMessage));
            }

            return Validate(projects) ? 0 : 1;
        }

        static bool Validate(List<Project> projects)
        {
            (string shortinclude, (string path, string[] projectfiles)[] paths)[] refs = [.. projects
                .SelectMany(p => p.References, (p, r) =>
                    (
                        project: p,
                        reference: r
                    ))
                .GroupBy(r => r.reference.Shortinclude, (shortinclude, references) =>
                {
                    (string shortinclude, (string path, string[] projectfiles)[] paths) y =
                    (
                        shortinclude,
                        paths: [.. references
                            .Select(rr =>
                                (
                                    rr.reference.Path,
                                    projectfiles: references
                                        .Select(p => p.project.ProjectFile)
                                ))
                                .GroupBy(r => r.Path, (path, projects2) =>
                                {
                                    (string path, string[] projectfiles) x =
                                    (
                                        path,
                                        projectfiles: [.. projects2
                                            .SelectMany(p => p.projectfiles)
                                            .Distinct()
                                            .OrderBy(f => f)]
                                    );
                                    return x;
                                })
                                .OrderBy(r => r.path)]
                    );
                    return y;
                })
                .OrderBy(r => r.shortinclude)];

            (string shortinclude, (string path, string[] projectfiles)[] paths)[] failrefs = [.. refs.Where(r => r.paths.Length > 1)];

            Console.WriteLine("Found " + refs.Length + " references, " + failrefs.Length + " inconsistent.");

            foreach (var (shortinclude, paths) in failrefs)
            {
                var count1 = paths.Length;
                var count2 = paths.Length;

                ConsoleHelper.WriteLineColor(shortinclude + " (" + count1 + "/" + count2 + ")", ConsoleColor.Cyan);

                foreach (var path in paths)
                {
                    Console.WriteLine("  " + path.path);
                    foreach (var projectfile in path.projectfiles)
                    {
                        Console.WriteLine("    " + projectfile);
                    }
                }
            }

            if (failrefs.Length > 0)
            {
                ConsoleHelper.WriteLineColor("Inconsistencies found in " + failrefs.Length + " projects.", ConsoleColor.Red);
                return false;
            }

            ConsoleHelper.WriteLineColor("All good!", ConsoleColor.Green);
            return true;
        }
    }
}
