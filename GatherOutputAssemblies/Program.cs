using System;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading;

namespace GatherOutputAssemblies
{
    class Program
    {
        static int Main(string[] args)
        {
            // Make all string comparisons (and sort/order) invariant of current culture
            // Thus, project output files is written in a consistent manner
            Thread.CurrentThread.CurrentCulture = CultureInfo.InvariantCulture;

            var usage =
@"GatherOutputAssemblies 2.0 - Program for gathering compiled output from Visual Studio.

Usage: GatherOutputAssemblies [-a] [-d] [-r] [-s] [-v] [-w] <solutionfiles> <buildconfig> <outputfolder> +include1... -exclude1...

solutionfiles:  Comma separated list of solution files. Wildcard patterns allowed.
buildconfig:    Name of build config to be able to find a distinct output folder of.
                each project Can be a part of the full name, e.g. Debug or Release.
outputfolder:   Folder where all project output will be copied to, one subfolder
                for each project will be created here.

-a:    Copy all projects.
-d:    Delete target folder before copying, if it exists.
-r:    Recurse subfolders when matching solution filenames.
-s:    Simulate, dry run.
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

            var gatherall = false;
            var deletetargetfolder = false;
            var recurse = false;
            var simulate = false;
            var verbose = false;

            var parsedArgs = args;

            if (parsedArgs.Contains("-a"))
            {
                gatherall = true;
                parsedArgs = [.. parsedArgs.Where(a => a != "-a")];
            }
            if (parsedArgs.Contains("-d"))
            {
                deletetargetfolder = true;
                parsedArgs = [.. parsedArgs.Where(a => a != "-d")];
            }
            if (parsedArgs.Contains("-r"))
            {
                recurse = true;
                parsedArgs = [.. parsedArgs.Where(a => a != "-r")];
            }
            if (parsedArgs.Contains("-s"))
            {
                simulate = true;
                parsedArgs = [.. parsedArgs.Where(a => a != "-s")];
            }
            if (parsedArgs.Contains("-v"))
            {
                verbose = true;
                parsedArgs = [.. parsedArgs.Where(a => a != "-v")];
            }

            string[] includeProjects = [.. parsedArgs.Where(a => a.StartsWith('+')).Select(a => a[1..])];
            string[] excludeProjects = [.. parsedArgs.Where(a => a.StartsWith('-')).Select(a => a[1..])];

            parsedArgs = [.. parsedArgs.Where(a => !a.StartsWith('+')).Where(a => !a.StartsWith('-'))];

            if (parsedArgs.Length != 3)
            {
                Console.WriteLine(usage);
                return 0;
            }

            var solutionfiles = parsedArgs[0].Split(',');
            solutionfiles = [.. solutionfiles
                .SelectMany(f =>
                {
                    var dir = Path.GetDirectoryName(f);
                    var pattern = Path.GetFileName(f);
                    if (dir == string.Empty)
                    {
                        dir = ".";
                    }
                    Console.WriteLine("Dir: '" + dir + "', Pattern: '" + pattern + "'");
                    string[] files = [.. Directory.GetFiles(dir, pattern, recurse ? SearchOption.AllDirectories : SearchOption.TopDirectoryOnly)
                        .Select(ff => ff.StartsWith(@".\") ? ff[2..] : ff)];
                    return files;
                })];

            Console.WriteLine("Found " + solutionfiles.Length + " solutions.");
            foreach (var filename in solutionfiles)
            {
                Console.WriteLine("'" + filename + "'");
            }

            var buildconfig = parsedArgs[1];
            var outputpath = parsedArgs[2];

            return LoadSolutions(solutionfiles, buildconfig, outputpath, includeProjects, excludeProjects, deletetargetfolder, gatherall, simulate, verbose);
        }

        private static int LoadSolutions(string[] solutionfiles,
            string buildconfig, string outputpath, string[] includeProjects, string[] excludeProjects,
            bool deletetargetfolder, bool gatherall, bool simulate, bool verbose)
        {
            Console.WriteLine("Loading " + solutionfiles.Length + " solutions...");

            Solution[] solutions = [.. solutionfiles.Select(s => new Solution(s))];

            string[] projectfiles = [.. solutions
                .SelectMany(s => s.Projectfiles)
                .Distinct()
                .OrderBy(f => f)];

            Console.WriteLine("Loading " + projectfiles.Length + " projects...");

            Project[] projects = [.. projectfiles.Select(Project.LoadProject).Where(p => p != null)];

            foreach (var project in projects)
            {
                project._solutionfiles = [.. solutions
                    .Where(s => s.Projectfiles.Contains(project._path))
                    .Select(s => s._path)
                    .OrderBy(f => f)];
            }

            var result = Solution.CopyProjectOutput(projects, buildconfig, outputpath, includeProjects, excludeProjects, deletetargetfolder, gatherall, simulate, verbose);

            return result;
        }
    }
}
