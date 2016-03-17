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
        //public string _solutionfile { get; set; }
        public string projectFile { get; set; }
        private List<XElement> _allfiles { get; set; }

        public bool parseError { get; set; }
        public int missingfilesError { get; set; }
        public int missingfilesWarning { get; set; }
        public int excessfiles { get; set; }

        private string _formatStringError { get; set; }
        private string _formatStringWarning { get; set; }

        private static string[] excludedtags = {
            "AppDesigner", "BootstrapperPackage", "CodeAnalysisDependentAssemblyPaths", "COMReference", "Folder", "Import", "None",
            "ProjectConfiguration", "Reference", "Service", "WCFMetadata", "WCFMetadataStorage", "WebReferences", "WebReferenceUrl" };

        public Project(string projectfile, bool teamcityErrorMessage)
        {
            projectFile = projectfile;

            XDocument xdoc;
            XNamespace ns;

            try
            {
                xdoc = XDocument.Load(projectFile);
            }
            catch (IOException ex)
            {
                throw new ApplicationException("Couldn't load project: '" + projectFile + "': " + ex.Message);
            }
            catch (UnauthorizedAccessException ex)
            {
                throw new ApplicationException("Couldn't load project: '" + projectFile + "': " + ex.Message);
            }
            catch (ArgumentException ex)
            {
                throw new ApplicationException("Couldn't load project: '" + projectFile + "': " + ex.Message);
            }
            catch (System.Xml.XmlException ex)
            {
                throw new ApplicationException("Couldn't load project: '" + projectFile + "': " + ex.Message);
            }

            ns = xdoc.Root.Name.Namespace;

            try
            {
                _allfiles =
                xdoc
                    .Elements(ns + "Project")
                    .Elements(ns + "ItemGroup")
                    .Elements()
                    .Where(el => el.Attribute("Include") != null)
                    .OrderBy(el => el.Attribute("Include").Value)
                    .ToList();
            }
            catch (Exception ex)
            {
                throw new ApplicationException("Couldn't parse project: '" + projectFile + "': " + ex.Message);
            }

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
            parseError = false;
            missingfilesError = 0;
            missingfilesWarning = 0;
            excessfiles = 0;

            if (_allfiles.Count() == 0)
            {
                ConsoleHelper.WriteLine(projectFile + ": No files found in project.");
            }

            if (reverseCheck)
            {
                // Files should exist in file project file.

                string projectfolder = Path.GetDirectoryName(projectFile);

                string fullfilename = Path.Combine(projectFile);

                string[] files = Directory.GetFiles(projectfolder, "*", SearchOption.AllDirectories)
                    .Where(f => !string.Equals(f, fullfilename, StringComparison.OrdinalIgnoreCase)).ToArray();

                string[] allfiles = _allfiles.Select(el =>
                {
                    try
                    {
                        return Path.Combine(
                            Path.GetDirectoryName(projectFile),
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

                        string message = string.Format(_formatStringWarning, projectFile, filenameRelativeFromProject);
                        ConsoleHelper.WriteLineColor(message, ConsoleColor.Yellow);
                        excessfiles++;
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
                            Path.GetDirectoryName(projectFile),
                            include);
                    }
                    catch (System.ArgumentException ex)
                    {
                        ConsoleHelper.WriteLineColor(
                            "Couldn't construct file name: '" + projectFile + "' + '" + include + "': " + ex.Message,
                            ConsoleColor.Red
                            );
                        parseError = true;
                        continue;
                    }

                    if (!File.Exists(fullfilename))
                    {
                        string message = string.Format(_formatStringError, projectFile, include);
                        ConsoleHelper.WriteLineColor(message, ConsoleColor.Red);
                        missingfilesError++;
                    }
                }

                foreach (string include in _allfilesWarning)
                {
                    // Files should exist in file system.
                    string fullfilename;
                    try
                    {
                        fullfilename = Path.Combine(
                            Path.GetDirectoryName(projectFile),
                            include);
                    }
                    catch (System.ArgumentException ex)
                    {
                        ConsoleHelper.WriteLineColor(
                            "Couldn't construct file name: '" + projectFile + "' + '" + include + "': " + ex.Message,
                            ConsoleColor.Red
                            );
                        parseError = true;
                        continue;
                    }
                    if (!File.Exists(fullfilename))
                    {
                        string message = string.Format(_formatStringWarning, projectFile, include);
                        ConsoleHelper.WriteLineColor(message, ConsoleColor.Yellow);
                        missingfilesWarning++;
                    }
                }
            }
        }
    }
}
