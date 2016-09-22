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
@"GatherOutputAssemblies 1.9 - Program for gathering compiled output from Visual Studio.

Usage: GatherOutputAssemblies [-a] [-v] [-w] <solutionfiles> <buildconfig> <outputfolder> +include1... -exclude1...

solutionfiles:  Comma separated list of solution files.
buildconfig:    Name of build config to be able to find a distinct output folder of.
                each project Can be a part of the full name, e.g. Debug or Release.
outputfolder:   Folder where all project output will be copied to, one folder
                for each project will be created here.

-a:    Copy all projects.
-v:    Verbose logging.
-w:    TODO! Also include Web/Mvc projects. Instead of using this flag, please consider
       to *Publish* Web/Mvc projects, that's the better approach because only VS
       knows how to publish/gather a Web/Mvc project, it's not easy to do this right.
       Of course, some time in the future this program might do exactly that, i.e.
       call VS to perform a publish by the book.

+/-:   Additional projects which should always be included/excluded.
       Wildcards are supported, -*test* might be useful.

Example: GatherOutputAssemblies mysol.sln ""Release|AnyCPU"" artifacts -*Tests

This program copies files from project output folders. Although not from all projects
included in the solution, only from the *resulting* subset. It is useful when you have
a solution with many projects and doesn't want to maintain hard coded project names or
paths.

Resulting projects are projects which doesn't have any reference to them, it is
usually exe and web projects, although test projects usually wreak havoc with
this assumption and usually needs to be excluded to make projects referenced by
test projects copied to the output folder.";


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


            string[] includeProjects =
                args
                .Where(a => a.StartsWith("+"))
                .Select(a => a.Substring(1))
                .ToArray();

            string[] excludeProjects =
                args
                .Where(a => a.StartsWith("-"))
                .Select(a => a.Substring(1))
                .ToArray();

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

            string[] solutionfiles = args[0].Split(',');
            string buildconfig = args[1];
            string outputpath = args[2];


            List<Solution> solutions = solutionfiles
                .Select(s => new Solution(s))
                .ToList();

            string[] webmvcguids =
            {
                "{603C0E0B-DB56-11DC-BE95-000D561079B0}",
                "{F85E285D-A4E0-4152-9332-AB1D724D3325}",
                "{E53F8FEA-EAE0-44A6-8774-FFD645390401}",
                "{E3E379DF-F4C6-4180-9B81-6769533ABE47}",
                "{349C5851-65DF-11DA-9384-00065B846F21}"
            };

            List<Project> projects = solutions
                .SelectMany(s => s.LoadProjects())
                .ToList();
            if (projects == null)
            {
                return 1;
            }

            int result = Solution.CopyProjectOutput(projects, buildconfig, outputpath, includeProjects, excludeProjects, webmvcguids, verbose, gatherall);

            return result;
        }
    }
}
