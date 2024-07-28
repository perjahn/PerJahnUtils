using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml;
using System.Xml.Linq;

namespace RemoveMissingFiles
{
    class Project
    {
        public string _sln_package { get; set; }
        public string _sln_path { get; set; }
        public List<string> _allfiles { get; set; }
        public int _removedfiles { get; set; }

        private static string[] excludedtags = [
            "Reference", "Folder", "Import", "Service", "BootstrapperPackage", "CodeAnalysisDependentAssemblyPaths",
            "COMReference", "WCFMetadata", "WebReferences", "WCFMetadataStorage", "WebReferenceUrl" ];

        public static Project LoadProject(string solutionfile, string projectfilepath)
        {
            Project newproj = new();
            XDocument xdoc;

            var fullfilename = Path.Combine(Path.GetDirectoryName(solutionfile), projectfilepath);

            try
            {
                xdoc = XDocument.Load(fullfilename);
            }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or ArgumentException or XmlException)
            {
                throw new ApplicationException($"Couldn't load project: '{fullfilename}': {ex.Message}");
            }

            var ns = xdoc.Root.Name.Namespace;

            // File names are, believe it or not, percent encoded. Although space is encoded as space, not as +.

            newproj._allfiles = [.. xdoc
                .Element(ns + "Project").Elements(ns + "ItemGroup").Elements()
                .Where(el => el.Attribute("Include") != null && !excludedtags.Contains(el.Name.LocalName))
                .OrderBy(el => el.Attribute("Include").Value)
                .Select(el => Uri.UnescapeDataString(el.Attribute("Include").Value))];

            return newproj;
        }

        public void Fix(string solutionfile)
        {
            List<string> existingfiles = [];

            foreach (var include in _allfiles)
            {
                // Files must exist in file system.
                var fullfilename = Path.Combine(Path.GetDirectoryName(solutionfile), Path.GetDirectoryName(_sln_path), include);
                if (!File.Exists(fullfilename))
                {
                    ConsoleHelper.WriteLineColor($"'{_sln_path}' --> '{include}'", ConsoleColor.Red);
                    _removedfiles++;
                }
                else
                {
                    existingfiles.Add(include);
                }
            }

            _allfiles = existingfiles;
        }

        public void WriteProject(string solutionfile, bool simulate)
        {
            XDocument xdoc;
            var fullfilename = Path.Combine(Path.GetDirectoryName(solutionfile), _sln_path);

            try
            {
                xdoc = XDocument.Load(fullfilename);
            }
            catch (Exception ex) when (ex is IOException or XmlException)
            {
                ConsoleHelper.WriteLine($"Couldn't load project: '{fullfilename}': {ex.Message}");
                return;
            }

            UpdateFiles(xdoc);

            ConsoleHelper.WriteLine($"Writing file: '{fullfilename}'.");
            if (!simulate)
            {
                FileHelper.RemoveRO(fullfilename);
                xdoc.Save(fullfilename);
            }
        }

        // Todo: check case sensitivity
        public void UpdateFiles(XDocument xdoc)
        {
            var ns = xdoc.Root.Name.Namespace;

            List<XElement> fileitems = [.. xdoc
                .Element(ns + "Project").Elements(ns + "ItemGroup").Elements()
                .Where(el => el.Attribute("Include") != null && !excludedtags.Contains(el.Name.LocalName))
                .OrderBy(el => el.Attribute("Include").Value)];

            foreach (var fileitem in fileitems)
            {
                var filename = Uri.UnescapeDataString(fileitem.Attribute("Include").Value);

                if (!_allfiles.Contains(filename))
                {
                    //Console.WriteLine($"Removing file: '{filename}'");
                    fileitem.Remove();
                }
            }
        }
    }
}
