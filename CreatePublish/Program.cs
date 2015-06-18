using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Mail;
using System.Xml.Linq;

namespace CreatePublish
{
    class Program
    {
        private static string eol = Environment.NewLine;

        static void Main(string[] args)
        {
            string usage =
@"CreatePublish 1.6 - Program for creating msbuild publishing script of Web/MVC projects.

Usage: CreatePublish [-e] <solutionfile> <msbuildfile> <publishfolder>

Example: CreatePublish mysol.sln publishmvc.proj ..\Deploy";

            bool writeemptyfile = false;
            if (args.Contains("-e"))
            {
                writeemptyfile = true;
                args = args.Where(a => a != "-e").ToArray();
            }

            if (args.Length != 3)
            {
                Console.WriteLine(usage);
                return;
            }


            string solutionfile = args[0];
            string buildfile = args[1];
            string publishfolder = args[2];

            if (args[0] == "test" && args[1] == "test" && args[2] == "test")
            {
                Tuple<string, string, char[]>[] testvalues = new Tuple<string, string, char[]>[] {
                    Tuple.Create<string, string, char[]>("waaa", "wbbb, ", new char[] { '.' }),
                    Tuple.Create<string, string, char[]>("w.aaa", "w.bbb, ", new char[] { '.' }),
                    Tuple.Create<string, string, char[]>("wipcore.custo .mer!.app.", ". Wipco. reCustomer.app. sub. app, ", new char[] { '.' }),
                    Tuple.Create<string, string, char[]>("wipcore.custo .mer!.app.", ". Wipco. reCustomer.app. sub. app, ", new char[] { '.' }),
                    Tuple.Create<string, string, char[]>("wipcore.custo .mer!.app", ". Wipco. reCustomer.app. sub. app, ", new char[] { '.' }),
                    Tuple.Create<string, string, char[]>("wipcore.cuszto .mer!.app.", ". Wipco. reCustomer.app. sub. app, ", new char[] { '.' }),
                    Tuple.Create<string, string, char[]>(". Wipco. reCustomer.app. sub. app, ","wipcore.custo .mer!.app.", new char[] { '.' })
                };

                foreach (var testvalue in testvalues)
                {
                    string sol = testvalue.Item1;
                    string projectname = testvalue.Item2;
                    char[] keep = testvalue.Item3;

                    string result = string.Join(string.Empty, projectname.ToCharArray().Where(c => !char.IsWhiteSpace(c)));
                    Console.WriteLine("'" + projectname + "' '" + sol + "' '" + string.Join("", keep) + "' -> '" + result + "'");
                }

                return;
            }

            CreateBuildFile(solutionfile, buildfile, publishfolder, writeemptyfile);
        }

        static void CreateBuildFile(string solutionfile, string buildfile, string publishfolder, bool writeemptyfile)
        {
            Solution solution = new Solution(solutionfile);

            string[] webmvcguids =
            {
                "{603C0E0B-DB56-11DC-BE95-000D561079B0}",
                "{F85E285D-A4E0-4152-9332-AB1D724D3325}",
                "{E53F8FEA-EAE0-44A6-8774-FFD645390401}",
                "{E3E379DF-F4C6-4180-9B81-6769533ABE47}",
                "{349C5851-65DF-11DA-9384-00065B846F21}"
            };
            webmvcguids = webmvcguids.Select(g => g.ToLower()).ToArray();

            List<Project> projects = solution.LoadProjects();
            if (projects == null)
            {
                return;
            }

            List<Project> webmvcprojects = new List<Project>();

            foreach (Project project in projects)
            {
                if (project._ProjectTypeGuids.Select(g => g.ToLower()).Any(webmvcguids.Contains))
                {
                    webmvcprojects.Add(project);
                }
            }


            if (!writeemptyfile && webmvcprojects.Count == 0)
            {
                Console.WriteLine("No Web/MVC projects found.");
                return;
            }

            string xml1 = "<Project xmlns=\"http://schemas.microsoft.com/developer/msbuild/2003\">" + Environment.NewLine + "  <Target Name=\"Build\">" + Environment.NewLine;
            string xml2 = "  </Target>" + Environment.NewLine + "</Project>" + Environment.NewLine;

            string buf = xml1;
            string solutionname = Path.GetFileNameWithoutExtension(solutionfile);

            if (Path.IsPathRooted(solutionfile) && !Path.IsPathRooted(publishfolder))
            {
                publishfolder = FileHelper.CompactPath(Path.Combine(Environment.CurrentDirectory, publishfolder));
            }

            foreach (Project project in webmvcprojects.OrderBy(p => Path.GetFileNameWithoutExtension(p._sln_path)))
            {
                string filename = project._sln_path;

                if (filename.StartsWith(@".\"))
                {
                    filename = filename.Substring(2);
                }

                string relpath = FileHelper.GetRelativePath(Path.Combine(Path.GetDirectoryName(solutionfile), Path.GetDirectoryName(buildfile), project._sln_path), publishfolder);

                string projectname = Path.GetFileNameWithoutExtension(project._sln_path);
                string folder = string.Join(string.Empty, projectname.ToCharArray().Where(c => !char.IsWhiteSpace(c)));

                filename = Path.Combine(Path.GetDirectoryName(solutionfile), filename);

                relpath = Path.Combine(relpath, folder);

                Console.WriteLine("'" + solutionname + "' + '" + projectname + "' -> '" + relpath + "' (" + filename + ")");

                buf += "    <MSBuild Projects=\"" + filename + "\" Targets=\"PipelinePreDeployCopyAllFilesToOneFolder\" Properties=\"Configuration=Release;_PackageTempDir=" + relpath + "\" />" + Environment.NewLine;
            }

            buf += xml2;

            File.WriteAllText(buildfile, buf);
        }
    }
}
