using System;
using System.IO;
using System.Linq;
using System.Xml.Linq;

namespace CreatePublish
{
    class Test
    {
        public static void Test1()
        {
            Tuple<string, string, char[]>[] testvalues = [
                Tuple.Create<string, string, char[]>("waaa", "wbbb, ", ['.']),
                Tuple.Create<string, string, char[]>("w.aaa", "w.bbb, ", ['.']),
                Tuple.Create<string, string, char[]>("mycompany.custo .mer!.app.", ". Mycompa. nyCustomer.app. sub. app, ", ['.']),
                Tuple.Create<string, string, char[]>("mycompany.custo .mer!.app.", ". Mycompa. ny.app. sub. app, ", ['.']),
                Tuple.Create<string, string, char[]>("mycompany.custo .mer!.app", ". Mycompa. nyCustomer.app. sub. app, ", ['.']),
                Tuple.Create<string, string, char[]>("mycompany.cuszto .mer!.app.", ". Mycompa. nyCustomer.app. sub. app, ", ['.']),
                Tuple.Create<string, string, char[]>(". Mycompa. nyCustomer.app. sub. app, ","mycompany.custo .mer!.app.", ['.'])
            ];

            foreach (var testvalue in testvalues)
            {
                var sol = testvalue.Item1;
                var projectname = testvalue.Item2;
                var keep = testvalue.Item3;

                var result = string.Join(string.Empty, projectname.ToCharArray().Where(c => !char.IsWhiteSpace(c)));
                Console.WriteLine($"'{projectname}' '{sol}' '{string.Join("", keep)}' -> '{result}'");
            }
        }

        public static void Test2()
        {
            {
                var solutionfile = Path.Combine(Directory.GetCurrentDirectory(), @"aa.bb\cc.dd.sln");
                var projectfile = Path.Combine(Directory.GetCurrentDirectory(), @"aa.bb\My.Web\My.Web .csproj");
                var buildfile = Path.Combine(Directory.GetCurrentDirectory(), @"aa.bb\cc.dd.sln_web.build");
                var publishfolder = Path.Combine(Directory.GetCurrentDirectory(), @"Artifacts");

                var buildtoproject = @"My.Web\My.Web .csproj";
                var buildtopublish = @"..\..\Artifacts\My.Web";

                Setup(solutionfile, projectfile, buildfile, publishfolder);
                Program.CreateBuildFile(solutionfile, buildfile, publishfolder, false);
                Validate(buildfile, buildtoproject, buildtopublish);
                Cleanup(solutionfile, projectfile, buildfile);
            }

            Console.WriteLine();

            {
                var solutionfile = @"aa.bb\cc.dd.sln";
                var projectfile = @"aa.bb\My.Web\My.Web .csproj";
                var buildfile = @"aa.bb\cc.dd.sln_web.build";
                var publishfolder = @"Artifacts";

                var buildtoproject = @"My.Web\My.Web .csproj";
                var buildtopublish = @"..\..\Artifacts\My.Web";

                Setup(solutionfile, projectfile, buildfile, publishfolder);
                Program.CreateBuildFile(solutionfile, buildfile, publishfolder, false);
                Validate(buildfile, buildtoproject, buildtopublish);
                Cleanup(solutionfile, projectfile, buildfile);
            }

            Console.WriteLine();

            {
                var solutionfile = @"aa.bb\cc.dd.sln";
                var projectfile = @"aa.bb\My.Web\My.Web .csproj";
                var buildfile = @"web.build";
                var publishfolder = @"Artifacts";

                var buildtoproject = @"aa.bb\My.Web\My.Web .csproj";
                var buildtopublish = @"Artifacts\My.Web";

                Setup(solutionfile, projectfile, buildfile, publishfolder);
                Program.CreateBuildFile(solutionfile, buildfile, publishfolder, false);
                Validate(buildfile, buildtoproject, buildtopublish);
                Cleanup(solutionfile, projectfile, buildfile);
            }
        }

        private static void Setup(string solutionfile, string projectfile, string buildfile, string publishfolder)
        {
            Console.WriteLine($"solutionfile: '{solutionfile}'");
            Console.WriteLine($"projectfile: '{projectfile}'");
            Console.WriteLine($"buildfile: '{buildfile}'");
            Console.WriteLine($"publishfolder: '{publishfolder}'");
            var slnpath = FileHelper.GetRelativePath(Path.GetDirectoryName(Path.GetFullPath(solutionfile)), Path.GetFullPath(projectfile));

            Console.WriteLine($"slnpath: '{slnpath}'");

            var solutioncontent = "Project(\"{FAE04EC0-301F-11D3-BF4B-00C04F79EFBC}\") = \"" + Path.GetFileNameWithoutExtension(slnpath) +
                "\", \"" + slnpath + "\", \"{12121212-1212-1212-1212-121212121212}\"";

            var folder = Path.GetDirectoryName(solutionfile);
            Console.WriteLine($"Creating directory: '{folder}'");
            _ = Directory.CreateDirectory(folder);
            File.WriteAllText(solutionfile, solutioncontent);

            var projectcontent = @"<Project><PropertyGroup><ProjectTypeGuids>{E3E379DF-F4C6-4180-9B81-6769533ABE47}</ProjectTypeGuids></PropertyGroup></Project>";

            folder = Path.GetDirectoryName(projectfile);
            Console.WriteLine($"Creating directory: '{folder}'");
            _ = Directory.CreateDirectory(folder);
            File.WriteAllText(projectfile, projectcontent);
        }

        private static void Validate(string buildfile, string buildtoproject, string buildtopublish)
        {
            var xdoc = XDocument.Load(buildfile);
            var ns = xdoc.Root.Name.Namespace;

            XElement msbuild = xdoc.Element(ns + "Project").Element(ns + "Target").Element(ns + "MSBuild");

            var pathtoproject = msbuild.Attribute("Projects").Value;
            var pathtopublish = msbuild.Attribute("Properties").Value.Split(';')[1].Split('=')[1];

            if (pathtoproject == buildtoproject)
            {
                ConsoleHelper.ColorWrite(ConsoleColor.Green, $"Valid: Should be: '{buildtoproject}', was: '{pathtoproject}'");
            }
            else
            {
                ConsoleHelper.ColorWrite(ConsoleColor.Red, $"Error: Should be: '{buildtoproject}', was: '{pathtoproject}'");
            }

            if (pathtopublish == buildtopublish)
            {
                ConsoleHelper.ColorWrite(ConsoleColor.Green, $"Valid: Should be: '{buildtopublish}', was: '{pathtopublish}'");
            }
            else
            {
                ConsoleHelper.ColorWrite(ConsoleColor.Red, $"Error: Should be: '{buildtopublish}', was: '{pathtopublish}'");
            }
        }

        private static void Cleanup(string solutionfile, string projectfile, string buildfile)
        {
            if (Environment.GetEnvironmentVariable("KeepTestFiles") == "true")
            {
                return;
            }

            Console.WriteLine($"Deleting: '{solutionfile}'");
            File.Delete(solutionfile);
            Console.WriteLine($"Deleting: '{projectfile}'");
            File.Delete(projectfile);
            Console.WriteLine($"Deleting: '{buildfile}'");
            File.Delete(buildfile);

            Console.WriteLine($"Deleting folders: '{Path.GetDirectoryName(solutionfile)}' ...");
            DeleteFoldersIfEmpty(Path.GetDirectoryName(solutionfile));
            Console.WriteLine($"Deleting folders: '{Path.GetDirectoryName(projectfile)}' ...");
            DeleteFoldersIfEmpty(Path.GetDirectoryName(projectfile));
            Console.WriteLine($"Deleting folders: '{Path.GetDirectoryName(buildfile)}' ...");
            DeleteFoldersIfEmpty(Path.GetDirectoryName(buildfile));
        }

        private static void DeleteFoldersIfEmpty(string folder)
        {
            while (folder.Length > 0)
            {
                if (Directory.Exists(folder))
                {
                    var files = Directory.GetFiles(folder, "*", SearchOption.AllDirectories);
                    var dirs = Directory.GetDirectories(folder, "*", SearchOption.AllDirectories);

                    if (files.Length > 0 || dirs.Length > 0)
                    {
                        return;
                    }
                    Console.WriteLine($"Deleting directory: '{folder}'");
                    try
                    {
                        Directory.Delete(folder);
                    }
                    catch (IOException)
                    {
                        // Absolute paths may not be deletable
                    }
                }
                folder = Path.GetDirectoryName(folder);
            }
        }
    }
}
