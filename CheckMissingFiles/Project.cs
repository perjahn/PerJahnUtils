using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Xml.Linq;

namespace CheckMissingFiles
{
    class Project
    {
        public string _solutionfile { get; set; }
        public string _projectfilepath { get; set; }
        public List<string> _allfilesError { get; set; }
        public List<string> _allfilesWarning { get; set; }
        public int _missingfilesError { get; set; }
        public int _missingfilesWarning { get; set; }
        public bool _parseError { get; set; }

        private static string[] excludedtags = {
            "Reference", "Folder", "Import", "None", "Service", "BootstrapperPackage", "CodeAnalysisDependentAssemblyPaths",
            "COMReference", "ProjectConfiguration", "WCFMetadata", "WebReferences", "WCFMetadataStorage", "WebReferenceUrl" };

        public Project(string solutionfile, string projectfilepath)
        {
            _solutionfile = solutionfile;

            XDocument xdoc;
            XNamespace ns;

            string fullfilename = Path.Combine(Path.GetDirectoryName(solutionfile), projectfilepath);

            _projectfilepath = projectfilepath;

            try
            {
                xdoc = XDocument.Load(fullfilename);
            }
            catch (IOException ex)
            {
                throw new ApplicationException("Couldn't load project: '" + fullfilename + "': " + ex.Message);
            }
            catch (UnauthorizedAccessException ex)
            {
                throw new ApplicationException("Couldn't load project: '" + fullfilename + "': " + ex.Message);
            }
            catch (ArgumentException ex)
            {
                throw new ApplicationException("Couldn't load project: '" + fullfilename + "': " + ex.Message);
            }
            catch (System.Xml.XmlException ex)
            {
                throw new ApplicationException("Couldn't load project: '" + fullfilename + "': " + ex.Message);
            }

            ns = xdoc.Root.Name.Namespace;

            // File names are, believe it or not, percent encoded. Although space is encoded as space, not as +.

            _allfilesError =
                (from el in xdoc.Element(ns + "Project").Elements(ns + "ItemGroup").Elements()
                 where el.Attribute("Include") != null && !excludedtags.Contains(el.Name.LocalName)
                 orderby el.Attribute("Include").Value
                 select System.Uri.UnescapeDataString(el.Attribute("Include").Value))
                 .ToList();

            _allfilesWarning =
                (from el in xdoc.Element(ns + "Project").Elements(ns + "ItemGroup").Elements()
                 where el.Attribute("Include") != null && el.Name.LocalName == "None"
                 orderby el.Attribute("Include").Value
                 select System.Uri.UnescapeDataString(el.Attribute("Include").Value))
                 .ToList();


            return;
        }

        public void Check()
        {
            _parseError = false;

            foreach (string include in _allfilesError)
            {
                // Files must exist in file system.
                string fullfilename;
                try
                {
                    fullfilename = Path.Combine(
                        Path.GetDirectoryName(_solutionfile),
                        Path.GetDirectoryName(_projectfilepath),
                        include);
                }
                catch (System.ArgumentException ex)
                {
                    ConsoleHelper.WriteLineColor(
                        "Couldn't construct file name: '" + _solutionfile + "' + '" + _projectfilepath + "' + '" + include + "': " + ex.Message, ConsoleColor.Red
                        );
                    _parseError = true;
                    continue;
                }

                if (!File.Exists(fullfilename))
                {
                    ConsoleHelper.WriteLineColor(
                         "##teamcity[message text='" + _projectfilepath + " --> " + include + "' status='ERROR']",
                         ConsoleColor.Red);
                    _missingfilesError++;
                }
            }

            foreach (string include in _allfilesWarning)
            {
                // Files should exist in file system.
                string fullfilename;
                try
                {
                    fullfilename = Path.Combine(
                        Path.GetDirectoryName(_solutionfile),
                        Path.GetDirectoryName(_projectfilepath),
                        include);
                }
                catch (System.ArgumentException ex)
                {
                    ConsoleHelper.WriteLineColor(
                        "Couldn't construct file name: '" + _solutionfile + "' + '" + _projectfilepath + "' + '" + include + "': " + ex.Message, ConsoleColor.Red
                        );
                    _parseError = true;
                    continue;
                }
                if (!File.Exists(fullfilename))
                {
                    ConsoleHelper.WriteLineColor(
                         "##teamcity[message text='" + _projectfilepath + " --> " + include + "' status='WARNING']",
                         ConsoleColor.Yellow);
                    _missingfilesWarning++;
                }
            }

            return;
        }
    }
}
