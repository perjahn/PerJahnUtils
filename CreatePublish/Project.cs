using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml.Linq;
using System.IO;

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
            catch (IOException ex)
            {
                ConsoleHelper.ColorWrite(ConsoleColor.Red, "Couldn't load project: '" + fullfilename + "': " + ex.Message);
                return null;
            }
            catch (UnauthorizedAccessException ex)
            {
                ConsoleHelper.ColorWrite(ConsoleColor.Red, "Couldn't load project: '" + fullfilename + "': " + ex.Message);
                return null;
            }
            catch (ArgumentException ex)
            {
                ConsoleHelper.ColorWrite(ConsoleColor.Red, "Couldn't load project: '" + fullfilename + "': " + ex.Message);
                return null;
            }
            catch (System.Xml.XmlException ex)
            {
                ConsoleHelper.ColorWrite(ConsoleColor.Red, "Couldn't load project: '" + fullfilename + "': " + ex.Message);
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
