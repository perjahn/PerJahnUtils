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
        static bool _verbose = false;

        static int Main(string[] args)
        {
            string usage =
@"VerifyReferences 0.1 - Verifies that refereces in projects are to same assemblies.

Usage: VerifyReferences <path>

Example: VerifyReferences .";

            if (args.Length != 1)
            {
                ConsoleHelper.WriteLineColor(usage, ConsoleColor.Red);
                return 1;
            }

            string path = args[0];

            string[] projectpaths = Directory.GetFiles(path, "*.*proj", SearchOption.AllDirectories)
                .Select(p => p.StartsWith(@".\") ? p.Substring(2) : p)
                .ToArray();

            Console.WriteLine("Loading " + projectpaths.Length + " projects...");

            List<Project> projects = new List<Project>();

            foreach (string projectpath in projectpaths)
            {
                projects.Add(new Project(projectpath));
            }


            return Validate(projects) ? 0 : 1;
        }

        static bool Validate(List<Project> projects)
        {
            var refs = projects
                .SelectMany(p =>new { r = p.references },)
                .ToLookup(p => p.shortinclude)
                .OrderBy(p => p.Key)
                .ToList();

            Console.WriteLine("Found " + refs.Count() + " references.");


            var failrefs = refs
                .Where(g => !g.All(r => r.path == g.First().path))
                .ToList();


            foreach (var failref in failrefs)
            {
                ConsoleHelper.WriteLineColor(failref.Key, ConsoleColor.Cyan);
                foreach (var project in failref)
                {
                    ConsoleHelper.WriteLineColor(failref.Key, ConsoleColor.Cyan);
                }

                Console.WriteLine("Non-unique hint path: '" + string.Join("', '", refgroup.Select(r => r.path)) + "'");
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
