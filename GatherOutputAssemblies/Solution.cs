using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace GatherOutputAssemblies
{
    class Solution
    {
        private string _solutionfile;

        public Solution(string solutionfile)
        {
            _solutionfile = solutionfile;
        }

        public List<Project> LoadProjects()
        {
            string[] rows;
            try
            {
                rows = File.ReadAllLines(_solutionfile);
            }
            catch (IOException ex)
            {
                ConsoleHelper.ColorWrite(ConsoleColor.Red, "Couldn't load solution: '" + _solutionfile + "': " + ex.Message);
                return null;
            }
            catch (UnauthorizedAccessException ex)
            {
                ConsoleHelper.ColorWrite(ConsoleColor.Red, "Couldn't load solution: '" + _solutionfile + "': " + ex.Message);
                return null;
            }
            catch (ArgumentException ex)
            {
                ConsoleHelper.ColorWrite(ConsoleColor.Red, "Couldn't load solution: '" + _solutionfile + "': " + ex.Message);
                return null;
            }

            List<Project> projects = new List<Project>();

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

                        projects.Add(new Project()
                        {
                            _sln_path = path
                        });
                    }
                }
            }


            bool error = false;

            foreach (Project p in projects)
            {
                Project p2 = Project.LoadProject(_solutionfile, p._sln_path);
                if (p2 == null)
                {
                    error = true;
                    continue;
                }

                ConsoleHelper.WriteLine(
                    "sln_path: '" + p._sln_path + "'.",
                    true);

                p._proj_guids = p2._proj_guids;
                p._ProjectTypeGuids = p2._ProjectTypeGuids;

                p._outputpaths = p2._outputpaths;
                p._outdirs = p2._outdirs;
                p._projectReferences = p2._projectReferences;
            }

            if (error)
            {
                ConsoleHelper.ColorWrite(ConsoleColor.Red, "Fix errors before continuing!");
                return null;
            }

            foreach (Project project in projects.OrderBy(p => p._sln_path))
            {
                project.Compact();
            }

            return projects;
        }

        public int CopyProjectOutput(List<Project> projects, string buildconfig, string outputpath, List<string> includeProjects, List<string> excludeProjects, string[] webmvcguids, bool verbose)
        {
            int result = 0;

            foreach (Project project in projects.OrderBy(p => p._sln_path))
            {
                project.FixVariables(_solutionfile, buildconfig);

                if (verbose)
                {
                    ConsoleHelper.ColorWrite(ConsoleColor.Blue, "Parsing project: '" + project._sln_path + "'");
                }

                if (project._ProjectTypeGuids.Any(g1 => webmvcguids.Any(g2 => string.Compare(g1, g2, true) == 0)))
                {
                    ConsoleHelper.ColorWrite(ConsoleColor.Blue, "Excluding web/mvc project: '" + project._sln_path + "'");
                }
                else
                {
                    if (includeProjects.Contains(Path.GetFileNameWithoutExtension(project._sln_path)) ||
                        !projects.Any(p => p._projectReferences.Any(r => Path.GetFileName(r.include) == Path.GetFileName(project._sln_path)))
                        && !excludeProjects.Contains(Path.GetFileNameWithoutExtension(project._sln_path)))
                    {
                        bool projectresult = project.CopyOutput(
                            _solutionfile,
                            buildconfig,
                            Path.Combine(outputpath, Path.GetFileNameWithoutExtension(project._sln_path)),
                            verbose);
                        if (!projectresult)
                        {
                            result = 1;
                        }
                    }
                }
            }


            return result;
        }

        public bool IsWildcardMatch(string file, string pattern)
        {
            string folder = Path.GetDirectoryName(file);
            if (folder == string.Empty)
            {
                folder = ".";
            }

            return Directory.GetFiles(folder, pattern)
                .Select(f => Path.GetFileName(f))
                .Contains(Path.GetFileName(file));
        }
    }
}
