using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace CheckMissingFiles
{
    class Solution
    {
        public string SolutionFile { get; set; }
        public List<string> ProjectsPaths { get; set; }

        public Solution(string solutionfile, bool teamcityErrorMessage)
        {
            SolutionFile = solutionfile;

            string[] rows;
            try
            {
                rows = File.ReadAllLines(SolutionFile);
            }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or ArgumentException)
            {
                var message =
                    teamcityErrorMessage ?
                        $"##teamcity[message text='Could not load solution: {SolutionFile} --> {ex.Message.Replace("\'", "")}' status='ERROR']" :
                        $"Couldn't load solution: '{SolutionFile}' --> '{ex.Message}'";

                throw new ApplicationException(message);
            }

            ProjectsPaths = [];

            foreach (var row in rows)
            {
                string[] projtypeguids = [
                    "{151D2E53-A2C4-4D7D-83FE-D05416EBD58E}",  // deploy
                    "{20D4826A-C6FA-45DB-90F4-C717570B9F32}",  // cds
                    "{8BB2217D-0F2D-49D1-97BC-3654ED321F3B}",  // aspnet5
                    "{8BC9CEB8-8B4A-11D0-8D11-00A0C91BC942}",  // cpp
                    "{930C7802-8A8C-48F9-8165-68863BCCD9DD}",  // wix
                    "{9A19103F-16F7-4668-BE54-9A1E7A4F7556}",  // old c# core (xproj)
                    "{CC5FD16D-436D-48AD-A40C-5A424C6E3E79}",  // new c# core (csproj)
                    "{F2A71F9B-5D33-465A-A702-920D77279786}",  // f#
                    "{F184B08F-C81C-45F6-A57F-5ABD9991F28F}",  // vb
                    "{FAE04EC0-301F-11D3-BF4B-00C04F79EFBC}"  // c#
                ];
                string[] ignoreguids = [
                    "{2150E333-8FDC-42A3-9474-1A3956D46DE8}",  // solution folder
                    "{E24C65DC-7377-472B-9ABA-BC803B73C61A}"  // website
                ];

                if (row.StartsWith("Project(\""))
                {
                    if (ignoreguids.Any(g => row.StartsWith($"Project(\"{g}\") =")))
                    {
                    }
                    else if (projtypeguids.Any(g => row.StartsWith($"Project(\"{g}\") =")))
                    {
                        string[] values = row[(row.IndexOf('=') + 1)..].Split(',');
                        if (values.Length != 3)
                        {
                            ConsoleHelper.WriteLineColor($"{SolutionFile}: Corrupt solution file: '{row}'", ConsoleColor.Yellow);
                            continue;
                        }

                        var path = values[1].Trim().Trim('"');

                        ProjectsPaths.Add(path);
                    }
                    else
                    {
                        ConsoleHelper.WriteLineColor($"{SolutionFile}: Ignoring unknown project type: '{row}'", ConsoleColor.Yellow);
                        continue;
                    }
                }
            }
        }
    }
}
