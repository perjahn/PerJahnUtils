using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml;
using System.Xml.Linq;

namespace GatherReferencedAssemblies
{
    class Project
    {
        public string Path { get; set; }
        //public List<string> assnames { get; set; }
        public XDocument Xdoc { get; set; }
    }

    class FailedProject
    {
        public string Path { get; set; }
        public string Message { get; set; }
    }

    class Assembly
    {
        public string Path { get; set; }
        public string Shortname { get; set; }
        public string ProjectPath { get; set; }
    }

    class Program
    {
        static void Main(string[] args)
        {
            bool pause = ParseArguments(args);
            if (pause && Environment.UserInteractive)
            {
                Console.WriteLine("\nPress any key to continue...");
                Console.ReadKey();
            }
        }

        static bool ParseArguments(string[] args)
        {
            bool gatherAssemblyReferences = args.Any(a => a == "-r");
            args = args.Except(new string[] { "-r" }).ToArray();


            if (args.Length != 3)
            {
                string usage =
@"Gather Referenced Assemblies 1.0

GetAss.exe [-r] <project file> <build config> <output path>

-r:  Also gather all referenced assemblies (non-source project).

Example: getass myproj.csproj Release ..\libs";

                Console.WriteLine(usage);
                return true;
            }

            string projectFile = args[0];
            string buildConfig = args[1];
            string outputPath = args[2];


            List<Project> projects = new List<Project>();
            List<FailedProject> fails = new List<FailedProject>();
            GetProjects(projectFile, ref projects, ref fails);
            projects = projects.OrderBy(p => p.Path).ToList();
            fails = fails.OrderBy(f => f.Path).ToList();
            ShowFails(fails, "Couldn't load {0} projects:");

            WriteLineColor($"Loaded {projects.Count()} projects:", ConsoleColor.Green);
            foreach (Project project in projects)
            {
                WriteLineColor($"  {project.Path}", ConsoleColor.Green);
            }


            fails = new List<FailedProject>();
            List<Assembly> assemblies = GetAssemblies(projects, buildConfig, ref fails, gatherAssemblyReferences);
            assemblies = assemblies.OrderBy(a => a.Path).ToList();
            fails = fails.OrderBy(f => f.Path).ToList();
            ShowFails(fails, "Couldn't find {0} assemblies:");

            WriteLineColor($"Found {assemblies.Count()} assemblies:", ConsoleColor.Green);
            foreach (Assembly ass in assemblies)
            {
                WriteLineColor($"  {ass.Path}", ConsoleColor.Green);
            }


            if (assemblies.Count() == 0)
            {
                return true;
            }

            if (Environment.UserInteractive)
            {
                Console.WriteLine($"Press Enter to copy {assemblies.Count()} files to {outputPath}...");
                if (Console.ReadKey().Key != ConsoleKey.Enter)
                {
                    return false;
                }
            }

            foreach (Assembly ass in assemblies)
            {
                string destfile = Path.Combine(outputPath, Path.GetFileName(ass.Path));
                Console.WriteLine($"Copying '{ass.Path}' -> '{destfile}'...");
                if (File.Exists(destfile))
                {
                    RemoveRO(destfile);
                }
                File.Copy(ass.Path, destfile, true);
                RemoveRO(destfile);
            }

            return true;
        }

        static void GetProjects(string project, ref List<Project> projects, ref List<FailedProject> fails)
        {
            string path = CompactPath(project);

            if (projects.Any(p => string.Compare(p.Path, path, true) == 0) || fails.Any(p => string.Compare(p.Path, path, true) == 0))
            {
                return;
            }

            XDocument xdoc;

            try
            {
                //Console.WriteLine($"Loading project: '{project}'");
                xdoc = XDocument.Load(project);
            }
            catch (IOException ex)
            {
                fails.Add(new FailedProject { Path = project, Message = ex.Message });
                return;
            }
            catch (XmlException ex)
            {
                fails.Add(new FailedProject() { Path = project, Message = ex.Message });
                return;
            }

            projects.Add(new Project() { Path = project, Xdoc = xdoc });


            XNamespace ns = xdoc.Root.Name.Namespace;

            IEnumerable<string> projectPaths =
                    from el in xdoc.Element(ns + "Project").Elements(ns + "ItemGroup").Elements(ns + "ProjectReference")
                    where el.Attribute("Include") != null
                    orderby Path.GetFileNameWithoutExtension(el.Attribute("Include").Value)
                    select el.Attribute("Include").Value;

            foreach (string subProjectPath in projectPaths)
            {
                path = subProjectPath;
                if (!Path.IsPathRooted(path))
                {
                    path = CompactPath(Path.Combine(Path.GetDirectoryName(project), path));
                }

                GetProjects(path, ref projects, ref fails);
            }
        }

        static void ShowFails(List<FailedProject> fails, string formatstring)
        {
            if (fails.Count() > 0)
            {
                fails = fails.OrderBy(p => p.Path).ToList();

                WriteLineColor(string.Format(formatstring, fails.Count()), ConsoleColor.Red);
                foreach (FailedProject p in fails)
                {
                    WriteLineColor($"  {p.Path}: {p.Message}", ConsoleColor.Red);
                }
            }
        }

        static List<Assembly> GetAssemblies(List<Project> projects, string buildConfig, ref List<FailedProject> fails, bool gatherAssemblyReferences)
        {
            List<Assembly> assemblies = new List<Assembly>();
            List<Assembly> assembliesMissing = new List<Assembly>();

            foreach (Project project in projects)
            {
                XDocument xdoc = project.Xdoc;
                XNamespace ns = project.Xdoc.Root.Name.Namespace;


                IEnumerable<string> assemblynames =
                        from el in xdoc.Element(ns + "Project").Elements(ns + "PropertyGroup").Elements(ns + "AssemblyName")
                        orderby el.Value
                        select el.Value;

                if (assemblynames.Count() > 1)
                {
                    fails.Add(new FailedProject { Path = project.Path, Message = "Too many AssemblyName tags found." });
                    continue;
                }
                if (assemblynames.Count() < 1)
                {
                    fails.Add(new FailedProject { Path = project.Path, Message = "No AssemblyName tag found." });
                    continue;
                }


                IEnumerable<string> outputpaths =
                    from el in xdoc.Element(ns + "Project").Elements(ns + "PropertyGroup").Elements(ns + "OutputPath")
                    where MatchCondition(el.Parent.Attribute("Condition"), buildConfig)
                    select el.Value;

                if (outputpaths.Count() > 1)
                {
                    fails.Add(new FailedProject { Path = project.Path, Message = "Too many OutputPath tags found." });
                    continue;
                }
                if (outputpaths.Count() < 1)
                {
                    fails.Add(new FailedProject { Path = project.Path, Message = "No OutputPath tag found." });
                    continue;
                }


                IEnumerable<string> outputtypes =
                    from el in xdoc.Element(ns + "Project").Elements(ns + "PropertyGroup").Elements(ns + "OutputType")
                    select el.Value;

                if (outputtypes.Count() > 1)
                {
                    fails.Add(new FailedProject { Path = project.Path, Message = "Too many OutputType tags found." });
                    continue;
                }
                if (outputtypes.Count() < 1)
                {
                    fails.Add(new FailedProject { Path = project.Path, Message = "No OutputType tag found." });
                    continue;
                }


                if (gatherAssemblyReferences)
                {
                    IEnumerable<XElement> assemblyReferences =
                            from el in project.Xdoc.Element(ns + "Project").Elements(ns + "ItemGroup").Elements(ns + "Reference")
                            where el.Attribute("Include") != null && !Gac.IsSystemAssembly(el.Attribute("Include").Value.Split(',')[0], true)
                            select el;

                    foreach (XElement assref in assemblyReferences)
                    {
                        string fullname = assref.Attribute("Include").Value;
                        string shortname = fullname.Split(',')[0];
                        XElement xele = assref.Element(ns + "HintPath");
                        if (xele == null)
                        {
                            // Might be ok
                            assembliesMissing.Add(new Assembly { Path = null, Shortname = shortname, ProjectPath = project.Path });
                            continue;
                        }
                        else
                        {
                            string hintpath = xele.Value;
                            string asspath = CompactPath(Path.Combine(Path.GetDirectoryName(project.Path), hintpath));

                            if (assemblies.Any(p => string.Compare(Path.GetFileName(p.Path), Path.GetFileName(asspath), true) == 0))
                            {
                                continue;
                            }

                            if (!File.Exists(asspath))
                            {
                                assembliesMissing.Add(new Assembly { Path = asspath, Shortname = shortname, ProjectPath = project.Path });
                                continue;
                            }

                            assemblies.Add(new Assembly { Path = asspath, Shortname = shortname });
                        }
                    }
                }


                string assemblyname = assemblynames.Single();
                string outputpath = outputpaths.Single();
                string outputtype = outputtypes.Single();

                string ext;
                switch (outputtype)
                {
                    case "Library":
                        ext = ".dll";
                        break;
                    case "WinExe":
                        ext = ".exe";
                        break;
                    case "Exe":
                        ext = ".exe";
                        break;
                    default:
                        fails.Add(new FailedProject { Path = project.Path, Message = $"Unsupported project type: '{outputtype}'" });
                        continue;
                }


                string path = CompactPath(Path.Combine(Path.Combine(Path.GetDirectoryName(project.Path), outputpath), assemblyname + ext));

                if (assemblies.Any(p => string.Compare(p.Path, path, true) == 0) ||
                    assembliesMissing.Any(p => string.Compare(p.Path, path, true) == 0))
                {
                    continue;
                }

                if (!File.Exists(path))
                {
                    assembliesMissing.Add(new Assembly { Path = path, Shortname = assemblyname, ProjectPath = project.Path });
                    continue;
                }

                assemblies.Add(new Assembly { Path = path, Shortname = assemblyname });
            }


            foreach (Assembly ass in assembliesMissing)
            {
                if (ass.Path == null)
                {
                    if (assemblies.Any(a => string.Compare(a.Shortname, ass.Shortname, true) == 0))
                    {
                        continue;
                    }
                    else
                    {
                        WriteLineColor($"{ass.ProjectPath}: Reference: '{ass.Shortname}', Warning: HintPath not found, assembly file ignored!",
                            ConsoleColor.Yellow);
                    }
                }
                else
                {
                    WriteLineColor($"{ass.ProjectPath}: Reference: '{ass.Shortname}', Warning: File not found, assembly file ignored: '{ass.Path}'",
                        ConsoleColor.Yellow);
                }

                //fails.Add(new FailedProject { path = project.path, message = $"File not found: '{path}'" });
            }


            return assemblies;
        }

        static bool MatchCondition(XAttribute xattr, string buildConfig)
        {
            if (xattr == null)
                return false;

            string condition = xattr.Value;
            int pos = condition.IndexOf("==");
            if (pos >= 0)
            {
                string[] conditionvalues = condition.Substring(pos + 2).Trim().Trim('\'').Split('|');
                if (conditionvalues.Any(c => c.Trim() == buildConfig))
                {
                    return true;
                }
            }
            return false;
        }

        // Remove unnecessary .. from path
        // dir1\dir2\..\dir3 -> dir1\dir3
        // This code is 100% robust!
        public static string CompactPath(string path)
        {
            List<string> folders = path.Split(Path.DirectorySeparatorChar).ToList();

            // Remove redundant folders
            for (int i = 0; i < folders.Count;)
            {
                if (i > 0 && folders[i] == ".." && folders[i - 1] != ".." && folders[i - 1] != "")
                {
                    folders.RemoveAt(i - 1);
                    folders.RemoveAt(i - 1);
                    i--;
                }
                else if (i > 0 && folders[i] == "" && folders[i - 1] == "")
                {
                    folders.RemoveAt(i);
                }
                else
                {
                    i++;
                }
            }

            // Combine folders into path2
            string path2 = string.Join(Path.DirectorySeparatorChar.ToString(), folders.ToArray());

            // If path had a starting/ending \, keep it
            string sep = Path.DirectorySeparatorChar.ToString();
            if (path2 == "" && (path.StartsWith(sep) || path.EndsWith(sep)))
            {
                path2 = Path.DirectorySeparatorChar.ToString();
            }

            return path2;
        }

        public static void RemoveRO(string filename)
        {
            FileAttributes fa = File.GetAttributes(filename);
            if ((fa & FileAttributes.ReadOnly) == FileAttributes.ReadOnly)
            {
                File.SetAttributes(filename, fa & ~FileAttributes.ReadOnly);
            }
        }

        public static void WriteLineColor(string text, ConsoleColor color)
        {
            ConsoleColor oldColor = Console.ForegroundColor;
            try
            {
                Console.ForegroundColor = color;
                Console.WriteLine(text);
            }
            finally
            {
                Console.ForegroundColor = oldColor;
            }
        }
    }
}
