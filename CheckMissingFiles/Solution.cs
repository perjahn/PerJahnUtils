using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace CheckMissingFiles
{
    class Solution
    {
        private string _solutionfile;
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
                throw new ApplicationException("Couldn't load solution: '" + _solutionfile + "': " + ex.Message);
            }
            catch (UnauthorizedAccessException ex)
            {
                throw new ApplicationException("Couldn't load solution: '" + _solutionfile + "': " + ex.Message);
            }
            catch (ArgumentException ex)
            {
                throw new ApplicationException("Couldn't load solution: '" + _solutionfile + "': " + ex.Message);
            }

            List<string> projpaths = new List<string>();

            foreach (string row in rows)
            {
                // cs, vb, cpp
                string[] projtypeguids = { "{FAE04EC0-301F-11D3-BF4B-00C04F79EFBC}", "{F184B08F-C81C-45F6-A57F-5ABD9991F28F}", "{8BC9CEB8-8B4A-11D0-8D11-00A0C91BC942}" };

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
                        string path = values[1].Trim().Trim('"');

                        projpaths.Add(path);
                    }
                }
            }


            bool error = false;

            _projects = new List<Project>();

            foreach (string projpath in projpaths)
            {
                Project p;
                try
                {
                    p = Project.LoadProject(_solutionfile, projpath);
                }
                catch (ApplicationException ex)
                {
                    ConsoleHelper.WriteLineColor(ex.Message, ConsoleColor.Red);
                    error = true;
                    continue;
                }

                _projects.Add(p);
            }

            if (error)
            {
                throw new ApplicationException("Fix errors before continuing!");
            }
        }

        public int CheckProjects()
        {
            foreach (Project p in _projects.OrderBy(p => p._projectfilepath))
            {
                p.Check(_solutionfile);
            }

            int missingfiles = _projects.Select(p => p._missingfiles).Sum();

            if (missingfiles == 0)
            {
                ConsoleHelper.WriteLine("Parsed " + _projects.Count + " projects found no missing files.");
                return 0;
            }

            ConsoleHelper.WriteLine("Parsed " + _projects.Count + " projects, found " + missingfiles + " missing files.");
            return 3;
        }
    }
}
