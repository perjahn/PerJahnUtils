using System;
using System.Collections.Generic;
using System.IO;

namespace CreatePublish
{
    class Solution(string solutionfile)
    {
        private readonly string Solutionfile = solutionfile;

        public List<Project> LoadProjects()
        {
            string[] rows;
            try
            {
                rows = File.ReadAllLines(Solutionfile);
            }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or ArgumentException)
            {
                ConsoleHelper.ColorWrite(ConsoleColor.Red, $"Couldn't load solution: '{Solutionfile}': {ex.Message}");
                return null;
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
                        var values = row[projtypeline.Length..].Split(',');
                        if (values.Length != 3)
                        {
                            continue;
                        }

                        var shortfilename = values[0].Trim().Trim('"');
                        var path = values[1].Trim().Trim('"');

                        projects.Add(new Project()
                        {
                            Sln_shortfilename = shortfilename,
                            Sln_path = path
                        });
                    }
                }
            }

            var error = false;

            foreach (var p in projects)
            {
                var p2 = Project.LoadProject(Solutionfile, p.Sln_path);
                if (p2 == null)
                {
                    error = true;
                    continue;
                }

                ConsoleHelper.WriteLine($"sln_shortfilename: '{p.Sln_shortfilename}', sln_path: '{p.Sln_path}'.", true);

                p.ProjectTypeGuids = p2.ProjectTypeGuids;
            }

            if (error)
            {
                ConsoleHelper.ColorWrite(ConsoleColor.Red, "Fix errors before continuing!");
                return null;
            }

            return projects;
        }
    }
}
