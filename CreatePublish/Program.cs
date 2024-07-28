using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace CreatePublish
{
    class Program
    {
        static void Main(string[] args)
        {
            var usage =
@"CreatePublish 1.8 - Program for creating msbuild publishing script of Web/MVC projects.

Usage: CreatePublish [-e] <solutionfile> <msbuildfile> <publishfolder>

-e:  Write empty file even if no valid projects found.

Example: CreatePublish mysol.sln publishmvc.proj ..\Deploy";

            var parsedArgs = args;

            var writeemptyfile = false;
            if (parsedArgs.Contains("-e"))
            {
                writeemptyfile = true;
                parsedArgs = [.. parsedArgs.Where(a => a != "-e")];
            }

            if (parsedArgs.Length != 3)
            {
                Console.WriteLine(usage);
                return;
            }

            var solutionfile = parsedArgs[0];
            var buildfile = parsedArgs[1];
            var publishfolder = parsedArgs[2];

            if (parsedArgs[0] == "test" && parsedArgs[1] == "test" && parsedArgs[2] == "test")
            {
                Test.Test1();
                Console.ReadKey();
                return;
            }
            else if (parsedArgs[0] == "test" && parsedArgs[1] == "test" && parsedArgs[2] == "test2")
            {
                Test.Test2();
                Console.ReadKey();
                return;
            }
            else if (parsedArgs[0] == "test" && parsedArgs[1] == "test" && parsedArgs[2] == "test3")
            {
                FileHelper.TestGetRelativePath();
                Console.ReadKey();
                return;
            }

            CreateBuildFile(solutionfile, buildfile, publishfolder, writeemptyfile);
        }

        public static void CreateBuildFile(string solutionfile, string buildfile, string publishfolder, bool writeemptyfile)
        {
            var content = GenerateBuildFileContent(solutionfile, buildfile, publishfolder, writeemptyfile);

            File.WriteAllText(buildfile, content);
        }

        public static string GenerateBuildFileContent(string solutionfile, string buildfile, string publishfolder, bool generateemptyfile)
        {
            Solution solution = new(solutionfile);

            string[] webmvcguids =
            [
                "{603C0E0B-DB56-11DC-BE95-000D561079B0}",
                "{F85E285D-A4E0-4152-9332-AB1D724D3325}",
                "{E53F8FEA-EAE0-44A6-8774-FFD645390401}",
                "{E3E379DF-F4C6-4180-9B81-6769533ABE47}",
                "{349C5851-65DF-11DA-9384-00065B846F21}"
            ];
            webmvcguids = [.. webmvcguids.Select(g => g.ToLower())];

            var projects = solution.LoadProjects();
            if (projects == null)
            {
                return null;
            }

            List<Project> webmvcprojects = [];

            foreach (var project in projects)
            {
                if (project._ProjectTypeGuids.Select(g => g.ToLower()).Any(webmvcguids.Contains))
                {
                    webmvcprojects.Add(project);
                }
            }

            if (!generateemptyfile && webmvcprojects.Count == 0)
            {
                Console.WriteLine("No Web/MVC projects found.");
                return string.Empty;
            }

            StringBuilder sb = new();
            sb.AppendLine("<Project xmlns=\"http://schemas.microsoft.com/developer/msbuild/2003\">");
            sb.AppendLine("  <Target Name=\"Build\">");

            var solutionname = Path.GetFileNameWithoutExtension(solutionfile);

            if (Path.IsPathRooted(solutionfile) || Path.IsPathRooted(buildfile) || Path.IsPathRooted(publishfolder))
            {
                solutionfile = Path.GetFullPath(solutionfile);
                buildfile = Path.GetFullPath(buildfile);
                publishfolder = Path.GetFullPath(publishfolder);
            }

            if (Environment.GetEnvironmentVariable("VerbosePublish") == "true")
            {
                Console.WriteLine($"solutionfile:  '{solutionfile}'");
                Console.WriteLine($"buildfile:     '{buildfile}'");
                Console.WriteLine($"publishfolder: '{publishfolder}'");
            }

            foreach (var project in webmvcprojects.OrderBy(p => Path.GetFileNameWithoutExtension(p._sln_path)))
            {
                var slnpath = project._sln_path;

                if (slnpath.StartsWith(@".\"))
                {
                    slnpath = slnpath[2..];
                }

                var projectname = Path.GetFileNameWithoutExtension(project._sln_path);
                var publishfolder2 = string.Join(string.Empty, projectname.ToCharArray().Where(c => !char.IsWhiteSpace(c)));

                // projfilename = (curdir -> ) buildfile -> project
                var projfilename = FileHelper.GetRelativePath(Path.GetDirectoryName(buildfile), Path.Combine(Path.GetDirectoryName(solutionfile), slnpath));

                // publishfolder = (curdir -> ) project -> publishfolder
                var publishfolder3 = FileHelper.GetRelativePath(Path.GetDirectoryName(Path.Combine(Path.GetDirectoryName(solutionfile), slnpath)), Path.Combine(publishfolder, publishfolder2));

                Console.WriteLine($"'{solutionname}' + '{projectname}' -> '{publishfolder3}' ({projfilename})");

                sb.AppendLine($"    <MSBuild Targets=\"PipelinePreDeployCopyAllFilesToOneFolder\" Properties=\"Configuration=Release;_PackageTempDir={publishfolder3}\" Projects=\"{projfilename}\" />");
            }

            sb.AppendLine("  </Target>");
            sb.AppendLine("</Project>");

            return sb.ToString();
        }
    }
}
