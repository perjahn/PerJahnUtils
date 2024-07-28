using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Xml;
using System.Xml.Linq;

namespace SetAssemblyVersionFile
{
    class Program
    {
        static int Main(string[] args)
        {
            var result = 0;

            if (args.Length != 2)
            {
                Log("Usage: SetAssemblyVersionFile.exe <rootfolder> <assemblyinfofile>");
                result = 1;
            }
            else
            {
                var rootfolder = args[0];
                var assemblyinfofile = args[1];

                try
                {
                    UpdateProjectFiles(rootfolder, assemblyinfofile);
                }
                catch (ApplicationException ex)
                {
                    LogColor(ex.Message, ConsoleColor.Red);
                    result = 1;
                }
            }

            if (Environment.UserInteractive)
            {
                Log("Press any key to continue...");
                Console.ReadKey();
            }

            return result;
        }

        private static void UpdateProjectFiles(string rootfolder, string assemblyinfofile)
        {
            LogColor("***** Updating project files *****", ConsoleColor.Cyan);

            Log($"Current Directory: '{Directory.GetCurrentDirectory()}'");

            string[] projectfiles;
            try
            {
                projectfiles = [.. Directory.GetFiles(rootfolder, "*.*proj", SearchOption.AllDirectories)
                    .Select(f => f.StartsWith(@".\") ? f[2..] : f)];
            }
            catch (DirectoryNotFoundException ex)
            {
                throw new ApplicationException(ex.Message);
            }

            Log($"Project files: {projectfiles.Length}");

            foreach (var projectfile in projectfiles)
            {
                UpdateProjectFile(projectfile, assemblyinfofile);
            }
        }

        public static void UpdateProjectFile(string projectfile, string assemblyinfofile)
        {
            XDocument xdoc;

            try
            {
                xdoc = XDocument.Load(projectfile);
            }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or ArgumentException or XmlException)
            {
                throw new ApplicationException($"Couldn't load project: {ex.Message}");
            }

            var ns = xdoc.Root.Name.Namespace;

            XElement[] itemGroups = [.. xdoc
                .Descendants(ns + "ItemGroup")
                .Where(el => el.Elements(ns + "Compile").Any())
                .Select(el => el)];
            if (itemGroups.Length == 0)
            {
                LogColor($"{projectfile}: Ignoring project file. No suitable itemgroup found in project file.", ConsoleColor.Yellow);
                return;
            }

            var relpath = GetRelativePath(projectfile, assemblyinfofile);

            var modified = false;

            XElement[] links = [.. xdoc
                .Descendants(ns + "Compile")
                .Where(el => el.Attribute("Include") != null && el.Attribute("Include").Value == relpath)
                .Select(el => el)];
            if (links.Length > 0)
            {
                LogColor($"{projectfile}: Ignoring project file. Already contains link to specified assembly file.", ConsoleColor.DarkGray);
                return;
            }
            else
            {
                Console.Write($"{Dns.GetHostName()}: ");
                LogColorFragment(projectfile, ConsoleColor.Green);
                Console.Write(": Adding link: '");
                LogColorFragment(relpath, ConsoleColor.Green);
                Console.WriteLine("'");

                XElement newlink = new(ns + "Compile",
                    new XAttribute("Include", relpath),
                    new XElement(ns + "Link", $"Properties\\{Path.GetFileName(assemblyinfofile)}"));

                itemGroups[0].AddFirst(newlink);
                modified = true;
            }

            XElement[] compileelements = [.. xdoc
                .Descendants(ns + "Compile")
                .Where(el => el.Attribute("Include") != null && el.Attribute("Include").Value != relpath)
                .Select(el => el)];
            foreach (var el in compileelements)
            {
                var sourcefile = Path.Combine(Path.GetDirectoryName(projectfile), el.Attribute("Include").Value);

                if (!File.Exists(sourcefile))
                {
                    LogColor($"{projectfile}: Couldn't find source file: '{sourcefile}'", ConsoleColor.Red);
                    continue;
                }

                var rows = File.ReadAllLines(sourcefile);
                List<string> newrows = [];
                var modifiedsourcefile = false;
                foreach (var row in rows)
                {
                    if (row.StartsWith("[assembly: AssemblyVersion") || row.StartsWith("[assembly: AssemblyFileVersion"))
                    {
                        modifiedsourcefile = true;
                    }
                    else
                    {
                        newrows.Add(row);
                    }
                }
                if (modifiedsourcefile)
                {
                    LogColor($"Updating source file: '{sourcefile}'", ConsoleColor.Magenta);
                    File.WriteAllLines(sourcefile, newrows, Encoding.UTF8);
                }
            }

            XElement[] emptyelements = [.. xdoc
                .Descendants()
                .Where(el => !el.IsEmpty && el.Value == string.Empty && !el.Descendants().Any())
                .Select(el => el)];
            foreach (var el in emptyelements)
            {
                el.Value = $"{Environment.NewLine}{string.Join(string.Empty, Enumerable.Repeat("  ", el.Ancestors().Count()))}";
                LogColor($"{projectfile}: Fixing empty element: {el.Name.LocalName}", ConsoleColor.DarkGray);
                modified = true;
            }

            if (modified)
            {
                LogColor($"{projectfile}: Saving...", ConsoleColor.Gray);
                xdoc.Save(projectfile);
            }
        }

        private static string GetRelativePath(string pathFrom, string pathTo)
        {
            var s = pathFrom;

            int pos = 0, dirs = 0;
            while (!pathTo.StartsWith(s) && s.Length > 0)
            {
                pos = s.LastIndexOf(Path.DirectorySeparatorChar);
                s = pos == -1 ? string.Empty : s[..pos];
                dirs++;
            }

            dirs--;

            return string.Join(string.Empty, Enumerable.Repeat($"..{Path.DirectorySeparatorChar}", dirs)) + pathTo[(pos + 1)..];
        }

        private static void LogColorFragment(string message, ConsoleColor color)
        {
            var oldColor = Console.ForegroundColor;
            try
            {
                Console.ForegroundColor = color;
                Console.Write(message);
            }
            finally
            {
                Console.ForegroundColor = oldColor;
            }
        }

        private static void LogColor(string message, ConsoleColor color)
        {
            var oldColor = Console.ForegroundColor;
            try
            {
                Console.ForegroundColor = color;
                Log(message);
            }
            finally
            {
                Console.ForegroundColor = oldColor;
            }
        }

        private static void Log(string message)
        {
            var hostname = Dns.GetHostName();
            Console.WriteLine($"{hostname}: {message}");
        }
    }
}
