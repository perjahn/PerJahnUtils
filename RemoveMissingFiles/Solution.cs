using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace RemoveMissingFiles
{
    class Solution(string solutionfile)
    {
        private readonly string _solutionfile = solutionfile;
        private List<Project> Projects;

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
                            Sln_package = package,
                            Sln_path = path,
                            Removedfiles = 0
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
                    p2 = Project.LoadProject(_solutionfile, p.Sln_path);
                }
                catch (ApplicationException ex)
                {
                    ConsoleHelper.WriteLineColor(ex.Message, ConsoleColor.Red);
                    error = true;
                    continue;
                }

                p.Allfiles = p2.Allfiles;
            }

            if (error)
            {
                throw new ApplicationException("Fix errors before continuing!");
            }

            Projects = projects;
        }

        public int FixProjects()
        {
            foreach (var p in Projects.OrderBy(p => p.Sln_path))
            {
                p.Fix(_solutionfile);
            }

            return Projects.Sum(p => p.Removedfiles);
        }

        public void WriteProjects(bool simulate)
        {
            int count1, count2, count3;

            count1 = count2 = count3 = 0;
            foreach (var p in Projects.OrderBy(pp => pp.Sln_path))
            {
                count1++;
                if (p.Removedfiles > 0)
                {
                    p.WriteProject(_solutionfile, simulate);
                    count2++;
                    count3 += p.Removedfiles;
                }
            }

            if (count2 != 0)
            {
                ConsoleHelper.WriteLine($"Fixed {count2} of {count1} projects, removed {count3} file references.");
            }
        }
    }
}
