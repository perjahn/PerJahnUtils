using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;

namespace AddRef
{
    class Project
    {
        public static void AddRef(string projectfilepath, string referencename)
        {
            XDocument xdoc = XDocument.Load(projectfilepath);

            XNamespace ns = xdoc.Root.Name.Namespace;

            AddReferenceToDoc(xdoc, ns, referencename, null);

            xdoc.Save(projectfilepath);

            return;
        }

        private static void AddReferenceToDoc(XDocument xdoc, XNamespace ns, string include, string hintpath)
        {
            XElement newref;

            if (hintpath == null)
            {
                newref = new XElement(ns + "Reference",
                    new XAttribute("Include", include)
                    //new XElement(ns + "SpecificVersion", "False")
                    );
            }
            else
            {
                newref = new XElement(ns + "Reference",
                    new XAttribute("Include", include),
                    //new XElement(ns + "SpecificVersion", "False"),
                    new XElement(ns + "HintPath", hintpath)
                    );
            }


            // Sort insert
            var groups = from el in xdoc.Element(ns + "Project").Elements(ns + "ItemGroup")
                                     where el.Element(ns + "Reference") != null
                                     select el;
            if (groups.Count() == 0)
            {
                throw new NotImplementedException("Cannot insert reference!");
            }

            var refs = from el in groups.ElementAt(0).Elements(ns + "Reference")
                                 where el.Attribute("Include") != null
                                 orderby el.Attribute("Include").Value
                                 select el;

            if (include.CompareTo(refs.First().Attribute("Include").Value) < 0)
            {
                groups.ElementAt(0).AddFirst(newref);
            }
            else if (include.CompareTo(refs.Last().Attribute("Include").Value) > 0)
            {
                refs.Last().AddAfterSelf(newref);
            }
            else
            {
                for (int i = 0; i < refs.Count() - 1; i++)
                {
                    string inc1 = refs.ElementAt(i).Attribute("Include").Value;
                    string inc2 = refs.ElementAt(i + 1).Attribute("Include").Value;
                    if (include.CompareTo(inc1) > 0 && include.CompareTo(inc2) < 0)
                    {
                        refs.ElementAt(i).AddAfterSelf(newref);
                    }
                }
            }
        }
    }
}
