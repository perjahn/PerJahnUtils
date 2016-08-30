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

    class diff
    {
        public string FolderName;
        public string ProjectName;
        public string AssemblyName;
        public string RootNamespace;
        public Level Level;
    }

    class VerifyProjectConsistency
    {
        static int Main(string[] args)
        {
            bool onlyErrors = args.Contains("-e");
            string[] parsedArgs = args
                .Where(a => a != "-e")
                .ToArray();

            string[] excludeFolders = parsedArgs
                .Where(a => a.StartsWith("-"))
                .Select(a => a.Substring(1))
                .ToArray();
            parsedArgs = parsedArgs
                .Where(a => !a.StartsWith("-"))
                .ToArray();

            if (parsedArgs.Length < 0 || parsedArgs.Length > 1)
            {
                Console.WriteLine(
@"VerifyProjectConsistency 1.0 - Verifies names and contents of VS project files.

Usage: VerifyProjectConsistency [-e] [path] [-exclude folder 1] [-exclude folder 2] ...

Default path is current directory.
-e  Show only projects with serious errors.

Return value: Number of errors + warnings, or only
              number of errors if -e is specified.");

                return 1;
            }

            string[] files = GetFiles(parsedArgs.Length == 1 ? parsedArgs[0] : ".", excludeFolders);
            if (files == null)
            {
                return 1;
            }

            diff[] diffs = GetDiffs(files);
            if (diffs == null)
            {
                return 1;
            }

            PrintDiffs(diffs, onlyErrors);

            Console.WriteLine("Diff Count: " + diffs.Length + "/" + files.Length +
                " (" + diffs.Count(d => d.Level == Level.Error) + " errors, " +
                diffs.Count(d => d.Level == Level.Warning) + " warnings)");

            return onlyErrors ? diffs.Count(d => d.Level == Level.Error) : diffs.Length;
        }

        private static string[] GetFiles(string path, string[] excludeFolders)
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

            return files
                .Select(f => f.StartsWith(@".\") ? f.Substring(2) : f)
                .Where(f => !f.EndsWith(".vcxproj") && !f.EndsWith(".vcproj") && !f.EndsWith(".proj") &&
                    !(excludeFolders.Any(f.Contains)))
                .ToArray();
        }

        private static diff[] GetDiffs(string[] files)
        {
            List<diff> diffs = new List<diff>();

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
                    WriteColor("Project file: " + filename + ". " + ex.Message, ConsoleColor.Red);

                    diff diff = new diff();
                    diff.FolderName = FolderName;
                    diff.ProjectName = ProjectName;
                    diff.AssemblyName = string.Empty;
                    diff.RootNamespace = string.Empty;
                    diff.Level = Level.Error;
                    diffs.Add(diff);
                    continue;
                }

                string[] names = new string[] { FolderName, ProjectName, AssemblyName, RootNamespace };

                if (names.Any(n => n.Length > FolderName.Length && !n.EndsWith(FolderName)) ||
                    names.Any(n => n.Length >= ProjectName.Length && !n.EndsWith(ProjectName)) ||
                    names.Any(n => n.Length >= AssemblyName.Length && !n.EndsWith(AssemblyName)) ||
                    names.Any(n => n.Length >= RootNamespace.Length && !n.EndsWith(RootNamespace)))
                {
                    diff diff = new diff();
                    diff.FolderName = FolderName;
                    diff.ProjectName = ProjectName;
                    diff.AssemblyName = AssemblyName;
                    diff.RootNamespace = RootNamespace;
                    diff.Level = Level.Error;
                    diffs.Add(diff);
                }
                else if (FolderName != ProjectName || FolderName != AssemblyName || FolderName != RootNamespace)
                {
                    diff diff = new diff();
                    diff.FolderName = FolderName;
                    diff.ProjectName = ProjectName;
                    diff.AssemblyName = AssemblyName;
                    diff.RootNamespace = RootNamespace;
                    diff.Level = Level.Warning;
                    diffs.Add(diff);
                }
            }

            return diffs.ToArray();
        }

        private static void PrintDiffs(diff[] diffs, bool onlyErrors)
        {
            diff[] diffs2 = diffs
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
