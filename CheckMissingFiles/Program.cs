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

            var success = CheckFiles(args);

            if (ConsoleHelper.HasWritten &&
                Environment.UserInteractive &&
                string.IsNullOrEmpty(Environment.GetEnvironmentVariable("DontPrompt")))
            {
                Console.WriteLine($"{Environment.NewLine}Press any key to continue...");
                _ = Console.ReadKey();
            }

            return success;
        }

        static int CheckFiles(string[] args)
        {
            var usage = @"CheckMissingFiles 4.0

Usage: CheckMissingFiles [-b] [-esolution file 1,solution file 2] [-r] [-t] <solution path/pattern>

-b:  Reverse check - warn if files exists in file system and but missing in project files.
-e:  Exclude solutions.
-r:  Recurse subdirectories.
-t:  Teamcity error and warning messages.

Example: CheckMissingFiles -eHello2.sln,Hello3.sln hello*.sln";

            var parsedArgs = args;

            var reverseCheck = parsedArgs.Any(a => a == "-b");
            parsedArgs = [.. parsedArgs.Where(a => a != "-b")];

            string[] excludeSolutions = [.. parsedArgs.Where(a => a.StartsWith("-e")).SelectMany(a => a[2..].Split(','))];
            parsedArgs = [.. parsedArgs.Where(a => !a.StartsWith("-e"))];

            var parseSubdirs = parsedArgs.Any(a => a == "-r");
            parsedArgs = [.. parsedArgs.Where(a => a != "-r")];

            var teamcityErrorMessage = parsedArgs.Any(a => a == "-t");
            parsedArgs = [.. parsedArgs.Where(a => a != "-t")];

            if (parsedArgs.Length != 1 || parsedArgs[0] == string.Empty)
            {
                ConsoleHelper.WriteLine(usage);
                return 1;
            }

            var path = parsedArgs[0];

            var success = CheckSolutions(path, excludeSolutions, parseSubdirs, reverseCheck, teamcityErrorMessage);
            return success;
        }

        private static int CheckSolutions(string path, string[] excludeSolutions, bool parseSubdirs, bool reverseCheck, bool teamcityErrorMessage)
        {
            var success = 0;

            var solutionFiles = GetFiles(path, parseSubdirs);
            if (solutionFiles == null)
            {
                return 1;
            }

            ConsoleHelper.WriteLine($"Found {solutionFiles.Count} solutions.");

            List<string> excessiveExcludes = [];
            foreach (var excludeFile in excludeSolutions)
            {
                List<string> removeFiles = [.. solutionFiles.Where(f => Path.GetFileName(f) == excludeFile)];

                if (removeFiles.Count >= 1)
                {
                    ConsoleHelper.WriteLine($"Excluding {removeFiles.Count} solutions: '{excludeFile}'");
                    _ = solutionFiles.RemoveAll(f => Path.GetFileName(f) == excludeFile);
                }
                else
                {
                    excessiveExcludes.Add(excludeFile);
                }
            }

            if (excessiveExcludes.Count > 0)
            {
                ConsoleHelper.WriteLineColor(
                    "Missing files cannot be excluded. Please keep the exclude filter tidy by removing the missing files from the exclude filter. ASAP.",
                    ConsoleColor.Red);
                ConsoleHelper.WriteLineColor(
                    $"The following {excessiveExcludes.Count} files couldn't be excluded: '{string.Join("', '", excessiveExcludes)}'",
                    ConsoleColor.Red);
                success = 1;
            }

            ConsoleHelper.WriteLine($"Loading {solutionFiles.Count} solutions...");

            List<Solution> solutions = [];
            foreach (var solutionFile in solutionFiles)
            {
                try
                {
                    solutions.Add(new Solution(solutionFile, teamcityErrorMessage));
                }
                catch (ApplicationException ex)
                {
                    ConsoleHelper.WriteLineColor(ex.Message, ConsoleColor.Red);
                    success = 1;
                }
            }

            var projects = LoadProjects(solutions, teamcityErrorMessage, out bool loadError);

            var check = CheckProjects(projects, reverseCheck);
            if (loadError || !check)
            {
                success = 1;
            }

            return success;
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
            var searchOption = parseSubdirs ? SearchOption.AllDirectories : SearchOption.TopDirectoryOnly;

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

            List<string> solutionFiles = [.. files.Select(f => f.StartsWith(@".\") ? f[2..] : f)];

            return solutionFiles;
        }

        private static List<Project> LoadProjects(List<Solution> solutions, bool teamcityErrorMessage, out bool loadError)
        {
            (string projectPath, string[] solutionFiles)[] projectsToLoad = [.. solutions
                .SelectMany(s =>
                    s.ProjectsPaths, (s, relpath) =>
                        (
                            solutionFile: s.SolutionFile,
                            projectPath: CompactPath(Path.Combine(Path.GetDirectoryName(s.SolutionFile), relpath))
                        ))
                .GroupBy(p => p.projectPath, (projectPath, projs) =>
                {
                    (string projectPath, string[] solutionFiles) x =
                    (
                        projectPath,
                        solutionFiles: [.. projs.Select(proj => proj.solutionFile)]
                    );
                    return x;
                })
                .OrderBy(p => p.projectPath)];

            ConsoleHelper.WriteLine($"Loading {projectsToLoad.Length} projects...");

            loadError = false;

            List<Project> projects = [];

            foreach (var (projectPath, solutionFiles) in projectsToLoad)
            {
                Project project;
                try
                {
                    project = new(projectPath, solutionFiles, teamcityErrorMessage);
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
            List<string> folders = [.. path.Split(Path.DirectorySeparatorChar)];

            for (var i = 0; i < folders.Count;)
            {
                if (i > 0 && folders[i] == ".." && folders[i - 1] != ".." && folders[i - 1] != string.Empty)
                {
                    folders.RemoveAt(i - 1);
                    folders.RemoveAt(i - 1);
                    i--;
                }
                else if (i > 0 && folders[i] == string.Empty && folders[i - 1] == string.Empty)
                {
                    folders.RemoveAt(i);
                }
                else
                {
                    i++;
                }
            }

            var path2 = string.Join(Path.DirectorySeparatorChar.ToString(), folders);

            var sep = Path.DirectorySeparatorChar.ToString();
            if (path2 == string.Empty && (path.StartsWith(sep) || path.EndsWith(sep)))
            {
                path2 = Path.DirectorySeparatorChar.ToString();
            }

            return path2;
        }

        public static bool CheckProjects(List<Project> projects, bool reverseCheck)
        {
            foreach (var p in projects.OrderBy(p => p.ProjectFile))
            {
                p.Check(reverseCheck);
            }

            var parseError = projects.Any(p => p.ParseError);

            var missingfilesError = projects.Sum(p => p.MissingfilesError);
            var missingfilesWarning = projects.Sum(p => p.MissingfilesWarning);
            var excessfiles = projects.Sum(p => p.Excessfiles);

            var msg = $"Parsed {projects.Count} projects, found";

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

            return !parseError && missingfilesError == 0;
        }
    }
}
