using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace CheckMissingFiles
{
    class Program
    {
        static int Main(string[] args)
        {
            ConsoleHelper.HasWritten = false;

            int result = CheckFiles(args);

            if (ConsoleHelper.HasWritten &&
                Environment.UserInteractive &&
                string.IsNullOrEmpty(Environment.GetEnvironmentVariable("DontPrompt")))
            {
                Console.WriteLine($"{Environment.NewLine}Press any key to continue...");
                Console.ReadKey();
            }

            return result;
        }

        static int CheckFiles(string[] args)
        {
            string usage = @"CheckMissingFiles 4.0

Usage: CheckMissingFiles [-b] [-esolution file 1,solution file 2] [-r] [-t] <solution path/pattern>

-b:  Reverse check - warn if files exists in file system and but missing in project files.
-e:  Exclude solutions.
-r:  Recurse subdirectories.
-t:  Teamcity error and warning messages.

Example: CheckMissingFiles -eHello2.sln,Hello3.sln hello*.sln";


            bool reverseCheck = args.Any(a => a == "-b");
            args = args.Where(a => a != "-b").ToArray();

            string[] excludeSolutions = args
                .Where(a => a.StartsWith("-e"))
                .SelectMany(a => a.Substring(2).Split(','))
                .ToArray();
            args = args.Where(a => !a.StartsWith("-e")).ToArray();

            bool parseSubdirs = args.Any(a => a == "-r");
            args = args.Where(a => a != "-r").ToArray();

            bool teamcityErrorMessage = args.Any(a => a == "-t");
            args = args.Where(a => a != "-t").ToArray();


            if (args.Length != 1 || args[0] == string.Empty)
            {
                ConsoleHelper.WriteLine(usage);
                return 1;
            }

            string path = args[0];

            int result = CheckSolutions(path, excludeSolutions, parseSubdirs, reverseCheck, teamcityErrorMessage);
            return result;
        }

        private static int CheckSolutions(string path, string[] excludeSolutions, bool parseSubdirs, bool reverseCheck, bool teamcityErrorMessage)
        {
            int result = 0;

            List<string> solutionFiles = GetFiles(path, parseSubdirs);
            if (solutionFiles == null)
            {
                return 1;
            }

            ConsoleHelper.WriteLine($"Found {solutionFiles.Count()} solutions.");

            List<string> excessiveExcludes = new List<string>();
            foreach (string excludeFile in excludeSolutions)
            {
                List<string> removeFiles = solutionFiles.Where(f => Path.GetFileName(f) == excludeFile).ToList();

                if (removeFiles.Count() >= 1)
                {
                    ConsoleHelper.WriteLine($"Excluding {removeFiles.Count()} solutions: '{excludeFile}'");
                    solutionFiles.RemoveAll(f => Path.GetFileName(f) == excludeFile);
                }
                else
                {
                    excessiveExcludes.Add(excludeFile);
                }
            }

            if (excessiveExcludes.Count() > 0)
            {
                ConsoleHelper.WriteLineColor(
                    "Missing files cannot be excluded. Please keep the exclude filter tidy by removing the missing files from the exclude filter. ASAP.",
                    ConsoleColor.Red);
                ConsoleHelper.WriteLineColor(
                    $"The following {excessiveExcludes.Count()} files couldn't be excluded: '" + string.Join("', '", excessiveExcludes) + "'",
                    ConsoleColor.Red);
                result = 1;
            }

            ConsoleHelper.WriteLine($"Loading {solutionFiles.Count()} solutions...");

            List<Solution> solutions = new List<Solution>();
            foreach (string solutionFile in solutionFiles)
            {
                try
                {
                    solutions.Add(new Solution(solutionFile, teamcityErrorMessage));
                }
                catch (ApplicationException ex)
                {
                    ConsoleHelper.WriteLineColor(ex.Message, ConsoleColor.Red);
                    result = 1;
                }
            }


            bool loadError;
            List<Project> projects = LoadProjects(solutions, teamcityErrorMessage, out loadError);

            bool check = CheckProjects(projects, reverseCheck);
            if (loadError || !check)
            {
                result = 1;
            }


            return result;
        }

        private static List<string> GetFiles(string path, bool parseSubdirs)
        {
            string searchPath, searchPattern;
            if (Directory.Exists(path))
            {
                searchPath = path;
                searchPattern = "*.sln";
            }
            else
            {
                if (path.Contains(Path.DirectorySeparatorChar))
                {
                    searchPath = Path.GetDirectoryName(path);
                    searchPattern = Path.GetFileName(path);
                }
                else
                {
                    searchPath = ".";
                    searchPattern = path;
                }
            }
            SearchOption searchOption = parseSubdirs ? SearchOption.AllDirectories : SearchOption.TopDirectoryOnly;

            ConsoleHelper.WriteLine($"Searching: '{searchPath}' for '{searchPattern}'...");

            string[] files;
            try
            {
                files = Directory.GetFiles(searchPath, searchPattern, searchOption);
            }
            catch (DirectoryNotFoundException ex)
            {
                ConsoleHelper.WriteLineColor(ex.Message, ConsoleColor.Red);
                return null;
            }

            List<string> solutionFiles = files
                .Select(f => f.StartsWith(@".\") ? f.Substring(2) : f)
                .ToList();

            return solutionFiles;
        }

        private static List<Project> LoadProjects(List<Solution> solutions, bool teamcityErrorMessage, out bool loadError)
        {
            var projectsToLoad = solutions
                .SelectMany(s =>
                    s.projectsPaths, (s, relpath) =>
                        new
                        {
                            solutionFile = s.solutionFile,
                            projectPath = CompactPath(Path.Combine(Path.GetDirectoryName(s.solutionFile), relpath))
                        })
                .GroupBy(p => p.projectPath, (projectPath, projs) =>
                    new
                    {
                        projectPath = projectPath,
                        solutionFiles = projs.Select(proj => proj.solutionFile).ToArray()
                    })
                .OrderBy(p => p.projectPath)
                .ToArray();


            ConsoleHelper.WriteLine($"Loading {projectsToLoad.Length} projects...");

            loadError = false;

            List<Project> projects = new List<Project>();

            foreach (var projectToLoad in projectsToLoad)
            {
                Project project;
                try
                {
                    project = new Project(projectToLoad.projectPath, projectToLoad.solutionFiles, teamcityErrorMessage);
                }
                catch (ApplicationException ex)
                {
                    ConsoleHelper.WriteLineColor(ex.Message, ConsoleColor.Red);
                    loadError = true;
                    continue;
                }

                projects.Add(project);
            }

            return projects;
        }

        public static string CompactPath(string path)
        {
            List<string> folders = path.Split(Path.DirectorySeparatorChar).ToList();

            for (int i = 0; i < folders.Count;)
            {
                if (i > 0 && folders[i] == ".." && folders[i - 1] != ".." && folders[i - 1] != "")
                {
                    folders.RemoveAt(i - 1);
                    folders.RemoveAt(i - 1);
                    i--;
                }
                else if (i > 0 && folders[i] == "" && folders[i - 1] == "")
                {
                    folders.RemoveAt(i);
                }
                else
                {
                    i++;
                }
            }

            string path2 = string.Join(Path.DirectorySeparatorChar.ToString(), folders.ToArray());

            string sep = Path.DirectorySeparatorChar.ToString();
            if (path2 == "" && (path.StartsWith(sep) || path.EndsWith(sep)))
            {
                path2 = Path.DirectorySeparatorChar.ToString();
            }

            return path2;
        }

        public static bool CheckProjects(List<Project> projects, bool reverseCheck)
        {
            foreach (Project p in projects.OrderBy(p => p.projectFile))
            {
                p.Check(reverseCheck);
            }

            bool parseError = projects.Any(p => p.parseError);

            int missingfilesError = projects.Select(p => p.missingfilesError).Sum();
            int missingfilesWarning = projects.Select(p => p.missingfilesWarning).Sum();
            int excessfiles = projects.Select(p => p.excessfiles).Sum();

            string msg = $"Parsed {projects.Count} projects, found";

            if (reverseCheck)
            {
                if (excessfiles == 0)
                {
                    ConsoleHelper.WriteLine($"{msg} no excess files.");
                }
                else
                {
                    ConsoleHelper.WriteLine($"{msg} {excessfiles} files in file system that wasn't included in project files.");
                }
            }
            else
            {
                if (missingfilesError == 0)
                {
                    if (missingfilesWarning == 0)
                    {
                        ConsoleHelper.WriteLine($"{msg} no missing files.");
                    }
                    else
                    {
                        ConsoleHelper.WriteLine($"{msg} no missing files (although {missingfilesWarning} missing files with None build action).");
                    }
                }
                else
                {
                    if (missingfilesWarning == 0)
                    {
                        ConsoleHelper.WriteLine($"{msg} {missingfilesError} missing files.");
                    }
                    else
                    {
                        ConsoleHelper.WriteLine($"{msg} {missingfilesError} missing files (and {missingfilesWarning} missing files with None build action).");
                    }
                }
            }

            if (parseError == false && missingfilesError == 0)
            {
                return true;
            }
            else
            {
                return false;
            }
        }
    }
}
