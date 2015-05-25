﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace CheckMissingFiles
{
    class Solution
    {
        private string _solutionfile;
        private List<Project> _projects;
        private bool _reverseCheck { get; set; }

        public Solution(string solutionfile, bool teamcityErrorMessage)
        {
            _solutionfile = solutionfile;

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
                    p = new Project(_solutionfile, projpath, teamcityErrorMessage);
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

        public int CheckProjects(bool reverseCheck)
        {
            foreach (Project p in _projects.OrderBy(p => p._projectfilepath))
            {
                p.Check(reverseCheck);
            }

            bool parseError = _projects.Any(p => p._parseError);

            int missingfilesError = _projects.Select(p => p._missingfilesError).Sum();
            int missingfilesWarning = _projects.Select(p => p._missingfilesWarning).Sum();
            int excessfiles = _projects.Select(p => p._excessfiles).Sum();

            string msg = "Parsed " + _projects.Count + " projects, found ";

            if (reverseCheck)
            {
                if (excessfiles == 0)
                {
                    ConsoleHelper.WriteLine(msg + "no excess files.");
                }
                else
                {
                    ConsoleHelper.WriteLine(msg + excessfiles + " files in file system that wasn't included in project files.");
                }
            }
            else
            {
                if (missingfilesError == 0)
                {
                    if (missingfilesWarning == 0)
                    {
                        ConsoleHelper.WriteLine(msg + "no missing files.");
                    }
                    else
                    {
                        ConsoleHelper.WriteLine(msg + "no missing files (although " + missingfilesWarning + " missing files with None build action).");
                    }
                }
                else
                {
                    if (missingfilesWarning == 0)
                    {
                        ConsoleHelper.WriteLine(msg + missingfilesError + " missing files.");
                    }
                    else
                    {
                        ConsoleHelper.WriteLine(msg + missingfilesError + " missing files (and " + missingfilesWarning + " missing files with None build action).");
                    }
                }
            }

            if (parseError)
            {
                return 2;
            }

            if (missingfilesError == 0)
            {
                return 0;
            }
            else
            {
                return 3;
            }
        }
    }
}
