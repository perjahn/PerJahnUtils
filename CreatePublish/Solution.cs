using System;
using System.Collections.Generic;
using System.IO;

namespace CreatePublish
{
    class Solution(string solutionfile)
    {
        private readonly string _solutionfile = solutionfile;

        public List<Project> LoadProjects()
        {
            string[] rows;
            try
            {
                rows = File.ReadAllLines(_solutionfile);
            }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or ArgumentException)
            {
                ConsoleHelper.ColorWrite(ConsoleColor.Red, $"Couldn't load solution: '{_solutionfile}': {ex.Message}");
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
                            _sln_shortfilename = shortfilename,
                            _sln_path = path
                        });
                    }
                }
            }

            var error = false;

            foreach (var p in projects)
            {
                var p2 = Project.LoadProject(_solutionfile, p._sln_path);
                if (p2 == null)
                {
                    error = true;
                    continue;
                }

                ConsoleHelper.WriteLine($"sln_shortfilename: '{p._sln_shortfilename}', sln_path: '{p._sln_path}'.", true);

                p._ProjectTypeGuids = p2._ProjectTypeGuids;
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
