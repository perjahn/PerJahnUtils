using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace GetMissingWarnings
{
    class Program
    {
        static void Main(string[] args)
        {
            if (args.Length != 1)
            {
                Console.WriteLine("Usage: GetMissingWarnings <path>");
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

            foreach (string file in files)
            {
                LoadProject(file);
            }
        }

        static void LoadProject(string projectfile)
        {
            XDocument xdoc;
            XNamespace ns;

            try
            {
                xdoc = XDocument.Load(projectfile);
            }
            catch (IOException ex)
            {
                Console.WriteLine("Couldn't load project: '" + projectfile + "': " + ex.Message);
                return;
            }
            catch (UnauthorizedAccessException ex)
            {
                Console.WriteLine("Couldn't load project: '" + projectfile + "': " + ex.Message);
                return;
            }
            catch (ArgumentException ex)
            {
                Console.WriteLine("Couldn't load project: '" + projectfile + "': " + ex.Message);
                return;
            }
            catch (System.Xml.XmlException ex)
            {
                Console.WriteLine("Couldn't load project: '" + projectfile + "': " + ex.Message);
                return;
            }

            ns = xdoc.Root.Name.Namespace;


            // Is there any config that is missing warnings-as-errors, but has siblings which has warnings-as-errors?

            var elements = xdoc
                .Elements(ns + "Project")
                .Elements(ns + "PropertyGroup")
                .Where(el => !el.Elements(ns + "TreatWarningsAsErrors").Any() && el.Attributes().Any() &&
                    el.Parent.Elements(ns + "PropertyGroup").Where(el2 => el2.Elements(ns + "TreatWarningsAsErrors").Any() && el2.Attributes().Any()).Any());

            if (elements.Any())
            {
                var color = Console.ForegroundColor;
                Console.ForegroundColor = ConsoleColor.Cyan;
                Console.WriteLine("********** '" + projectfile + "' " + elements.Count() + " **********");
                Console.ForegroundColor = color;

                Console.WriteLine(string.Join(Environment.NewLine, elements.Select(el => el.ToString())));
            }

            return;
        }
    }
}
