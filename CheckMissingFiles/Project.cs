using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml.Linq;

// Some tags, like AppDesigner, needs existing *folders*. Ignore such tags.
// Warn on any FS entry missing (but actually AppDesigner and None is the useful ones)

namespace CheckMissingFiles
{
    class Project
    {
        public string ProjectFile { get; set; }
        public string[] SolutionFiles { get; set; }
        private List<XElement> _allfiles { get; set; }

        public bool ParseError { get; set; }
        public int MissingfilesError { get; set; }
        public int MissingfilesWarning { get; set; }
        public int Excessfiles { get; set; }

        private bool _teamcityErrorMessage { get; set; }

        private static string[] excludedelements = {
            "AppDesigner", "BootstrapperPackage", "CodeAnalysisDependentAssemblyPaths", "COMReference", "DnxInvisibleContent",
            "DotNetCliToolReference", "Folder", "Import", "None", "PackageReference", "ProjectConfiguration", "Reference",
            "Service", "WCFMetadata", "WCFMetadataStorage", "WebReferences", "WebReferenceUrl" };

        public Project(string projectfile, string[] solutionfiles, bool teamcityErrorMessage)
        {
            ProjectFile = projectfile;
            SolutionFiles = solutionfiles;

            _teamcityErrorMessage = teamcityErrorMessage;

            XDocument xdoc;
            XNamespace ns;

            try
            {
                xdoc = XDocument.Load(ProjectFile);
            }
            catch (Exception ex) when (ex is IOException || ex is UnauthorizedAccessException || ex is ArgumentException || ex is System.Xml.XmlException)
            {
                string message =
                    teamcityErrorMessage ?
                        $"##teamcity[message text='Could not load project: {string.Join(", ", solutionfiles)} --> {ex.Message.Replace("\'", "")}' status='ERROR']" :
                        $"Couldn't load project: '{string.Join("', '", solutionfiles)}' --> '{ex.Message}'";

                throw new ApplicationException(message);
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
                throw new ApplicationException($"Couldn't parse project: '{ProjectFile}': {ex.Message}");
            }

            // File names are, believe it or not, percent encoded. Although space is encoded as space, not as +.
            _allfiles
                .ForEach(el => el.Attribute("Include").Value = Uri.UnescapeDataString(el.Attribute("Include").Value));


            return;
        }

        public void Check(bool reverseCheck)
        {
            string formatStringError;
            string formatStringWarning;

            if (_teamcityErrorMessage)
            {
                formatStringError = "##teamcity[message text='File not found: {0} --> {1}' status='ERROR']";
                formatStringWarning = "##teamcity[message text='File not found: {0} --> {1}' status='WARNING']";
            }
            else
            {
                formatStringError = "File not found: '{0}' --> '{1}'";
                formatStringWarning = "File not found: '{0}' --> '{1}'";
            }


            ParseError = false;
            MissingfilesError = 0;
            MissingfilesWarning = 0;
            Excessfiles = 0;

            if (_allfiles.Count() == 0)
            {
                ConsoleHelper.WriteLine($"No files found in project: '{string.Join("', '", SolutionFiles)}': '{ProjectFile}'");
            }

            if (reverseCheck)
            {
                // Files should exist in project file.

                string projectfolder = Path.GetDirectoryName(ProjectFile);

                string[] files = Directory.GetFiles(projectfolder, "*", SearchOption.AllDirectories)
                    .Where(f => !string.Equals(f, ProjectFile, StringComparison.OrdinalIgnoreCase)).ToArray();

                string[] allfiles = _allfiles.Select(el =>
                {
                    try
                    {
                        return Path.Combine(
                            Path.GetDirectoryName(ProjectFile),
                            el.Attribute("Include").Value);
                    }
                    catch (ArgumentException)
                    {
                        return null;
                    }
                }).Where(f => f != null).ToArray();

                foreach (string filename in files)
                {
                    if (!allfiles.Any(f => string.Equals(f, filename, StringComparison.OrdinalIgnoreCase)))
                    {
                        string filenameRelativeFromProject = filename.Substring(projectfolder.Length).TrimStart('\\');

                        string message = string.Format(formatStringWarning, ProjectFile, filenameRelativeFromProject);
                        ConsoleHelper.WriteLineColor(message, ConsoleColor.Yellow);
                        Excessfiles++;
                    }
                }
            }
            else
            {
                List<string> _allfilesError = _allfiles
                    .Where(el => !excludedelements.Contains(el.Name.LocalName))
                    .Select(el => el.Attribute("Include").Value)
                    .ToList();
                List<string> _allfilesWarning = _allfiles
                    .Where(el => el.Name.LocalName == "None")
                    .Select(el => el.Attribute("Include").Value)
                    .ToList();


                foreach (string include in _allfilesError)
                {
                    // Files must exist in file system.
                    string[] files;
                    try
                    {
                        files = Directory.GetFiles(Path.GetDirectoryName(ProjectFile), include);
                    }
                    catch (DirectoryNotFoundException)
                    {
                        // Suppress exception message
                        files = new string[] { };
                    }
                    catch (Exception ex) when (ex is ArgumentException || ex is IOException || ex is UnauthorizedAccessException)
                    {
                        ConsoleHelper.WriteLineColor(
                            $"Couldn't get files: '{string.Join("', '", SolutionFiles)}': '{ProjectFile}' + '{include}': {ex.Message}",
                            ConsoleColor.Red);
                        ParseError = true;
                        continue;
                    }

                    if (files.Length == 0)
                    {
                        string message = string.Format(formatStringError, ProjectFile, include);
                        ConsoleHelper.WriteLineColor(message, ConsoleColor.Red);
                        MissingfilesError++;
                    }
                }

                foreach (string include in _allfilesWarning)
                {
                    // Files should exist in file system.
                    string[] files;
                    try
                    {
                        files = Directory.GetFiles(Path.GetDirectoryName(ProjectFile), include);
                    }
                    catch (DirectoryNotFoundException)
                    {
                        // Suppress exception message
                        files = new string[] { };
                    }
                    catch (Exception ex) when (ex is ArgumentException || ex is IOException || ex is UnauthorizedAccessException)
                    {
                        ConsoleHelper.WriteLineColor(
                            $"Couldn't get files: '{string.Join("', '", SolutionFiles)}': '{ProjectFile}' + '{include}': {ex.Message}",
                            ConsoleColor.Red);
                        ParseError = true;
                        continue;
                    }
                    if (files.Length == 0)
                    {
                        string message = string.Format(formatStringWarning, ProjectFile, include);
                        ConsoleHelper.WriteLineColor(message, ConsoleColor.Yellow);
                        MissingfilesWarning++;
                    }
                }
            }
        }
    }
}
