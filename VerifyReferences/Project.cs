using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml;
using System.Xml.Linq;

namespace VerifyReferences
{
    class Project
    {
        public string ProjectFile { get; set; }
        public List<Reference> References { get; set; }

        public Project(string projectpath, bool teamcityErrorMessage)
        {
            ProjectFile = projectpath;

            XDocument xdoc;

            try
            {
                xdoc = XDocument.Load(ProjectFile);
            }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or ArgumentException or XmlException)
            {
                var message =
                    teamcityErrorMessage ?
                        string.Format(
                            "##teamcity[message text='Could not load project: {0}' status='ERROR']",
                            ex.Message.Replace("\'", string.Empty)) :
                        string.Format(
                            "Couldn't load project: '{0}'",
                            ex.Message);

                throw new ApplicationException(message);
            }

            var ns = xdoc.Root.Name.Namespace;

            References = [.. xdoc
                .Elements(ns + "Project").Elements(ns + "ItemGroup").Elements(ns + "Reference")
                .Where(el => el.Attribute("Include") != null && el.Elements(ns + "HintPath").Any())
                .OrderBy(el => el.Attribute("Include").Value)
                .Select(el => new Reference
                {
                    Include = el.Attribute("Include").Value,
                    Shortinclude = el.Attribute("Include").Value.Split(',')[0],
                    Hintpath = el.Elements(ns + "HintPath")
                        .OrderBy(elHintPath => elHintPath.Value)
                        .First()
                        .Value,
                    Path = CompactPath(Path.Combine(Path.GetDirectoryName(projectpath), el.Elements(ns + "HintPath")
                        .OrderBy(elHintPath => elHintPath.Value)
                        .First()
                        .Value))
                })];
        }

        public string GetRelativePath(string pathFrom, string pathTo)
        {
            var s = pathFrom;

            int pos = 0, dirs = 0;
            while (!pathTo.StartsWith(s) && s.Length > 0)
            {
                pos = s.LastIndexOf(Path.DirectorySeparatorChar);
                s = pos == -1 ? string.Empty : s[..pos];
                dirs++;
            }

            dirs--;

            return string.Join(string.Empty, Enumerable.Repeat($"..{Path.DirectorySeparatorChar}", dirs)) + pathTo[(pos + 1)..];
        }

        // Remove unnecessary .. from path
        public static string CompactPath(string path)
        {
            List<string> folders = [.. path.Split(Path.DirectorySeparatorChar)];

            for (var i = 0; i < folders.Count;)
            {
                if (i > 0 && folders[i] == ".." && folders[i - 1] != ".." && folders[i - 1] != string.Empty)
                {
                    folders.RemoveAt(i - 1);
                    folders.RemoveAt(i - 1);
                    i--;
                }
                else if (i > 0 && folders[i] == string.Empty && folders[i - 1] == string.Empty)
                {
                    folders.RemoveAt(i);
                }
                else
                {
                    i++;
                }
            }

            var path2 = string.Join(Path.DirectorySeparatorChar.ToString(), folders);

            var sep = Path.DirectorySeparatorChar.ToString();
            if (path2 == string.Empty && (path.StartsWith(sep) || path.EndsWith(sep)))
            {
                path2 = Path.DirectorySeparatorChar.ToString();
            }

            return path2;
        }
    }
}
