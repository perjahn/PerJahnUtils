using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml;
using System.Xml.Linq;

namespace CreatePublish
{
    class Project
    {
        public string _sln_shortfilename { get; set; }
        public string _sln_path { get; set; }

        public List<string> _ProjectTypeGuids { get; set; }


        public static Project LoadProject(string solutionfile, string projectfilepath)
        {
            Project newproj = new Project();
            XDocument xdoc;
            XNamespace ns;

            string fullfilename = Path.Combine(Path.GetDirectoryName(solutionfile), projectfilepath);

            try
            {
                xdoc = XDocument.Load(fullfilename);
            }
            catch (Exception ex) when (ex is IOException || ex is UnauthorizedAccessException || ex is ArgumentException || ex is XmlException)
            {
                ConsoleHelper.ColorWrite(ConsoleColor.Red, $"Couldn't load project: '{fullfilename}': {ex.Message}");
                return null;
            }

            ns = xdoc.Root.Name.Namespace;



            IEnumerable<string[]> guidsarr =
                from el
                in xdoc.Element(ns + "Project").Elements(ns + "PropertyGroup").Elements(ns + "ProjectTypeGuids")
                select el.Value.Split(';');

            newproj._ProjectTypeGuids = new List<string>();
            foreach (string[] guids in guidsarr)
            {
                foreach (string guid in guids)
                {
                    newproj._ProjectTypeGuids.Add(guid);
                }
            }


            return newproj;
        }
    }
}
