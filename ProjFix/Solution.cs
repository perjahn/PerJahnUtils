using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace ProjFix
{
    class Solution
    {
        private readonly string _solutionfile;

        public Solution(string solutionfile)
        {
            _solutionfile = solutionfile;
        }

        public void RestoreProjects()
        {
            List<Project> projects = LoadProjects();
            if (projects == null)
            {
                return;
            }

            foreach (Project p in projects)
            {
                p.Restore(_solutionfile);
            }

            return;
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
                ConsoleHelper.ColorWrite(ConsoleColor.Red, $"Couldn't load solution: '{_solutionfile}': {ex.Message}");
                return null;
            }
            catch (UnauthorizedAccessException ex)
            {
                ConsoleHelper.ColorWrite(ConsoleColor.Red, $"Couldn't load solution: '{_solutionfile}': {ex.Message}");
                return null;
            }
            catch (ArgumentException ex)
            {
                ConsoleHelper.ColorWrite(ConsoleColor.Red, $"Couldn't load solution: '{_solutionfile}': {ex.Message}");
                return null;
            }

            List<Project> projects = new List<Project>();

            foreach (string row in rows)
            {
                // Project("{FAE04EC0-301F-11D3-BF4B-00C04F79EFBC}") = "MyCsProject", "Folder\Folder\MyCsProject.csproj", "{01010101-0101-0101-0101-010101010101}"
                // Project("{F184B08F-C81C-45F6-A57F-5ABD9991F28F}") = "MyVbProject", "Folder\Folder\MyVbProject.vbproj", "{02020202-0202-0202-0202-020202020202}"

                string[] projtypeguids = { "{FAE04EC0-301F-11D3-BF4B-00C04F79EFBC}", "{F184B08F-C81C-45F6-A57F-5ABD9991F28F}" };

                foreach (string projtypeguid in projtypeguids)
                {
                    string projtypeline = $"Project(\"{projtypeguid}\") =";

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
                            _sln_package = package,
                            _sln_shortfilename = shortfilename,
                            _sln_path = path,
                            _sln_guid = guid,
                            _modified = false
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
                        $"sln_package: '{p._sln_package}" +
                        $"', sln_guid: '{p._sln_guid}" +
                        $"', sln_shortfilename: '{p._sln_shortfilename}" +
                        $"', sln_path: '{p._sln_path}" +
                        $"', proj_assemblynames: {p2._proj_assemblynames.Count}" +
                        $", proj_guids: {p2._proj_guids.Count}" +
                        $", proj_outputtypes: {p2._proj_outputtypes.Count}.",
                        true);

                p._proj_assemblynames = p2._proj_assemblynames;
                p._proj_guids = p2._proj_guids;
                p._proj_outputtypes = p2._proj_outputtypes;

                p._outputpaths = p2._outputpaths;
                p._references = p2._references;
                p._projectReferences = p2._projectReferences;
            }

            if (error)
            {
                ConsoleHelper.ColorWrite(ConsoleColor.Red, "Fix errors before continuing!");
                return null;
            }

            return projects;
        }

        public bool FixProjects(List<Project> projects, List<string> hintpaths, string outputpath, bool copylocal, bool removeversion)
        {
            bool valid = true;

            foreach (Project p in projects.OrderBy(p => p._sln_path))
            {
                p.Compact();
            }

            foreach (Project p in projects.OrderBy(p => p._sln_path))
            {
                p.CompactRefs();
            }

            ConsoleHelper.WriteLineDeferred("-=-=- Validating -=-=-");
            foreach (Project p in projects.OrderBy(p => p._sln_path))
            {
                if (!p.Validate(_solutionfile, projects))
                {
                    valid = false;
                }
            }
            ConsoleHelper.WriteLineDeferred(null);

            if (!valid)
            {
                ConsoleHelper.ColorWrite(ConsoleColor.Red, "Fix errors before continuing!");
                return false;
            }

            foreach (Project p in projects.OrderBy(p => p._sln_path))
            {
                p.Fix(_solutionfile, projects, hintpaths, outputpath, copylocal, removeversion);
            }

            return true;  // Success
        }

        public void WriteProjects(List<Project> projects, List<string> hintpaths, string outputpath, bool simulate, bool nobackup)
        {
            int count1, count2;

            count1 = count2 = 0;
            foreach (Project p in projects.OrderBy(pp => pp._sln_path))
            {
                count1++;
                if (p._modified)
                {
                    p.WriteProject(_solutionfile, simulate, nobackup);
                    count2++;
                }
            }

            ConsoleHelper.WriteLine($"Fixed {count2} of {count1} projects.", false);

            return;
        }
    }
}
