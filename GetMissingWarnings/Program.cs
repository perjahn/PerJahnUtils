using System;
using System.IO;
using System.Linq;
using System.Xml;
using System.Xml.Linq;

namespace GetMissingWarnings
{
    class Program
    {
        private static int _missingfiles;
        private static int _totalmissing;

        static void Main(string[] args)
        {
            var parsedArgs = args;

            var allenabled = parsedArgs.Contains("-a");
            parsedArgs = [.. parsedArgs.Where(a => a != "-a")];

            string[] exclude = [.. parsedArgs.Where(a => a.StartsWith("-e")).SelectMany(a => a[2..].Split(','))];
            parsedArgs = [.. parsedArgs.Where(a => !a.StartsWith("-e"))];

            if (parsedArgs.Length != 1)
            {
                Console.WriteLine(
@"Usage: GetMissingWarnings [-a] [-eProj1,Proj2] <path>

-a:  Display all (non-conditional) sections missing TreatWarningsAsErrors element.
-e:  Exclude projects.");
                return;
            }

            var path = parsedArgs[0];

            string[] files;

            try
            {
                files = Directory.GetFiles(path, "*.*proj", SearchOption.AllDirectories);
            }
            catch (DirectoryNotFoundException ex)
            {
                Console.WriteLine(ex.Message);
                return;
            }

            files = [.. files.Select(f => f.StartsWith(@".\") ? f[2..] : f)];

            string[] filteredfiles = [.. files.Where(f => !exclude.Any(f.Contains))];

            if (filteredfiles.Length < files.Length)
            {
                WriteLineColor($"Excluding: {files.Length - filteredfiles.Length} files:", ConsoleColor.Yellow);
                foreach (var file in files.Where(f => !filteredfiles.Contains(f)))
                {
                    WriteLineColor($"  '{file}'", ConsoleColor.Yellow);
                }
            }

            foreach (var file in filteredfiles)
            {
                LoadProject(file, allenabled);
            }

            Console.WriteLine();
            WriteLineColor($"Missing files: {_missingfiles}", ConsoleColor.Magenta);
            WriteLineColor($"Missing groups: {_totalmissing}", ConsoleColor.Magenta);
        }

        static void LoadProject(string projectfile, bool allenabled)
        {
            XDocument xdoc;

            try
            {
                xdoc = XDocument.Load(projectfile);
            }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or ArgumentException or XmlException)
            {
                Console.WriteLine($"Couldn't load project: '{projectfile}': {ex.Message}");
                return;
            }

            var ns = xdoc.Root.Name.Namespace;

            // Is there any config that is missing warnings-as-errors, but has siblings which has warnings-as-errors?

            if (allenabled)
            {
                XElement[] elements = [.. xdoc
                    .Elements(ns + "Project")
                    .Elements(ns + "PropertyGroup")
                    .Where(el => !el.Elements(ns + "TreatWarningsAsErrors").Any() && el.Attributes().Any())];

                if (elements.Length != 0)
                {
                    WriteLineColor($"********** '{projectfile}' {elements.Length} **********", ConsoleColor.Cyan);
                    Console.WriteLine(string.Join(Environment.NewLine, elements.Select(el => el.ToString())));
                }

                _totalmissing += elements.Length;
                if (elements.Length > 0)
                {
                    _missingfiles++;
                }
            }
            else
            {
                XElement[] elements = [.. xdoc
                    .Elements(ns + "Project")
                    .Elements(ns + "PropertyGroup")
                    .Where(el => !el.Elements(ns + "TreatWarningsAsErrors").Any() && el.Attributes().Any() &&
                        el.Parent.Elements(ns + "PropertyGroup").Any(el2 => el2.Elements(ns + "TreatWarningsAsErrors").Any() && el2.Attributes().Any()))];

                if (elements.Length != 0)
                {
                    WriteLineColor($"********** '{projectfile}' {elements.Length} **********", ConsoleColor.Cyan);
                    Console.WriteLine(string.Join(Environment.NewLine, elements.Select(el => el.ToString())));
                }

                _totalmissing += elements.Length;
                if (elements.Length != 0)
                {
                    _missingfiles++;
                }
            }
        }

        private static void WriteLineColor(string message, ConsoleColor color)
        {
            var oldcolor = Console.ForegroundColor;
            Console.ForegroundColor = color;
            try
            {
                Console.WriteLine(message);
            }
            finally
            {
                Console.ForegroundColor = oldcolor;
            }
        }
    }
}
