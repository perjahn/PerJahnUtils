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
        public int _existingfiles { get; set; }
        public bool _parseError { get; set; }
        private string _formatStringError { get; set; }
        private string _formatStringWarning { get; set; }
        private bool _reverseCheck { get; set; }

        private static string[] excludedtags = {
            "Reference", "Folder", "Import", "None", "Service", "BootstrapperPackage", "CodeAnalysisDependentAssemblyPaths",
            "COMReference", "ProjectConfiguration", "WCFMetadata", "WebReferences", "WCFMetadataStorage", "WebReferenceUrl" };

        public Project(string solutionfile, string projectfilepath, bool teamcityErrorMessage, bool reverseCheck)
        {
            _solutionfile = solutionfile;
            _reverseCheck = reverseCheck;


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

            if (_reverseCheck)
            {
                _allfilesError = new List<string>();

                _allfilesWarning =
                    (from el in xdoc.Element(ns + "Project").Elements(ns + "ItemGroup").Elements()
                     where el.Attribute("Include") != null
                     orderby el.Attribute("Include").Value
                     select System.Uri.UnescapeDataString(el.Attribute("Include").Value))
                     .ToList();
            }
            else
            {
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
            }

            if (teamcityErrorMessage)
            {
                _formatStringError = "##teamcity[message text='{0} --> {1}' status='ERROR']";
                _formatStringWarning = "##teamcity[message text='{0} --> {1}' status='WARNING']";
            }
            else
            {
                _formatStringError = "'{0}' --> '{1}'";
                _formatStringWarning = "'{0}' --> '{1}'";
            }

            return;
        }

        public void Check()
        {
            if (_reverseCheck)
            {
                ReverseCheck();
                return;
            }

            _parseError = false;
            _missingfilesError = 0;
            _missingfilesWarning = 0;
            _existingfiles = 0;

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
                        "Couldn't construct file name: '" + _solutionfile + "' + '" + _projectfilepath + "' + '" + include + "': " + ex.Message,
                        ConsoleColor.Red
                        );
                    _parseError = true;
                    continue;
                }

                if (!File.Exists(fullfilename))
                {
                    string message = string.Format(_formatStringError, _projectfilepath, include);
                    ConsoleHelper.WriteLineColor(message, ConsoleColor.Red);
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
                        "Couldn't construct file name: '" + _solutionfile + "' + '" + _projectfilepath + "' + '" + include + "': " + ex.Message,
                        ConsoleColor.Red
                        );
                    _parseError = true;
                    continue;
                }
                if (!File.Exists(fullfilename))
                {
                    string message = string.Format(_formatStringWarning, _projectfilepath, include);
                    ConsoleHelper.WriteLineColor(message, ConsoleColor.Yellow);
                    _missingfilesWarning++;
                }
            }

            return;
        }

        public void ReverseCheck()
        {
            // Files should exist in file project file.

            _missingfilesError = 0;
            _missingfilesWarning = 0;
            _existingfiles = 0;

            string projectfolder = Path.Combine(Path.GetDirectoryName(_solutionfile), Path.GetDirectoryName(_projectfilepath));

            string fullfilename = Path.Combine(Path.GetDirectoryName(_solutionfile), _projectfilepath);

            string[] files = Directory.GetFiles(projectfolder, "*", SearchOption.AllDirectories)
                .Where(f => !string.Equals(f, fullfilename, StringComparison.OrdinalIgnoreCase)).ToArray();


            string[] allfilesinproject = _allfilesWarning.Select(f =>
            {
                try
                {
                    return Path.Combine(
                        Path.GetDirectoryName(_solutionfile),
                        Path.GetDirectoryName(_projectfilepath),
                        f);
                }
                catch (System.ArgumentException)
                {
                    return null;
                }
            }).Where(f => f != null).ToArray();

            foreach (string filename in files)
            {
                if (!allfilesinproject.Any(f => string.Equals(f, filename, StringComparison.OrdinalIgnoreCase)))
                {
                    string filenameRelativeFromProject = filename.Substring(projectfolder.Length).TrimStart('\\');

                    string message = string.Format(_formatStringWarning, _projectfilepath, filenameRelativeFromProject);
                    ConsoleHelper.WriteLineColor(message, ConsoleColor.Yellow);
                    _missingfilesWarning++;
                }
                else
                {
                    _existingfiles++;
                }
            }

            return;
        }
    }
}
