using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace CheckNamespace
{
    class Solution
    {
        private readonly string _solutionfile;
        public List<Project> _projects;

        public Solution(string solutionfile)
        {
            _solutionfile = solutionfile;

            string[] rows;
            try
            {
                rows = File.ReadAllLines(_solutionfile);
            }
            catch (IOException ex)
            {
                throw new ApplicationException($"Couldn't load solution: '{Path.GetFileName(_solutionfile)}': {ex.Message}", ex);
            }
            catch (UnauthorizedAccessException ex)
            {
                throw new ApplicationException($"Couldn't load solution: '{Path.GetFileName(_solutionfile)}': {ex.Message}", ex);
            }
            catch (ArgumentException ex)
            {
                throw new ApplicationException($"Couldn't load solution: '{Path.GetFileName(_solutionfile)}': {ex.Message}", ex);
            }


            // Project("{FAE04EC0-301F-11D3-BF4B-00C04F79EFBC}") = "MyCsProject", "Folder\Folder\MyCsProject.csproj", "{01010101-0101-0101-0101-010101010101}"
            // Project("{F184B08F-C81C-45F6-A57F-5ABD9991F28F}") = "MyVbProject", "Folder\Folder\MyVbProject.vbproj", "{02020202-0202-0202-0202-020202020202}"

            string[] projtypeguids = { "{FAE04EC0-301F-11D3-BF4B-00C04F79EFBC}", "{F184B08F-C81C-45F6-A57F-5ABD9991F28F}" };

            IEnumerable<string> projpaths = rows
                .Where(r => projtypeguids.Any(g => IsPackageRow(r, g)))
                .Select(r => r.Split(',')[1].Trim().Trim('"'))
                .ToList();


            _projects = LoadProjects(projpaths).ToList();
        }

        private static bool IsPackageRow(string row, string guid)
        {
            string packagetext = $"Project(\"{guid}\") =";

            return row.StartsWith(packagetext) && row.Substring(packagetext.Length).Split(',').Length == 3;
        }

        private IEnumerable<Project> LoadProjects(IEnumerable<string> projpaths)
        {
            foreach (string path in projpaths)
            {
                Project p;

                try
                {
                    p = new Project(_solutionfile, path);
                }
                catch (ApplicationException ex)
                {
                    ConsoleHelper.WriteLineColor(ex.Message, ConsoleColor.Red);
                    continue;
                }

                yield return p;
            }
        }
    }
}
