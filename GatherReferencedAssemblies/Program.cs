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
            var gatherAssemblyReferences = args.Any(a => a == "-r");
            args = [.. args.Except(["-r"])];

            if (args.Length != 3)
            {
                var usage =
@"Gather Referenced Assemblies 1.0

GetAss.exe [-r] <project file> <build config> <output path>

-r:  Also gather all referenced assemblies (non-source project).

Example: getass myproj.csproj Release ..\libs";

                Console.WriteLine(usage);
                return true;
            }

            var projectFile = args[0];
            var buildConfig = args[1];
            var outputPath = args[2];

            List<Project> projects = [];
            List<FailedProject> fails = [];
            GetProjects(projectFile, ref projects, ref fails);
            projects = [.. projects.OrderBy(p => p.Path)];
            fails = [.. fails.OrderBy(f => f.Path)];
            ShowFails(fails, "Couldn't load {0} projects:");

            WriteLineColor($"Loaded {projects.Count} projects:", ConsoleColor.Green);
            foreach (var project in projects)
            {
                WriteLineColor($"  {project.Path}", ConsoleColor.Green);
            }

            fails = [];
            var assemblies = GetAssemblies(projects, buildConfig, ref fails, gatherAssemblyReferences);
            assemblies = [.. assemblies.OrderBy(a => a.Path)];
            fails = [.. fails.OrderBy(f => f.Path)];
            ShowFails(fails, "Couldn't find {0} assemblies:");

            WriteLineColor($"Found {assemblies.Count} assemblies:", ConsoleColor.Green);
            foreach (var ass in assemblies)
            {
                WriteLineColor($"  {ass.Path}", ConsoleColor.Green);
            }

            if (assemblies.Count == 0)
            {
                return true;
            }

            if (Environment.UserInteractive)
            {
                Console.WriteLine($"Press Enter to copy {assemblies.Count} files to {outputPath}...");
                if (Console.ReadKey().Key != ConsoleKey.Enter)
                {
                    return false;
                }
            }

            foreach (var ass in assemblies)
            {
                var destfile = Path.Combine(outputPath, Path.GetFileName(ass.Path));
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
            var path = CompactPath(project);

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
            catch (Exception ex) when (ex is IOException or XmlException)
            {
                fails.Add(new FailedProject { Path = project, Message = ex.Message });
                return;
            }

            projects.Add(new Project() { Path = project, Xdoc = xdoc });

            var ns = xdoc.Root.Name.Namespace;

            string[] projectPaths = [.. xdoc
                .Element(ns + "Project").Elements(ns + "ItemGroup").Elements(ns + "ProjectReference")
                .Where(el => el.Attribute("Include") != null)
                .OrderBy(el => Path.GetFileNameWithoutExtension(el.Attribute("Include").Value))
                .Select(el => el.Attribute("Include").Value)];

            foreach (var subProjectPath in projectPaths)
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
            if (fails.Count > 0)
            {
                fails = [.. fails.OrderBy(p => p.Path)];

                WriteLineColor(string.Format(formatstring, fails.Count), ConsoleColor.Red);
                foreach (var p in fails)
                {
                    WriteLineColor($"  {p.Path}: {p.Message}", ConsoleColor.Red);
                }
            }
        }

        static List<Assembly> GetAssemblies(List<Project> projects, string buildConfig, ref List<FailedProject> fails, bool gatherAssemblyReferences)
        {
            List<Assembly> assemblies = [];
            List<Assembly> assembliesMissing = [];

            foreach (var project in projects)
            {
                var xdoc = project.Xdoc;
                var ns = project.Xdoc.Root.Name.Namespace;

                string[] assemblynames = [.. xdoc.Element(ns + "Project").Elements(ns + "PropertyGroup").Elements(ns + "AssemblyName").OrderBy(el => el.Value).Select(el => el.Value)];

                if (assemblynames.Length > 1)
                {
                    fails.Add(new FailedProject { Path = project.Path, Message = "Too many AssemblyName tags found." });
                    continue;
                }
                if (assemblynames.Length < 1)
                {
                    fails.Add(new FailedProject { Path = project.Path, Message = "No AssemblyName tag found." });
                    continue;
                }

                string[] outputpaths = [.. xdoc.Element(ns + "Project").Elements(ns + "PropertyGroup").Elements(ns + "OutputPath").Where(el => MatchCondition(el.Parent.Attribute("Condition"), buildConfig)).Select(el => el.Value)];

                if (outputpaths.Length > 1)
                {
                    fails.Add(new FailedProject { Path = project.Path, Message = "Too many OutputPath tags found." });
                    continue;
                }
                if (outputpaths.Length < 1)
                {
                    fails.Add(new FailedProject { Path = project.Path, Message = "No OutputPath tag found." });
                    continue;
                }

                string[] outputtypes = [.. xdoc.Element(ns + "Project").Elements(ns + "PropertyGroup").Elements(ns + "OutputType").Select(el => el.Value)];

                if (outputtypes.Length > 1)
                {
                    fails.Add(new FailedProject { Path = project.Path, Message = "Too many OutputType tags found." });
                    continue;
                }
                if (outputtypes.Length < 1)
                {
                    fails.Add(new FailedProject { Path = project.Path, Message = "No OutputType tag found." });
                    continue;
                }

                if (gatherAssemblyReferences)
                {
                    XElement[] assemblyReferences = [.. project.Xdoc.Element(ns + "Project").Elements(ns + "ItemGroup").Elements(ns + "Reference").Where(el => el.Attribute("Include") != null && !Gac.IsSystemAssembly(el.Attribute("Include").Value.Split(',')[0], true))];

                    foreach (var assref in assemblyReferences)
                    {
                        var fullname = assref.Attribute("Include").Value;
                        var shortname = fullname.Split(',')[0];
                        XElement xele = assref.Element(ns + "HintPath");
                        if (xele == null)
                        {
                            // Might be ok
                            assembliesMissing.Add(new Assembly { Path = null, Shortname = shortname, ProjectPath = project.Path });
                            continue;
                        }
                        else
                        {
                            var hintpath = xele.Value;
                            var asspath = CompactPath(Path.Combine(Path.GetDirectoryName(project.Path), hintpath));

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

                var assemblyname = assemblynames.Single();
                var outputpath = outputpaths.Single();
                var outputtype = outputtypes.Single();

                var ext = outputtype switch
                {
                    "Library" => ".dll",
                    "WinExe" or "Exe" => ".exe",
                    _ => null
                };
                if (ext == null)
                {
                    fails.Add(new FailedProject { Path = project.Path, Message = $"Unsupported project type: '{outputtype}'" });
                    continue;
                }

                var path = CompactPath(Path.Combine(Path.Combine(Path.GetDirectoryName(project.Path), outputpath), assemblyname + ext));

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

            foreach (var ass in assembliesMissing)
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
            {
                return false;
            }

            var condition = xattr.Value;
            var pos = condition.IndexOf("==");
            if (pos >= 0)
            {
                var conditionvalues = condition[(pos + 2)..].Trim().Trim('\'').Split('|');
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
            List<string> folders = [.. path.Split(Path.DirectorySeparatorChar)];

            // Remove redundant folders
            for (var i = 0; i < folders.Count;)
            {
                if (i > 0 && folders[i] == ".." && folders[i - 1] != ".." && folders[i - 1] != string.Empty)
                {
                    folders.RemoveAt(i - 1);
                    folders.RemoveAt(i - 1);
                    i--;
                }
                else if (i > 0 && folders[i] == string.Empty && folders[i - 1] == string.Empty)
                {
                    folders.RemoveAt(i);
                }
                else
                {
                    i++;
                }
            }

            // Combine folders into path2
            var path2 = string.Join(Path.DirectorySeparatorChar.ToString(), folders);

            // If path had a starting/ending \, keep it
            var sep = Path.DirectorySeparatorChar.ToString();
            if (path2 == string.Empty && (path.StartsWith(sep) || path.EndsWith(sep)))
            {
                path2 = Path.DirectorySeparatorChar.ToString();
            }

            return path2;
        }

        public static void RemoveRO(string filename)
        {
            var fa = File.GetAttributes(filename);
            if ((fa & FileAttributes.ReadOnly) == FileAttributes.ReadOnly)
            {
                File.SetAttributes(filename, fa & ~FileAttributes.ReadOnly);
            }
        }

        public static void WriteLineColor(string text, ConsoleColor color)
        {
            var oldColor = Console.ForegroundColor;
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
