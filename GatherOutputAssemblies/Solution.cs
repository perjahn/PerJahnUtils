using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace GatherOutputAssemblies
{
    class Solution
    {
        public string SolutionPath { get; set; }
        public string[] Projectfiles { get; set; } = [];

        public Solution(string solutionfile)
        {
            SolutionPath = solutionfile;

            List<string> projects = [];

            string[] rows;
            try
            {
                rows = File.ReadAllLines(solutionfile);
            }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or ArgumentException)
            {
                ConsoleHelper.ColorWriteLine(ConsoleColor.Red, $"Couldn't load solution: '{solutionfile}': {ex.Message}");
                return;
            }

            foreach (var row in rows)
            {
                // Project("{FAE04EC0-301F-11D3-BF4B-00C04F79EFBC}") = "MyCsProject", "Folder\Folder\MyCsProject.csproj", "{01010101-0101-0101-0101-010101010101}"
                // Project("{F184B08F-C81C-45F6-A57F-5ABD9991F28F}") = "MyVbProject", "Folder\Folder\MyVbProject.vbproj", "{02020202-0202-0202-0202-020202020202}"
                // Project("{8BC9CEB8-8B4A-11D0-8D11-00A0C91BC942}") = "MyVcProject", "Folder\Folder\MyVcProject.vcxproj", "{03030303-0303-0303-0303-030303030303}"

                string[] projtypeguids = [
                    "{FAE04EC0-301F-11D3-BF4B-00C04F79EFBC}",
                    "{F184B08F-C81C-45F6-A57F-5ABD9991F28F}",
                    "{8BC9CEB8-8B4A-11D0-8D11-00A0C91BC942}" ];

                foreach (var projtypeguid in projtypeguids)
                {
                    var projtypeline = $"Project(\"{projtypeguid}\") =";

                    if (row.StartsWith(projtypeline))
                    {
                        var values = row[projtypeline.Length..].Split(',');
                        if (values.Length != 3)
                        {
                            continue;
                        }

                        var path = values[1].Trim().Trim('"');

                        projects.Add(FileHelper.CompactPath(Path.Combine(Path.GetDirectoryName(solutionfile), path)));
                    }
                }
            }

            Projectfiles = [.. projects];
        }

        public static int CopyProjectOutput(Project[] projects, string buildconfig, string outputpath, string[] includeProjects,
            string[] excludeProjects, bool deletetargetfolder, bool gatherall, bool simulate, bool verbose)
        {
            var result = 0;

            // If a project is excluded, it should not prevent referred projects from being included.

            List<Project> projects2 = [.. projects];

            projects2 = ExcludeCorruptProjects(projects2, verbose);
            projects2 = ExcludeExplicitProjects(projects2, excludeProjects, verbose);
            projects2 = ExcludeReferredProjects(projects2, gatherall, includeProjects, verbose);
            projects2 = ExcludeWebMvcProjects(projects2, verbose);

            Console.WriteLine($"Retrieving output folders for {projects2.Count} projects.");

            (string sourcepath, string targetpath)[] operations = [.. projects2
                .Select(p =>
                    (
                        sourcepath: p.GetOutputFolder(buildconfig, verbose),
                        targetpath: Path.Combine(outputpath, FileHelper.GetCleanFolderName(Path.GetFileNameWithoutExtension(p.ProjectPath)))
                    ))
                .Where(p => p.sourcepath != null)];

            if (deletetargetfolder && Directory.Exists(outputpath))
            {
                Console.WriteLine($"Deleting folder: '{outputpath}'");
                Directory.Delete(outputpath, true);
            }

            Console.WriteLine($"Copying {operations.Length} projects.");

            var copiedFiles = 0;

            foreach (var operation in operations)
            {
                var sourcepath = operation.sourcepath;
                var targetpath = operation.targetpath;

                ConsoleHelper.ColorWriteLine(ConsoleColor.Cyan, $"Copying folder: '{sourcepath}' -> '{targetpath}'");

                if (!FileHelper.CopyFolder(new(sourcepath), new(targetpath), simulate, verbose, ref copiedFiles))
                {
                    result = 1;
                }
            }

            Console.WriteLine($"Copied {copiedFiles} files.");

            return result;
        }

        private static List<Project> ExcludeCorruptProjects(List<Project> projects, bool verbose)
        {
            List<Project> resultingProjects = [];
            foreach (var project in projects)
            {
                if (verbose)
                {
                    ConsoleHelper.ColorWriteLine(ConsoleColor.Blue, $"Evaluating project (unusable): '{project.ProjectPath}'");
                }

                if (project.Proj_guid == null || project.ProjectTypeGuids == null)
                {
                    ConsoleHelper.ColorWrite(ConsoleColor.Blue, "Excluding unusable project: '");
                    ConsoleHelper.ColorWrite(ConsoleColor.DarkCyan, project.ProjectPath);
                    ConsoleHelper.ColorWriteLine(ConsoleColor.Blue, "'");
                    continue;
                }

                resultingProjects.Add(project);
            }

            return resultingProjects;
        }

        private static List<Project> ExcludeWebMvcProjects(List<Project> projects, bool verbose)
        {
            string[] webmvcguids =
            [
                "{603C0E0B-DB56-11DC-BE95-000D561079B0}",
                "{F85E285D-A4E0-4152-9332-AB1D724D3325}",
                "{E53F8FEA-EAE0-44A6-8774-FFD645390401}",
                "{E3E379DF-F4C6-4180-9B81-6769533ABE47}",
                "{349C5851-65DF-11DA-9384-00065B846F21}"
            ];

            List<Project> resultingProjects = [];
            foreach (var project in projects)
            {
                if (verbose)
                {
                    ConsoleHelper.ColorWriteLine(ConsoleColor.Blue, $"Evaluating project (web/mvc): '{project.ProjectPath}'");
                }

                if (project.ProjectTypeGuids.Any(g1 => webmvcguids.Any(g2 => string.Compare(g1, g2, true) == 0)))
                {
                    ConsoleHelper.ColorWrite(ConsoleColor.Blue, "Excluding web/mvc project: '");
                    ConsoleHelper.ColorWrite(ConsoleColor.DarkCyan, project.ProjectPath);
                    ConsoleHelper.ColorWriteLine(ConsoleColor.Blue, "'");
                    continue;
                }

                resultingProjects.Add(project);
            }

            return resultingProjects;
        }

        private static List<Project> ExcludeExplicitProjects(List<Project> projects, string[] excludeProjects, bool verbose)
        {
            Dictionary<string, bool> used = [];
            foreach (var project in excludeProjects)
            {
                used[project] = false;
            }

            List<Project> resultingProjects = [];
            foreach (var project in projects)
            {
                if (verbose)
                {
                    ConsoleHelper.ColorWriteLine(ConsoleColor.Blue, $"Evaluating project (explicit): '{project.ProjectPath}'");
                }

                string[] matches = [.. excludeProjects.Where(x => IsWildcardMatch(project.ProjectPath, x))];

                if (matches.Length > 0)
                {
                    ConsoleHelper.ColorWrite(ConsoleColor.Blue, "Excluding explicit project: '");
                    ConsoleHelper.ColorWrite(ConsoleColor.DarkCyan, project.ProjectPath);
                    ConsoleHelper.ColorWriteLine(ConsoleColor.Blue, "'");

                    foreach (var excludematch in matches)
                    {
                        used[excludematch] = true;
                    }

                    continue;
                }

                resultingProjects.Add(project);
            }

            foreach (var project in excludeProjects)
            {
                if (!used[project])
                {
                    ConsoleHelper.ColorWriteLine(ConsoleColor.Yellow, $"Excessive exclude filter unused: '{project}'");
                }
            }

            return resultingProjects;
        }

        private static List<Project> ExcludeReferredProjects(List<Project> projects, bool gatherall, string[] includeProjects, bool verbose)
        {
            List<Project> resultingProjects = [];
            foreach (var project in projects)
            {
                if (verbose)
                {
                    ConsoleHelper.ColorWriteLine(ConsoleColor.Blue, $"Evaluating project (referred): '{project.ProjectPath}'");
                }

                var include = includeProjects.Contains(Path.GetFileNameWithoutExtension(project.ProjectPath));
                var referred = projects.Any(p => p.ProjectReferences.Any(r => Path.GetFileName(r.Include) == Path.GetFileName(project.ProjectPath)));

                if (gatherall || include || !referred)
                {
                    resultingProjects.Add(project);
                }
                else
                {
                    var refs = "'" +
                        string.Join("', '",
                            projects
                                .Where(p => p.ProjectReferences.Any(r => Path.GetFileName(r.Include) == Path.GetFileName(project.ProjectPath)))
                                .OrderBy(p => Path.GetFileNameWithoutExtension(p.ProjectPath))
                                .Select(p => Path.GetFileNameWithoutExtension(p.ProjectPath)))
                        + "'";

                    ConsoleHelper.ColorWrite(ConsoleColor.Blue, "Excluding referred project '");
                    ConsoleHelper.ColorWrite(ConsoleColor.DarkCyan, project.ProjectPath);
                    ConsoleHelper.ColorWriteLine(ConsoleColor.Blue, $"'. Referred by: {refs}");
                }
            }

            return resultingProjects;
        }

        public static bool IsWildcardMatch(string file, string pattern)
        {
            var folder = Path.GetDirectoryName(file);
            if (folder == string.Empty)
            {
                folder = ".";
            }

            return Directory.GetFiles(folder, pattern + Path.GetExtension(file))
                .Select(Path.GetFileName)
                .Contains(Path.GetFileName(file));
        }
    }
}
