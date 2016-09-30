using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace GatherOutputAssemblies
{
    class Solution
    {
        public string _path { get; set; }
        public string[] projectfiles { get; set; } = { };

        public Solution(string solutionfile)
        {
            _path = solutionfile;

            List<string> projects = new List<string>();

            string[] rows;
            try
            {
                rows = File.ReadAllLines(solutionfile);
            }
            catch (Exception ex) when (ex is IOException || ex is UnauthorizedAccessException || ex is ArgumentException)
            {
                ConsoleHelper.ColorWriteLine(ConsoleColor.Red, "Couldn't load solution: '" + solutionfile + "': " + ex.Message);
                return;
            }

            foreach (string row in rows)
            {
                // Project("{FAE04EC0-301F-11D3-BF4B-00C04F79EFBC}") = "MyCsProject", "Folder\Folder\MyCsProject.csproj", "{01010101-0101-0101-0101-010101010101}"
                // Project("{F184B08F-C81C-45F6-A57F-5ABD9991F28F}") = "MyVbProject", "Folder\Folder\MyVbProject.vbproj", "{02020202-0202-0202-0202-020202020202}"
                // Project("{8BC9CEB8-8B4A-11D0-8D11-00A0C91BC942}") = "MyVcProject", "Folder\Folder\MyVcProject.vcxproj", "{03030303-0303-0303-0303-030303030303}"

                string[] projtypeguids = {
                    "{FAE04EC0-301F-11D3-BF4B-00C04F79EFBC}",
                    "{F184B08F-C81C-45F6-A57F-5ABD9991F28F}",
                    "{8BC9CEB8-8B4A-11D0-8D11-00A0C91BC942}" };

                foreach (string projtypeguid in projtypeguids)
                {
                    string projtypeline = "Project(\"" + projtypeguid + "\") =";

                    if (row.StartsWith(projtypeline))
                    {
                        string[] values = row.Substring(projtypeline.Length).Split(',');
                        if (values.Length != 3)
                        {
                            continue;
                        }

                        string package = row.Substring(9, projtypeline.Length - 13);
                        string shortfilename = values[0].Trim().Trim('"');
                        string path = values[1].Trim().Trim('"');
                        string guid = values[2].Trim().Trim('"');

                        projects.Add(FileHelper.CompactPath(Path.Combine(Path.GetDirectoryName(solutionfile), path)));
                    }
                }
            }

            projectfiles = projects.ToArray();
        }

        public static int CopyProjectOutput(Project[] projects, string buildconfig, string outputpath, string[] includeProjects,
            string[] excludeProjects, bool deletetargetfolder, bool gatherall, bool simulate, bool verbose)
        {
            int result = 0;

            // If a project is excluded, it should not prevent referred projects from being included.

            List<Project> projects2 = projects.ToList();

            projects2 = ExcludeCorruptProjects(projects2, verbose);
            projects2 = ExcludeExplicitProjects(projects2, excludeProjects, verbose);
            projects2 = ExcludeReferredProjects(projects2, gatherall, includeProjects, verbose);
            projects2 = ExcludeWebMvcProjects(projects2, verbose);


            Console.WriteLine("Retrieving output folders for " + projects2.Count() + " projects.");

            var operations = projects2
                .Select(p =>
                    new
                    {
                        sourcepath = p.GetOutputFolder(buildconfig, verbose),
                        targetpath = Path.Combine(outputpath, FileHelper.GetCleanFolderName(Path.GetFileNameWithoutExtension(p._path)))
                    })
                .Where(p => p.sourcepath != null)
                .ToArray();



            if (deletetargetfolder && Directory.Exists(outputpath))
            {
                Console.WriteLine("Deleting folder: '" + outputpath + "'");
                Directory.Delete(outputpath, true);
            }


            Console.WriteLine("Copying " + operations.Length + " projects.");

            int copiedFiles = 0;

            foreach (var operation in operations)
            {
                string sourcepath = operation.sourcepath;
                string targetpath = operation.targetpath;

                ConsoleHelper.ColorWriteLine(ConsoleColor.Cyan, "Copying folder: '" + sourcepath + "' -> '" + targetpath + "'");

                if (!FileHelper.CopyFolder(new DirectoryInfo(sourcepath), new DirectoryInfo(targetpath), simulate, verbose, ref copiedFiles))
                {
                    result = 1;
                }
            }

            Console.WriteLine("Copied " + copiedFiles + " files.");

            return result;
        }

        private static List<Project> ExcludeCorruptProjects(List<Project> projects, bool verbose)
        {
            List<Project> resultingProjects = new List<Project>();
            foreach (Project project in projects)
            {
                if (verbose)
                {
                    ConsoleHelper.ColorWriteLine(ConsoleColor.Blue, "Evaluating project (unusable): '" + project._path + "'");
                }

                if (project._proj_guid == null || project._ProjectTypeGuids == null)
                {
                    ConsoleHelper.ColorWrite(ConsoleColor.Blue, "Excluding unusable project: '");
                    ConsoleHelper.ColorWrite(ConsoleColor.DarkCyan, project._path);
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
            {
                "{603C0E0B-DB56-11DC-BE95-000D561079B0}",
                "{F85E285D-A4E0-4152-9332-AB1D724D3325}",
                "{E53F8FEA-EAE0-44A6-8774-FFD645390401}",
                "{E3E379DF-F4C6-4180-9B81-6769533ABE47}",
                "{349C5851-65DF-11DA-9384-00065B846F21}"
            };

            List<Project> resultingProjects = new List<Project>();
            foreach (Project project in projects)
            {
                if (verbose)
                {
                    ConsoleHelper.ColorWriteLine(ConsoleColor.Blue, "Evaluating project (web/mvc): '" + project._path + "'");
                }

                if (project._ProjectTypeGuids.Any(g1 => webmvcguids.Any(g2 => string.Compare(g1, g2, true) == 0)))
                {
                    ConsoleHelper.ColorWrite(ConsoleColor.Blue, "Excluding web/mvc project: '");
                    ConsoleHelper.ColorWrite(ConsoleColor.DarkCyan, project._path + "'");
                    ConsoleHelper.ColorWriteLine(ConsoleColor.Blue, "'");
                    continue;
                }

                resultingProjects.Add(project);
            }

            return resultingProjects;
        }

        private static List<Project> ExcludeExplicitProjects(List<Project> projects, string[] excludeProjects, bool verbose)
        {
            Dictionary<string, bool> used = new Dictionary<string, bool>();
            foreach (string project in excludeProjects)
            {
                used[project] = false;
            }

            List<Project> resultingProjects = new List<Project>();
            foreach (Project project in projects)
            {
                if (verbose)
                {
                    ConsoleHelper.ColorWriteLine(ConsoleColor.Blue, "Evaluating project (explicit): '" + project._path + "'");
                }

                string[] matches = excludeProjects.Where(x => IsWildcardMatch(project._path, x)).ToArray();

                if (matches.Length > 0)
                {
                    ConsoleHelper.ColorWrite(ConsoleColor.Blue, "Excluding explicit project: '");
                    ConsoleHelper.ColorWrite(ConsoleColor.DarkCyan, project._path);
                    ConsoleHelper.ColorWriteLine(ConsoleColor.Blue, "'");

                    foreach (string excludematch in matches)
                    {
                        used[excludematch] = true;
                    }

                    continue;
                }

                resultingProjects.Add(project);
            }

            foreach (string project in excludeProjects)
            {
                if (!used[project])
                {
                    ConsoleHelper.ColorWriteLine(ConsoleColor.Yellow, "Excessive exclude filter unused: '" + project + "'");
                }
            }

            return resultingProjects;
        }

        private static List<Project> ExcludeReferredProjects(List<Project> projects, bool gatherall, string[] includeProjects, bool verbose)
        {
            List<Project> resultingProjects = new List<Project>();
            foreach (Project project in projects)
            {
                if (verbose)
                {
                    ConsoleHelper.ColorWriteLine(ConsoleColor.Blue, "Evaluating project (referred): '" + project._path + "'");
                }

                bool include = includeProjects.Contains(Path.GetFileNameWithoutExtension(project._path));
                bool referred = projects.Any(p => p._projectReferences.Any(r => Path.GetFileName(r.include) == Path.GetFileName(project._path)));

                if (gatherall || include || !referred)
                {
                    resultingProjects.Add(project);
                }
                else
                {
                    string refs = "'" +
                        string.Join("', '",
                            projects
                                .Where(p => p._projectReferences.Any(r => Path.GetFileName(r.include) == Path.GetFileName(project._path)))
                                .OrderBy(p => Path.GetFileNameWithoutExtension(p._path))
                                .Select(p => Path.GetFileNameWithoutExtension(p._path)))
                        + "'";

                    ConsoleHelper.ColorWrite(ConsoleColor.Blue, "Excluding referred project '");
                    ConsoleHelper.ColorWrite(ConsoleColor.DarkCyan, project._path);
                    ConsoleHelper.ColorWriteLine(ConsoleColor.Blue, "'. Referred by: " + refs);
                }
            }

            return resultingProjects;
        }

        public static bool IsWildcardMatch(string file, string pattern)
        {
            string folder = Path.GetDirectoryName(file);
            if (folder == string.Empty)
            {
                folder = ".";
            }

            return Directory.GetFiles(folder, pattern + Path.GetExtension(file))
                .Select(f => Path.GetFileName(f))
                .Contains(Path.GetFileName(file));
        }
    }
}
