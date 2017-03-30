using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace CheckMissingFiles
{
    class Solution
    {
        public string solutionFile { get; set; }
        public List<string> projectsPaths { get; set; }

        public Solution(string solutionfile, bool teamcityErrorMessage)
        {
            solutionFile = solutionfile;

            string[] rows;
            try
            {
                rows = File.ReadAllLines(solutionFile);
            }
            catch (Exception ex) when (ex is IOException || ex is UnauthorizedAccessException || ex is ArgumentException)
            {
                string message =
                    teamcityErrorMessage ?
                        string.Format(
                            "##teamcity[message text='Could not load solution: {0} --> {1}' status='ERROR']",
                            solutionFile,
                            ex.Message.Replace("\'", "")) :
                        string.Format(
                            "Couldn't load solution: '{0}' --> '{1}'",
                            $"'{solutionFile}'",
                            ex.Message);

                throw new ApplicationException(message);
            }

            projectsPaths = new List<string>();

            foreach (string row in rows)
            {
                // cs, vb, cpp, cds, fs, wix, cs core
                string[] projtypeguids = {
                    "{FAE04EC0-301F-11D3-BF4B-00C04F79EFBC}",
                    "{F184B08F-C81C-45F6-A57F-5ABD9991F28F}",
                    "{8BC9CEB8-8B4A-11D0-8D11-00A0C91BC942}",
                    "{20D4826A-C6FA-45DB-90F4-C717570B9F32}",
                    "{F2A71F9B-5D33-465A-A702-920D77279786}",
                    "{930C7802-8A8C-48F9-8165-68863BCCD9DD}",
                    "{9A19103F-16F7-4668-BE54-9A1E7A4F7556}"
                };
                // solution folder, website
                string[] ignoreguids = {
                    "{2150E333-8FDC-42A3-9474-1A3956D46DE8}",
                    "{E24C65DC-7377-472B-9ABA-BC803B73C61A}"
                };

                if (row.StartsWith("Project(\""))
                {
                    if (ignoreguids.Any(g => row.StartsWith($"Project(\"{g}\") =")))
                    {
                    }
                    else if (projtypeguids.Any(g => row.StartsWith($"Project(\"{g}\") =")))
                    {
                        string[] values = row.Substring(row.IndexOf('=') + 1).Split(',');
                        if (values.Length != 3)
                        {
                            ConsoleHelper.WriteLineColor($"{solutionFile}: Corrupt solution file: '{row}'", ConsoleColor.Yellow);
                            continue;
                        }

                        string path = values[1].Trim().Trim('"');

                        projectsPaths.Add(path);
                    }
                    else
                    {
                        ConsoleHelper.WriteLineColor($"{solutionFile}: Ignoring unknown project type: '{row}'", ConsoleColor.Yellow);
                        continue;
                    }
                }
            }
        }
    }
}
