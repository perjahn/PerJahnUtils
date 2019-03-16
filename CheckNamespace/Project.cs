using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Xml.Linq;

namespace CheckNamespace
{
    class Project
    {
        public string _solutionfile { get; set; }
        public string _sln_path { get; set; }
        public string _rootnamespace { get; set; }
        public List<string> _allfiles { get; set; }

        private static string[] excludedtags = {
            "Reference", "Folder", "Service", "BootstrapperPackage", "CodeAnalysisDependentAssemblyPaths",
            "WCFMetadata", "WebReferences", "WCFMetadataStorage", "WebReferenceUrl" };

        public Project(string solutionfile, string projectfilepath)
        {
            _solutionfile = solutionfile;
            _sln_path = projectfilepath;

            XDocument xdoc;
            XNamespace ns;

            string fullfilename = Path.Combine(Path.GetDirectoryName(solutionfile), projectfilepath);

            try
            {
                xdoc = XDocument.Load(fullfilename);
            }
            catch (IOException ex)
            {
                throw new ApplicationException($"Couldn't load project: '{Path.GetFileName(solutionfile)}/{Path.GetFileName(fullfilename)}': {ex.Message}", ex);
            }
            catch (UnauthorizedAccessException ex)
            {
                throw new ApplicationException($"Couldn't load project: '{Path.GetFileName(solutionfile)}/{Path.GetFileName(fullfilename)}': {ex.Message}", ex);
            }
            catch (ArgumentException ex)
            {
                throw new ApplicationException($"Couldn't load project: '{Path.GetFileName(solutionfile)}/{Path.GetFileName(fullfilename)}': {ex.Message}", ex);
            }
            catch (System.Xml.XmlException ex)
            {
                throw new ApplicationException($"Couldn't load project: '{Path.GetFileName(solutionfile)}/{Path.GetFileName(fullfilename)}': {ex.Message}", ex);
            }

            ns = xdoc.Root.Name.Namespace;

            // File names are, believe it or not, percent encoded. Although space is encoded as space, not as +.

            IEnumerable<string> namespaces =
                from el in xdoc.Element(ns + "Project").Elements(ns + "PropertyGroup").Elements(ns + "RootNamespace")
                select el.Value;

            int count = namespaces.Count();
            if (count < 1)
            {
                ConsoleHelper.WriteLineColor($"{projectfilepath}: No RootNamespace found.", ConsoleColor.Red);
            }
            else if (count > 1)
            {
                ConsoleHelper.WriteLineColor($"{projectfilepath}: {count} RootNamespace found.", ConsoleColor.Red);
            }
            else
            {
                _rootnamespace = namespaces.Single();
            }

            _allfiles =
                (from el in xdoc.Element(ns + "Project").Elements(ns + "ItemGroup").Elements()
                 where el.Attribute("Include") != null && !excludedtags.Contains(el.Name.LocalName)
                 orderby el.Attribute("Include").Value
                 select System.Uri.UnescapeDataString(el.Attribute("Include").Value))
                 .ToList();
        }

        public int CheckNamespace()
        {
            int failcount = 0;

            foreach (string filename in _allfiles.Where(f => Path.GetExtension(f).ToLower() == ".cs"))
            {
                // Files must exist in file system.
                string fullfilename = Path.Combine(
                    Path.GetDirectoryName(_solutionfile),
                    Path.GetDirectoryName(_sln_path),
                    filename);
                if (!File.Exists(fullfilename))
                {
                    ConsoleHelper.WriteLineColor($"File not found: Project path: '{_sln_path}', File path: '{filename}'.", ConsoleColor.Red);
                    return 0;
                }

                string[] rows = File.ReadAllLines(fullfilename);
                int rownum = 1;
                foreach (string row in rows)
                {
                    if (row.TrimStart().StartsWith("namespace"))
                    {
                        string ns = row.TrimStart().Substring(9).TrimStart();
                        int index = ns.IndexOfAny(new char[] { ' ', '\t' });
                        if (index != -1)
                        {
                            ns = ns.Substring(0, index);
                        }
                        if (ns != _rootnamespace && !ns.StartsWith($"{_rootnamespace}."))
                        {
                            string commonns = GetCommonString(_rootnamespace, ns);

                            Console.Write(Path.GetDirectoryName(fullfilename) + Path.DirectorySeparatorChar);
                            ConsoleHelper.WriteColor(Path.GetFileName(fullfilename), ConsoleColor.White);
                            Console.Write($"' ({rownum}): '");

                            Console.Write(commonns);
                            ConsoleHelper.WriteColor(_rootnamespace.Substring(commonns.Length), ConsoleColor.Magenta);

                            Console.Write("' <-> '");

                            Console.Write(commonns);
                            ConsoleHelper.WriteColor(ns.Substring(commonns.Length), ConsoleColor.Magenta);

                            Console.WriteLine("'");
                            failcount++;
                        }
                    }
                    rownum++;
                }
            }

            return failcount;
        }

        private string GetCommonString(string a, string b)
        {
            int i, j;
            var sb = new StringBuilder();

            for (i = j = 0; i < a.Length && j < b.Length; i++, j++)
            {
                if (a[i] != b[j])
                {
                    return sb.ToString();
                }
                sb.Append(a[i]);
            }

            return sb.ToString();
        }
    }
}
