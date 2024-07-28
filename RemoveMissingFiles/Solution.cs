using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace RemoveMissingFiles
{
    class Solution(string solutionfile)
    {
        private readonly string _solutionfile = solutionfile;
        private List<Project> _projects;

        public void LoadProjects()
        {
            string[] rows;
            try
            {
                rows = File.ReadAllLines(_solutionfile);
            }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or ArgumentException)
            {
                throw new ApplicationException($"Couldn't load solution: '{_solutionfile}': {ex.Message}");
            }

            List<Project> projects = [];

            foreach (var row in rows)
            {
                // Project("{FAE04EC0-301F-11D3-BF4B-00C04F79EFBC}") = "MyCsProject", "Folder\Folder\MyCsProject.csproj", "{01010101-0101-0101-0101-010101010101}"
                // Project("{F184B08F-C81C-45F6-A57F-5ABD9991F28F}") = "MyVbProject", "Folder\Folder\MyVbProject.vbproj", "{02020202-0202-0202-0202-020202020202}"

                string[] projtypeguids = ["{FAE04EC0-301F-11D3-BF4B-00C04F79EFBC}", "{F184B08F-C81C-45F6-A57F-5ABD9991F28F}"];

                foreach (var projtypeguid in projtypeguids)
                {
                    var projtypeline = $"Project(\"{projtypeguid}\") =";

                    if (row.StartsWith(projtypeline))
                    {
                        string[] values = row[projtypeline.Length..].Split(',');
                        if (values.Length != 3)
                        {
                            continue;
                        }

                        var package = row.Substring(9, projtypeline.Length - 13);
                        var path = values[1].Trim().Trim('"');

                        projects.Add(new Project()
                        {
                            _sln_package = package,
                            _sln_path = path,
                            _removedfiles = 0
                        });
                    }
                }
            }

            var error = false;

            foreach (var p in projects)
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
            foreach (var p in _projects.OrderBy(p => p._sln_path))
            {
                p.Fix(_solutionfile);
            }

            return _projects.Select(p => p._removedfiles).Sum();
        }

        public void WriteProjects(bool simulate)
        {
            int count1, count2, count3;

            count1 = count2 = count3 = 0;
            foreach (var p in _projects.OrderBy(pp => pp._sln_path))
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
        }
    }
}
