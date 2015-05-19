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
        public string _projectfilepath { get; set; }
        public List<string> _allfiles { get; set; }
        public int _missingfiles { get; set; }

        private static string[] excludedtags = {
			"Reference", "Folder", "Import", "None", "Service", "BootstrapperPackage", "CodeAnalysisDependentAssemblyPaths",
            "COMReference", "WCFMetadata", "WebReferences", "WCFMetadataStorage", "WebReferenceUrl" };

        public static Project LoadProject(string solutionfile, string projectfilepath)
        {
            Project newproj = new Project();
            XDocument xdoc;
            XNamespace ns;

            string fullfilename = Path.Combine(Path.GetDirectoryName(solutionfile), projectfilepath);

            newproj._projectfilepath = projectfilepath;

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

            newproj._allfiles =
                (from el in xdoc.Element(ns + "Project").Elements(ns + "ItemGroup").Elements()
                 where el.Attribute("Include") != null && !excludedtags.Contains(el.Name.LocalName)
                 orderby el.Attribute("Include").Value
                 select System.Uri.UnescapeDataString(el.Attribute("Include").Value))
                 .ToList();


            return newproj;
        }

        public void Check(string solutionfile)
        {
            foreach (string include in _allfiles)
            {
                // Files must exist in file system.
                string fullfilename = Path.Combine(
                    Path.GetDirectoryName(solutionfile),
                    Path.GetDirectoryName(_projectfilepath),
                    include);
                if (!File.Exists(fullfilename))
                {
                    //ConsoleHelper.WriteLineColor(
                    //     "'" + _projectfilepath + "' --> '" + include + "'",
                    //     ConsoleColor.Red);
                    ConsoleHelper.WriteLineColor(
                         "##teamcity[message text='" + _projectfilepath + " --> " + include + "' status='ERROR']",
                         ConsoleColor.Red);
                    _missingfiles++;
                }
            }

            return;
        }
    }
}
