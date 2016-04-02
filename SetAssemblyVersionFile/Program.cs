using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace SetAssemblyVersionFile
{
    class Program
    {
        static int Main(string[] args)
        {
            int result = 0;

            if (args.Length != 2)
            {
                Log("Usage: SetAssemblyVersionFile.exe <rootfolder> <assemblyinfofile>");
                result = 1;
            }
            else
            {
                string rootfolder = args[0];
                string assemblyinfofile = args[1];

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

            Log("Current Directory: '" + Directory.GetCurrentDirectory() + "'");

            string[] projectfiles;
            try
            {
                projectfiles = Directory.GetFiles(rootfolder, "*.*proj", SearchOption.AllDirectories)
                    .Select(f => f.StartsWith(@".\") ? f.Substring(2) : f)
                    .ToArray();
            }
            catch (System.IO.DirectoryNotFoundException ex)
            {
                throw new ApplicationException(ex.Message);
            }

            Log("Project files: " + projectfiles.Length);

            foreach (string projectfile in projectfiles)
            {
                UpdateProjectFile(projectfile, assemblyinfofile);
            }
        }

        public static void UpdateProjectFile(string projectfile, string assemblyinfofile)
        {
            XDocument xdoc;
            XNamespace ns;

            try
            {
                xdoc = XDocument.Load(projectfile);
            }
            catch (System.Exception ex) when (ex is IOException || ex is UnauthorizedAccessException || ex is ArgumentException || ex is System.Xml.XmlException)
            {
                throw new ApplicationException("Couldn't load project: " + ex.Message);
            }

            ns = xdoc.Root.Name.Namespace;

            XElement[] itemGroups = xdoc
                .Descendants(ns + "ItemGroup")
                .Where(el => el.Elements(ns + "Compile").Count() > 0)
                .Select(el => el)
                .ToArray();
            if (itemGroups.Length == 0)
            {
                LogColor(projectfile + ": Ignoring project file. No suitable itemgroup found in project file.", ConsoleColor.Yellow);
                return;
            }


            string relpath = GetRelativePath(projectfile, assemblyinfofile);

            bool modified = false;

            XElement[] links = xdoc
                .Descendants(ns + "Compile")
                .Where(el => el.Attribute("Include") != null && el.Attribute("Include").Value == relpath)
                .Select(el => el)
                .ToArray();
            if (links.Length > 0)
            {
                LogColor(projectfile + ": Ignoring project file. Already contains link to specified assembly file.", ConsoleColor.DarkGray);
                return;
            }
            else
            {
                Console.Write(Dns.GetHostName() + ": ");
                LogColorFragment(projectfile, ConsoleColor.Green);
                Console.Write(": Adding link: '");
                LogColorFragment(relpath, ConsoleColor.Green);
                Console.WriteLine("'");


                XElement newlink = new XElement(ns + "Compile",
                    new XAttribute("Include", relpath),
                    new XElement(ns + "Link", @"Properties\" + Path.GetFileName(assemblyinfofile)));

                itemGroups[0].AddFirst(newlink);
                modified = true;
            }


            XElement[] compileelements = xdoc
                .Descendants(ns + "Compile")
                .Where(el => el.Attribute("Include") != null && el.Attribute("Include").Value != relpath)
                .Select(el => el)
                .ToArray();
            foreach (XElement el in compileelements)
            {
                string sourcefile = Path.Combine(Path.GetDirectoryName(projectfile), el.Attribute("Include").Value);

                if (!File.Exists(sourcefile))
                {
                    LogColor(projectfile + ": Couldn't find source file: '" + sourcefile + "'", ConsoleColor.Red);
                    continue;
                }

                string[] rows = File.ReadAllLines(sourcefile);
                List<string> newrows = new List<string>();
                bool modifiedsourcefile = false;
                foreach (string row in rows)
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
                    LogColor("Updating source file: '" + sourcefile + "'", ConsoleColor.Magenta);
                    File.WriteAllLines(sourcefile, newrows, Encoding.UTF8);
                }
            }


            XElement[] emptyelements = xdoc
                .Descendants()
                .Where(el => !el.IsEmpty && el.Value == string.Empty && el.Descendants().Count() == 0)
                .Select(el => el)
                .ToArray();
            foreach (XElement el in emptyelements)
            {
                el.Value = Environment.NewLine + string.Join(string.Empty, Enumerable.Repeat("  ", el.Ancestors().Count()));
                LogColor(projectfile + ": Fixing empty element: " + el.Name.LocalName, ConsoleColor.DarkGray);
                modified = true;
            }


            if (modified)
            {
                LogColor(projectfile + ": Saving...", ConsoleColor.Gray);
                xdoc.Save(projectfile);
            }
        }

        private static string GetRelativePath(string pathFrom, string pathTo)
        {
            string s = pathFrom;

            int pos = 0, dirs = 0;
            while (!pathTo.StartsWith(s) && s.Length > 0)
            {
                pos = s.LastIndexOf(Path.DirectorySeparatorChar);
                if (pos == -1)
                {
                    s = string.Empty;
                }
                else
                {
                    s = s.Substring(0, pos);
                }

                dirs++;
            }

            dirs--;

            return string.Join(string.Empty, Enumerable.Repeat(".." + Path.DirectorySeparatorChar, dirs).ToArray()) + pathTo.Substring(pos + 1);
        }

        private static void LogColorFragment(string message, ConsoleColor color)
        {
            ConsoleColor oldColor = Console.ForegroundColor;
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
            ConsoleColor oldColor = Console.ForegroundColor;
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
            string hostname = Dns.GetHostName();
            Console.WriteLine(hostname + ": " + message);
        }
    }
}
