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
            catch (Exception ex) when (ex is IOException || ex is UnauthorizedAccessException || ex is ArgumentException)
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
                p._solutionfile = p2._solutionfile;
                p._path = p2._path;
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

        public static int CopyProjectOutput(List<Project> projects, string buildconfig, string outputpath, string[] includeProjects,
            string[] excludeProjects, string[] webmvcguids, bool verbose, bool gatherall)
        {
            int result = 0;


            List<Project> projects2 = projects.OrderBy(p => p._path).ToList();

            foreach (Project project in projects2)
            {
                project.FixVariables(project._solutionfile, buildconfig);
            }


            // If a project is excluded, it should not prevent referred projects from being included.

            projects2 = ExcludeDuplicatedProjects(projects2, verbose);
            projects2 = ExcludeExplicitProjects(projects2, excludeProjects, verbose);
            projects2 = ExcludeReferredProjects(projects2, gatherall, includeProjects, verbose);
            projects2 = ExcludeWebMvcProjects(projects2, webmvcguids, verbose);


            Console.WriteLine("Copying " + projects2.Count() + " projects.");

            foreach (Project project in projects2.OrderBy(p => p._path))
            {
                /*      
                if (verbose)
                {
                    List<string> causes = new List<string>();
                    if (include)
                    {
                        causes.Add("include");
                    }
                    if (!referred)
                    {
                        causes.Add("!referred");
                    }
                    Console.WriteLine("Copying project (" + string.Join(",", causes) + "): '" + Path.GetFileNameWithoutExtension(project._sln_path) + "'");
                }
                */

                bool projectresult = project.CopyOutput(
                    project._solutionfile,
                    buildconfig,
                    Path.Combine(outputpath, Path.GetFileNameWithoutExtension(project._sln_path)),
                    verbose);
                if (!projectresult)
                {
                    result = 1;
                }
            }

            return result;
        }

        private static List<Project> ExcludeDuplicatedProjects(List<Project> projects, bool verbose)
        {
            List<Project> resultingProjects = new List<Project>();
            foreach (Project project in projects.OrderBy(p => p._path))
            {
                if (verbose)
                {
                    ConsoleHelper.ColorWrite(ConsoleColor.Blue, "Evaluating project: '" + project._path + "'");
                }

                if (resultingProjects.Any(e => e._path == project._path))
                {
                    if (verbose)
                    {
                        Console.WriteLine("Excluding duplicate project: '" + project._solutionfile + "', '" + project._path + "'");
                    }
                    continue;
                }

                resultingProjects.Add(project);
            }

            return resultingProjects;
        }

        private static List<Project> ExcludeWebMvcProjects(List<Project> projects, string[] webmvcguids, bool verbose)
        {
            List<Project> resultingProjects = new List<Project>();
            foreach (Project project in projects)
            {
                if (verbose)
                {
                    ConsoleHelper.ColorWrite(ConsoleColor.Blue, "Evaluating project: '" + project._path + "'");
                }

                if (project._ProjectTypeGuids.Any(g1 => webmvcguids.Any(g2 => string.Compare(g1, g2, true) == 0)))
                {
                    ConsoleHelper.ColorWrite(ConsoleColor.Blue, "Excluding web/mvc project: '", false);
                    ConsoleHelper.ColorWrite(ConsoleColor.DarkCyan, project._path + "'", false);
                    ConsoleHelper.ColorWrite(ConsoleColor.Blue, "'");
                    continue;
                }

                resultingProjects.Add(project);
            }

            return resultingProjects;
        }

        private static List<Project> ExcludeExplicitProjects(List<Project> projects, string[] excludeProjects, bool verbose)
        {
            List<Project> resultingProjects = new List<Project>();
            foreach (Project project in projects)
            {
                if (verbose)
                {
                    ConsoleHelper.ColorWrite(ConsoleColor.Blue, "Evaluating project: '" + project._path + "'");
                }

                if (excludeProjects.Any(x => IsWildcardMatch(project._path, x)))
                {
                    ConsoleHelper.ColorWrite(ConsoleColor.Blue, "Excluding explicit project: '", false);
                    ConsoleHelper.ColorWrite(ConsoleColor.DarkCyan, project._path, false);
                    ConsoleHelper.ColorWrite(ConsoleColor.Blue, "'");
                    continue;
                }

                resultingProjects.Add(project);
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
                    ConsoleHelper.ColorWrite(ConsoleColor.Blue, "Evaluating project: '" + project._path + "'");
                }

                bool include = includeProjects.Contains(Path.GetFileNameWithoutExtension(project._sln_path));
                bool referred = projects.Any(p => p._projectReferences.Any(r => Path.GetFileName(r.include) == Path.GetFileName(project._sln_path)));

                if (gatherall || include || !referred)
                {
                    resultingProjects.Add(project);
                }
                else
                {
                    string refs = "'" +
                        string.Join("', '",
                            projects
                                .Where(p => p._projectReferences.Any(r => Path.GetFileName(r.include) == Path.GetFileName(project._sln_path)))
                                .OrderBy(p => Path.GetFileNameWithoutExtension(p._sln_path))
                                .Select(p => Path.GetFileNameWithoutExtension(p._sln_path)))
                        + "'";

                    ConsoleHelper.ColorWrite(ConsoleColor.Blue, "Excluding referred project '", false);
                    ConsoleHelper.ColorWrite(ConsoleColor.DarkCyan, project._path, false);
                    ConsoleHelper.ColorWrite(ConsoleColor.Blue, "'. Referred by: " + refs);
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
