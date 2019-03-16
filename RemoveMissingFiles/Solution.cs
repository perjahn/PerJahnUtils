using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace RemoveMissingFiles
{
    class Solution
    {
        private readonly string _solutionfile;
        private List<Project> _projects;

        public Solution(string solutionfile)
        {
            _solutionfile = solutionfile;
        }

        public void LoadProjects()
        {
            string[] rows;
            try
            {
                rows = File.ReadAllLines(_solutionfile);
            }
            catch (IOException ex)
            {
                throw new ApplicationException($"Couldn't load solution: '{_solutionfile}': {ex.Message}");
            }
            catch (UnauthorizedAccessException ex)
            {
                throw new ApplicationException($"Couldn't load solution: '{_solutionfile}': {ex.Message}");
            }
            catch (ArgumentException ex)
            {
                throw new ApplicationException($"Couldn't load solution: '{_solutionfile}': {ex.Message}");
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
                        string path = values[1].Trim().Trim('"');

                        projects.Add(new Project()
                        {
                            _sln_package = package,
                            _sln_path = path,
                            _removedfiles = 0
                        });
                    }
                }
            }


            bool error = false;

            foreach (Project p in projects)
            {
                Project p2;
                try
                {
                    p2 = Project.LoadProject(_solutionfile, p._sln_path);
                }
                catch (ApplicationException ex)
                {
                    ConsoleHelper.WriteLineColor(ex.Message, ConsoleColor.Red);
                    error = true;
                    continue;
                }

                p._allfiles = p2._allfiles;
            }

            if (error)
            {
                throw new ApplicationException("Fix errors before continuing!");
            }

            _projects = projects;
        }

        public int FixProjects()
        {
            foreach (Project p in _projects.OrderBy(p => p._sln_path))
            {
                p.Fix(_solutionfile, _projects);
            }

            return _projects.Select(p => p._removedfiles).Sum();
        }

        public void WriteProjects(bool simulate)
        {
            int count1, count2, count3;

            count1 = count2 = count3 = 0;
            foreach (Project p in _projects.OrderBy(pp => pp._sln_path))
            {
                count1++;
                if (p._removedfiles > 0)
                {
                    p.WriteProject(_solutionfile, simulate);
                    count2++;
                    count3 += p._removedfiles;
                }
            }

            if (count2 != 0)
            {
                ConsoleHelper.WriteLine($"Fixed {count2} of {count1} projects, removed {count3} file references.");
            }

            return;
        }
    }
}
