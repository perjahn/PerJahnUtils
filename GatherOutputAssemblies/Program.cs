using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;

namespace GatherOutputAssemblies
{
    class Program
    {
        static int Main(string[] args)
        {
            // Make all string comparisons (and sort/order) invariant of current culture
            // Thus, project output files is written in a consistent manner
            System.Threading.Thread.CurrentThread.CurrentCulture = System.Globalization.CultureInfo.InvariantCulture;

            string usage =
@"GatherOutputAssemblies 1.6 - Program for gathering compiled output from Visual Studio.

Usage: GatherOutputAssemblies [-a] [-v] [-w] <solutionfile> <buildconfig> <outputfolder> +include1... -exclude1...

-a:    Copy all projects.
-v:    Verbose logging.
-w:    Also include Web/Mvc projects. Instead of using this flag, please consider TODO
       to *Publish* Web/Mvc projects, that's the better approach because only VS
       knows how to publish/gather a Web/Mvc project, it's not easy to do this right.
       Of course, some time in the future this program might do exactly that, i.e.
       call VS to perform a publish by the book.

+/-:   Additional projects which should always be included/excluded.
       Wild cards? Not yet implemented, maybe later (*test* should be useful)

Example: GatherOutputAssemblies mysol.sln ""Release|AnyCPU"" artifacts

This program copies files from project output folders. Although not from all projects
included in the solution, only from the *resulting* subset. It is useful when you have
a solution with many projects and doesn't want to maintain hard coded project names or
paths.

Resulting projects are projects which doesn't have any reference to them, it is
usually exe and web projects, although test projects usually wreak havoc with
this assumption and usually needs to be excluded.";


            bool verbose = false;
            bool gatherall = false;

            if (args.Contains("-v"))
            {
                verbose = true;
                args = args.Where(a => a != "-v").ToArray();
            }
            if (args.Contains("-a"))
            {
                gatherall = true;
                args = args.Where(a => a != "-a").ToArray();
            }


            List<string> includeProjects =
                args
                .Where(a => a.StartsWith("+"))
                .Select(a => a.Substring(1))
                .ToList();

            List<string> excludeProjects =
                args
                .Where(a => a.StartsWith("-"))
                .Select(a => a.Substring(1))
                .ToList();

            args =
                args
                .Where(a => !a.StartsWith("+"))
                .Where(a => !a.StartsWith("-"))
                .ToArray();

            if (args.Length != 3)
            {
                Console.WriteLine(usage);
                return 0;
            }

            string solutionfile = args[0];
            string buildconfig = args[1];
            string outputpath = args[2];


            Solution s = new Solution(solutionfile);

            string[] webmvcguids =
            {
                "{603C0E0B-DB56-11DC-BE95-000D561079B0}",
                "{F85E285D-A4E0-4152-9332-AB1D724D3325}",
                "{E53F8FEA-EAE0-44A6-8774-FFD645390401}",
                "{E3E379DF-F4C6-4180-9B81-6769533ABE47}",
                "{349C5851-65DF-11DA-9384-00065B846F21}"
            };

            List<Project> projects = s.LoadProjects();
            if (projects == null)
            {
                return 1;
            }

            int result = s.CopyProjectOutput(projects, buildconfig, outputpath, includeProjects, excludeProjects, webmvcguids, verbose, gatherall);

            return result;
        }
    }
}
