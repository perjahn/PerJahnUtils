using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml.Linq;

namespace GetMissingWarnings
{
    class Program
    {
        private static int _missingfiles = 0;
        private static int _totalmissing = 0;

        static void Main(string[] args)
        {
            bool allenabled = args.Contains("-a");
            args = args.Where(a => a != "-a").ToArray();

            string[] exclude = args.Where(a => a.StartsWith("-e")).SelectMany(a => a.Substring(2).Split(',')).ToArray();
            args = args.Where(a => !a.StartsWith("-e")).ToArray();

            if (args.Length != 1)
            {
                Console.WriteLine(
@"Usage: GetMissingWarnings [-a] [-eProj1,Proj2] <path>

-a:  Display all (non-conditional) sections missing TreatWarningsAsErrors element.
-e:  Exclude projects.");
                return;
            }

            string path = args[0];

            string[] files = null;

            try
            {
                files = Directory.GetFiles(path, "*.*proj", SearchOption.AllDirectories);
            }
            catch (DirectoryNotFoundException ex)
            {
                Console.WriteLine(ex.Message);
                return;
            }

            files = files
                .Select(f => f.StartsWith(@".\") ? f.Substring(2) : f)
                .ToArray();

            string[] filteredfiles = files.Where(f => !exclude.Any(f.Contains)).ToArray();

            if (filteredfiles.Length < files.Length)
            {
                WriteLineColor($"Excluding: {(files.Length - filteredfiles.Length)} files:", ConsoleColor.Yellow);
                foreach (string file in files.Where(f => !filteredfiles.Contains(f)))
                {
                    WriteLineColor($"  '{file}'", ConsoleColor.Yellow);
                }
            }


            foreach (string file in filteredfiles)
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
            XNamespace ns;

            try
            {
                xdoc = XDocument.Load(projectfile);
            }
            catch (IOException ex)
            {
                Console.WriteLine($"Couldn't load project: '{projectfile}': {ex.Message}");
                return;
            }
            catch (UnauthorizedAccessException ex)
            {
                Console.WriteLine($"Couldn't load project: '{projectfile}': {ex.Message}");
                return;
            }
            catch (ArgumentException ex)
            {
                Console.WriteLine($"Couldn't load project: '{projectfile}': {ex.Message}");
                return;
            }
            catch (System.Xml.XmlException ex)
            {
                Console.WriteLine($"Couldn't load project: '{projectfile}': {ex.Message}");
                return;
            }

            ns = xdoc.Root.Name.Namespace;


            // Is there any config that is missing warnings-as-errors, but has siblings which has warnings-as-errors?

            if (allenabled)
            {
                var elements = xdoc
                    .Elements(ns + "Project")
                    .Elements(ns + "PropertyGroup")
                    .Where(el => !el.Elements(ns + "TreatWarningsAsErrors").Any() && el.Attributes().Any());

                if (elements.Any())
                {
                    WriteLineColor($"********** '{projectfile}' {elements.Count()} **********", ConsoleColor.Cyan);

                    Console.WriteLine(string.Join(Environment.NewLine, elements.Select(el => el.ToString())));
                }

                _totalmissing += elements.Count();
                if (elements.Count() > 0)
                {
                    _missingfiles++;
                }
            }
            else
            {
                var elements = xdoc
                    .Elements(ns + "Project")
                    .Elements(ns + "PropertyGroup")
                    .Where(el => !el.Elements(ns + "TreatWarningsAsErrors").Any() && el.Attributes().Any() &&
                        el.Parent.Elements(ns + "PropertyGroup").Where(el2 => el2.Elements(ns + "TreatWarningsAsErrors").Any() && el2.Attributes().Any()).Any());

                if (elements.Any())
                {
                    WriteLineColor($"********** '{projectfile}' {elements.Count()} **********", ConsoleColor.Cyan);

                    Console.WriteLine(string.Join(Environment.NewLine, elements.Select(el => el.ToString())));
                }

                _totalmissing += elements.Count();
                if (elements.Count() > 0)
                {
                    _missingfiles++;
                }
            }

            return;
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
