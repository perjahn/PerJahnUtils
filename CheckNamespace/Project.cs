using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Xml;
using System.Xml.Linq;

namespace CheckNamespace
{
    class Project
    {
        public string _solutionfile { get; set; }
        public string _sln_path { get; set; }
        public string _rootnamespace { get; set; }
        public List<string> _allfiles { get; set; }

        private static string[] excludedtags = [
            "Reference", "Folder", "Service", "BootstrapperPackage", "CodeAnalysisDependentAssemblyPaths",
            "WCFMetadata", "WebReferences", "WCFMetadataStorage", "WebReferenceUrl" ];

        public Project(string solutionfile, string projectfilepath)
        {
            _solutionfile = solutionfile;
            _sln_path = projectfilepath;

            XDocument xdoc;

            var fullfilename = Path.Combine(Path.GetDirectoryName(solutionfile), projectfilepath);

            try
            {
                xdoc = XDocument.Load(fullfilename);
            }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or ArgumentException or XmlException)
            {
                throw new ApplicationException($"Couldn't load project: '{Path.GetFileName(solutionfile)}/{Path.GetFileName(fullfilename)}': {ex.Message}", ex);
            }

            var ns = xdoc.Root.Name.Namespace;

            // File names are, believe it or not, percent encoded. Although space is encoded as space, not as +.

            string[] namespaces = [.. xdoc
                .Element(ns + "Project").Elements(ns + "PropertyGroup").Elements(ns + "RootNamespace")
                .Select(el => el.Value)];

            var count = namespaces.Length;
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

            _allfiles = [.. xdoc
                .Element(ns + "Project").Elements(ns + "ItemGroup").Elements()
                .Where(el => el.Attribute("Include") != null && !excludedtags.Contains(el.Name.LocalName))
                .OrderBy(el => el.Attribute("Include").Value)
                .Select(el => Uri.UnescapeDataString(el.Attribute("Include").Value))];
        }

        public int CheckNamespace()
        {
            var failcount = 0;

            foreach (var filename in _allfiles.Where(f => string.Compare(Path.GetExtension(f), ".cs", true) == 0))
            {
                // Files must exist in file system.
                var fullfilename = Path.Combine(Path.GetDirectoryName(_solutionfile), Path.GetDirectoryName(_sln_path), filename);
                if (!File.Exists(fullfilename))
                {
                    ConsoleHelper.WriteLineColor($"File not found: Project path: '{_sln_path}', File path: '{filename}'.", ConsoleColor.Red);
                    return 0;
                }

                var rows = File.ReadAllLines(fullfilename);
                var rownum = 1;
                foreach (var row in rows)
                {
                    if (row.TrimStart().StartsWith("namespace"))
                    {
                        var ns = row.TrimStart()[9..].TrimStart();
                        var index = ns.IndexOfAny([' ', '\t']);
                        if (index != -1)
                        {
                            ns = ns[..index];
                        }
                        if (ns != _rootnamespace && !ns.StartsWith($"{_rootnamespace}."))
                        {
                            var commonns = GetCommonString(_rootnamespace, ns);

                            Console.Write(Path.GetDirectoryName(fullfilename) + Path.DirectorySeparatorChar);
                            ConsoleHelper.WriteColor(Path.GetFileName(fullfilename), ConsoleColor.White);
                            Console.Write($"' ({rownum}): '");

                            Console.Write(commonns);
                            ConsoleHelper.WriteColor(_rootnamespace[commonns.Length..], ConsoleColor.Magenta);

                            Console.Write("' <-> '");

                            Console.Write(commonns);
                            ConsoleHelper.WriteColor(ns[commonns.Length..], ConsoleColor.Magenta);

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
            StringBuilder sb = new();

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
