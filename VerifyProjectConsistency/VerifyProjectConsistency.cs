using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Linq;

namespace VerifyProjectConsistency
{
    enum Level { Error, Warning };

    class Diff
    {
        public string FolderName;
        public string ProjectName;
        public string AssemblyName;
        public string RootNamespace;
        public Level Level;
    }

    class ExcludeFolder
    {
        public string name;
        public bool used;
    }


    class VerifyProjectConsistency
    {
        static int Main(string[] args)
        {
            bool onlyErrors = args.Contains("-e");
            string[] parsedArgs = args
                .Where(a => a != "-e")
                .ToArray();

            ExcludeFolder[] excludeFolders = parsedArgs
                .Where(a => a.StartsWith("-"))
                .Select(a => new ExcludeFolder
                {
                    name = a.Substring(1),
                    used = false
                })
                .ToArray();
            parsedArgs = parsedArgs
                .Where(a => !a.StartsWith("-"))
                    .ToArray();

            if (parsedArgs.Length < 0 || parsedArgs.Length > 1)
            {
                Console.WriteLine(
@"VerifyProjectConsistency 1.1 - Verifies names and contents of VS project files.

Usage: VerifyProjectConsistency [-e] [path] [-exclude folder 1] [-exclude folder 2] ...

Default path is current directory.
-e  Show only projects with serious errors.

Return value: Number of errors + warnings, or only number of errors if -e is specified.

Will return number of excessive exclude folders, if any excessive exclude
folder is specified. This is done to make it easier to maintain an optimal
exclude filter while code in the verified folder change over time.");

                return 1;
            }

            string[] files = GetFiles(parsedArgs.Length == 1 ? parsedArgs[0] : ".");
            if (files == null)
            {
                return 1;
            }

            Diff[] diffs = GetDiffs(files, excludeFolders);
            if (diffs == null)
            {
                return 1;
            }

            PrintDiffs(diffs, onlyErrors);

            Console.WriteLine("Diff Count: " + diffs.Length + "/" + files.Length +
                " (" + diffs.Count(d => d.Level == Level.Error) + " errors, " +
                diffs.Count(d => d.Level == Level.Warning) + " warnings)");

            int diffcount = onlyErrors ? diffs.Count(d => d.Level == Level.Error) : diffs.Length;

            int exsessivecount = GetExcessiveExcludesCount(excludeFolders);
            if (exsessivecount > 0)
            {
                return exsessivecount;
            }

            return diffcount;
        }

        private static string[] GetFiles(string path)
        {
            string[] files;
            try
            {
                files = Directory.GetFiles(path, "*.*proj", SearchOption.AllDirectories);
            }
            catch (DirectoryNotFoundException ex)
            {
                WriteColor(ex.Message, ConsoleColor.Red);
                return null;
            }
            catch (ArgumentException ex)
            {
                WriteColor("Path: '" + path + "'. " + ex.Message, ConsoleColor.Red);
                return null;
            }

            files = files
                .Select(f => f.StartsWith(@".\") ? f.Substring(2) : f)
                .Where(f => !f.EndsWith(".vcxproj") && !f.EndsWith(".vcproj") && !f.EndsWith(".proj"))
                .ToArray();

            return files;
        }

        private static Diff[] GetDiffs(string[] files, ExcludeFolder[] excludeFolders)
        {
            List<Diff> diffs = new List<Diff>();

            foreach (string filename in files)
            {
                string FolderName = Path.GetFileName(Path.GetDirectoryName(filename));
                string ProjectName = Path.GetFileNameWithoutExtension(filename);

                XDocument xdoc;
                string AssemblyName, RootNamespace;

                try
                {
                    xdoc = XDocument.Load(filename);

                    AssemblyName = xdoc
                       .Descendants(xdoc.Root.Name.Namespace + "AssemblyName")
                       .FirstOrDefault()?.Value ?? string.Empty;
                    RootNamespace = xdoc
                       .Descendants(xdoc.Root.Name.Namespace + "RootNamespace")
                       .FirstOrDefault()?.Value ?? string.Empty;
                }
                catch (XmlException ex)
                {
                    if (ShouldExclude(filename, excludeFolders))
                    {
                        continue;
                    }

                    WriteColor("Project file: " + filename + ". " + ex.Message, ConsoleColor.Red);

                    Diff diff = new Diff
                    {
                        FolderName = FolderName,
                        ProjectName = ProjectName,
                        AssemblyName = string.Empty,
                        RootNamespace = string.Empty,
                        Level = Level.Error
                    };
                    diffs.Add(diff);

                    continue;
                }

                string[] names = new string[] { FolderName, ProjectName, AssemblyName, RootNamespace };

                if (names.Any(n => n.Length > FolderName.Length && !n.EndsWith(FolderName)) ||
                    names.Any(n => n.Length >= ProjectName.Length && !n.EndsWith(ProjectName)) ||
                    names.Any(n => n.Length >= AssemblyName.Length && !n.EndsWith(AssemblyName)) ||
                    names.Any(n => n.Length >= RootNamespace.Length && !n.EndsWith(RootNamespace)))
                {
                    if (ShouldExclude(filename, excludeFolders))
                    {
                        continue;
                    }

                    Diff diff = new Diff
                    {
                        FolderName = FolderName,
                        ProjectName = ProjectName,
                        AssemblyName = AssemblyName,
                        RootNamespace = RootNamespace,
                        Level = Level.Error
                    };
                    diffs.Add(diff);
                }
                else if (FolderName != ProjectName || FolderName != AssemblyName || FolderName != RootNamespace)
                {
                    if (ShouldExclude(filename, excludeFolders))
                    {
                        continue;
                    }

                    Diff diff = new Diff
                    {
                        FolderName = FolderName,
                        ProjectName = ProjectName,
                        AssemblyName = AssemblyName,
                        RootNamespace = RootNamespace,
                        Level = Level.Warning
                    };
                    diffs.Add(diff);
                }
            }

            return diffs.ToArray();
        }

        private static bool ShouldExclude(string filename, ExcludeFolder[] excludeFolders)
        {
            bool exclude = false;
            foreach (ExcludeFolder excludeFolder in excludeFolders)
            {
                if (filename.Split(Path.DirectorySeparatorChar).Contains(excludeFolder.name))
                {
                    excludeFolder.used = true;
                    exclude = true;
                }
            }
            if (exclude)
            {
                return true;
            }

            return false;
        }

        private static void PrintDiffs(Diff[] diffs, bool onlyErrors)
        {
            Diff[] diffs2 = diffs
                .Where(d => !onlyErrors || d.Level == Level.Error)
                .ToArray();

            if (diffs2.Length == 0)
            {
                return;
            }

            string[] lengths = {
                "{0,-" + diffs2.Max(d => d.FolderName.Length) + "} ",
                "{0,-" + diffs2.Max(d => d.ProjectName.Length) + "} ",
                "{0,-" + diffs2.Max(d => d.AssemblyName.Length)+ "} "};

            Console.WriteLine(
                string.Format(lengths[0], "FolderName") +
                string.Format(lengths[1], "ProjectName") +
                string.Format(lengths[2], "AssemblyName") +
                "RootNamespace");


            WriteCollection(diffs
                .Where(d => d.Level == Level.Error)
                .OrderBy(d => d.FolderName)
                .ThenBy(d => d.ProjectName)
                .ThenBy(d => d.AssemblyName)
                .ThenBy(d => d.RootNamespace)
                .Select(d =>
                    string.Format(lengths[0], d.FolderName) +
                    string.Format(lengths[1], d.ProjectName) +
                    string.Format(lengths[2], d.AssemblyName) + d.RootNamespace),
                ConsoleColor.Red);

            if (!onlyErrors)
            {
                WriteCollection(diffs
                    .Where(d => d.Level == Level.Warning)
                    .OrderBy(d => d.FolderName)
                    .ThenBy(d => d.ProjectName)
                    .ThenBy(d => d.AssemblyName)
                    .ThenBy(d => d.RootNamespace)
                    .Select(d =>
                        string.Format(lengths[0], d.FolderName) +
                        string.Format(lengths[1], d.ProjectName) +
                        string.Format(lengths[2], d.AssemblyName) + d.RootNamespace),
                ConsoleColor.Yellow);
            }
        }

        private static int GetExcessiveExcludesCount(ExcludeFolder[] excludeFolders)
        {
            int count = excludeFolders.Count(e => !e.used);

            if (count > 0)
            {
                Console.WriteLine("Excessive exclude folders specified, please remove all excessive exclude folders from command line:");

                foreach (ExcludeFolder excludeFolder in excludeFolders)
                {
                    if (!excludeFolder.used)
                    {
                        WriteColor(excludeFolder.name, ConsoleColor.Red);
                    }
                }

                return count;
            }

            return 0;
        }

        private static void WriteCollection(IEnumerable<string> collection, ConsoleColor color)
        {
            ConsoleColor oldColor = Console.ForegroundColor;
            try
            {
                Console.ForegroundColor = color;
                Console.WriteLine(string.Join(Environment.NewLine, collection));
            }
            finally
            {
                Console.ForegroundColor = oldColor;
            }
        }

        private static void WriteColor(string message, ConsoleColor color)
        {
            ConsoleColor oldColor = Console.ForegroundColor;
            try
            {
                Console.ForegroundColor = color;
                Console.WriteLine(message);
            }
            finally
            {
                Console.ForegroundColor = oldColor;
            }
        }

    }
}
