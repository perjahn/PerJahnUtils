using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
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
            XNamespace ns;

            try
            {
                xdoc = XDocument.Load(ProjectFile);
            }
            catch (System.Exception ex) when (ex is IOException || ex is UnauthorizedAccessException || ex is ArgumentException || ex is System.Xml.XmlException)
            {
                string message =
                    teamcityErrorMessage ?
                        string.Format(
                            "##teamcity[message text='Could not load project: {0}' status='ERROR']",
                            ex.Message.Replace("\'", "")) :
                        string.Format(
                            "Couldn't load project: '{0}'",
                            ex.Message);

                throw new ApplicationException(message);
            }

            ns = xdoc.Root.Name.Namespace;

            References =
                xdoc.Elements(ns + "Project").Elements(ns + "ItemGroup").Elements(ns + "Reference")
                    .Where(el => el.Attribute("Include") != null && el.Elements(ns + "HintPath").Count() >= 1)
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
                    })
                    .ToList();
        }

        public string GetRelativePath(string pathFrom, string pathTo)
        {
            string s = pathFrom;

            int pos = 0, dirs = 0;
            while (!pathTo.StartsWith(s) && s.Length > 0)
            {
                pos = s.LastIndexOf(Path.DirectorySeparatorChar);
                if (pos == -1)
                {
                    s = string.Empty;
                }
                else
                {
                    s = s.Substring(0, pos);
                }

                dirs++;
            }

            dirs--;

            return string.Join(string.Empty, Enumerable.Repeat($"..{Path.DirectorySeparatorChar}", dirs).ToArray()) + pathTo.Substring(pos + 1);
        }

        // Remove unnecessary .. from path
        public static string CompactPath(string path)
        {
            List<string> folders = path.Split(Path.DirectorySeparatorChar).ToList();

            for (int i = 0; i < folders.Count;)
            {
                if (i > 0 && folders[i] == ".." && folders[i - 1] != ".." && folders[i - 1] != "")
                {
                    folders.RemoveAt(i - 1);
                    folders.RemoveAt(i - 1);
                    i--;
                }
                else if (i > 0 && folders[i] == "" && folders[i - 1] == "")
                {
                    folders.RemoveAt(i);
                }
                else
                {
                    i++;
                }
            }

            string path2 = string.Join(Path.DirectorySeparatorChar.ToString(), folders.ToArray());

            string sep = Path.DirectorySeparatorChar.ToString();
            if (path2 == "" && (path.StartsWith(sep) || path.EndsWith(sep)))
            {
                path2 = Path.DirectorySeparatorChar.ToString();
            }

            return path2;
        }

    }
}
