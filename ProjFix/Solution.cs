using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace ProjFix
{
    class Solution(string solutionfile)
    {
        private readonly string Solutionfile = solutionfile;

        public void RestoreProjects()
        {
            List<Project> projects = LoadProjects();
            if (projects == null)
            {
                return;
            }

            foreach (var p in projects)
            {
                p.Restore(Solutionfile);
            }
        }

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

                        var package = row.Substring(9, projtypeline.Length - 13);
                        var shortfilename = values[0].Trim().Trim('"');
                        var path = values[1].Trim().Trim('"');
                        var guid = values[2].Trim().Trim('"');

                        projects.Add(new Project()
                        {
                            Sln_package = package,
                            Sln_shortfilename = shortfilename,
                            Sln_path = path,
                            Sln_guid = guid,
                            Modified = false
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

                ConsoleHelper.WriteLine(
                    $"sln_package: '{p.Sln_package}" +
                    $"', sln_guid: '{p.Sln_guid}" +
                    $"', sln_shortfilename: '{p.Sln_shortfilename}" +
                    $"', sln_path: '{p.Sln_path}" +
                    $"', proj_assemblynames: {p2.Proj_assemblynames.Count}" +
                    $", proj_guids: {p2.Proj_guids.Count}" +
                    $", proj_outputtypes: {p2.Proj_outputtypes.Count}.",
                    true);

                p.Proj_assemblynames = p2.Proj_assemblynames;
                p.Proj_guids = p2.Proj_guids;
                p.Proj_outputtypes = p2.Proj_outputtypes;

                p.Outputpaths = p2.Outputpaths;
                p.References = p2.References;
                p.ProjectReferences = p2.ProjectReferences;
            }

            if (error)
            {
                ConsoleHelper.ColorWrite(ConsoleColor.Red, "Fix errors before continuing!");
                return null;
            }

            return projects;
        }

        public bool FixProjects(List<Project> projects, List<string> hintpaths, bool removeversion)
        {
            var valid = true;

            foreach (var p in projects.OrderBy(p => p.Sln_path))
            {
                p.Compact();
            }

            foreach (var p in projects.OrderBy(p => p.Sln_path))
            {
                p.CompactRefs();
            }

            ConsoleHelper.WriteLineDeferred("-=-=- Validating -=-=-");
            foreach (var p in projects.OrderBy(p => p.Sln_path))
            {
                if (!p.Validate(Solutionfile, projects))
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

            foreach (var p in projects.OrderBy(p => p.Sln_path))
            {
                _ = p.Fix(Solutionfile, projects, hintpaths, removeversion);
            }

            return true;  // Success
        }

        public void WriteProjects(List<Project> projects, bool simulate, bool nobackup)
        {
            int count1, count2;

            count1 = count2 = 0;
            foreach (var p in projects.OrderBy(pp => pp.Sln_path))
            {
                count1++;
                if (p.Modified)
                {
                    p.WriteProject(Solutionfile, simulate, nobackup);
                    count2++;
                }
            }

            ConsoleHelper.WriteLine($"Fixed {count2} of {count1} projects.", false);
        }
    }
}
