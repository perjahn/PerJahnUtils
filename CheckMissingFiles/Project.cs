using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Xml.Linq;

// Some tags, like AppDesigner, needs existing *folders*. Ignore such tags.
// Warn on any FS entry missing (but actually AppDesigner and None is the useful ones)

namespace CheckMissingFiles
{
    class Project
    {
        public string _solutionfile { get; set; }
        public string _projectfilepath { get; set; }
        private List<XElement> _allfiles { get; set; }
        public int _missingfilesError { get; set; }
        public int _missingfilesWarning { get; set; }
        public int _excessfiles { get; set; }
        public bool _parseError { get; set; }
        private string _formatStringError { get; set; }
        private string _formatStringWarning { get; set; }

        private static string[] excludedtags = {
            "AppDesigner", "BootstrapperPackage", "CodeAnalysisDependentAssemblyPaths", "COMReference", "Folder", "Import", "None",
            "ProjectConfiguration", "Reference", "Service", "WCFMetadata", "WCFMetadataStorage", "WebReferences", "WebReferenceUrl" };

        public Project(string solutionfile, string projectfilepath, bool teamcityErrorMessage)
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

            _allfiles =
                xdoc.Element(ns + "Project").Elements(ns + "ItemGroup").Elements()
                    .Where(el => el.Attribute("Include") != null)
                    .OrderBy(el => el.Attribute("Include").Value)
                    .ToList();

            // File names are, believe it or not, percent encoded. Although space is encoded as space, not as +.
            _allfiles
                    .ForEach(el => el.Attribute("Include").Value = System.Uri.UnescapeDataString(el.Attribute("Include").Value));


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

        public void Check(bool reverseCheck)
        {
            _parseError = false;
            _missingfilesError = 0;
            _missingfilesWarning = 0;
            _excessfiles = 0;

            if (reverseCheck)
            {
                // Files should exist in file project file.

                string projectfolder = Path.Combine(Path.GetDirectoryName(_solutionfile), Path.GetDirectoryName(_projectfilepath));

                string fullfilename = Path.Combine(Path.GetDirectoryName(_solutionfile), _projectfilepath);

                string[] files = Directory.GetFiles(projectfolder, "*", SearchOption.AllDirectories)
                    .Where(f => !string.Equals(f, fullfilename, StringComparison.OrdinalIgnoreCase)).ToArray();

                string[] allfiles = _allfiles.Select(el =>
                {
                    try
                    {
                        return Path.Combine(
                            Path.GetDirectoryName(_solutionfile),
                            Path.GetDirectoryName(_projectfilepath),
                            el.Attribute("Include").Value);
                    }
                    catch (System.ArgumentException)
                    {
                        return null;
                    }
                }).Where(f => f != null).ToArray();

                foreach (string filename in files)
                {
                    if (!allfiles.Any(f => string.Equals(f, filename, StringComparison.OrdinalIgnoreCase)))
                    {
                        string filenameRelativeFromProject = filename.Substring(projectfolder.Length).TrimStart('\\');

                        string message = string.Format(_formatStringWarning, _projectfilepath, filenameRelativeFromProject);
                        ConsoleHelper.WriteLineColor(message, ConsoleColor.Yellow);
                        _excessfiles++;
                    }
                }
            }
            else
            {
                List<string> _allfilesError = _allfiles
                    .Where(el => !excludedtags.Contains(el.Name.LocalName))
                    .Select(el => el.Attribute("Include").Value)
                    .ToList();
                List<string> _allfilesWarning = _allfiles
                    .Where(el => el.Name.LocalName == "None")
                    .Select(el => el.Attribute("Include").Value)
                    .ToList();


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
            }
        }
    }
}
