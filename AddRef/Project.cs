using System;
using System.Linq;
using System.Xml.Linq;

namespace AddRef
{
    class Project
    {
        public static void AddRef(string projectfilepath, string referencename)
        {
            var xdoc = XDocument.Load(projectfilepath);
            var ns = xdoc.Root.Name.Namespace;
            AddReferenceToDoc(xdoc, ns, referencename, null);
            xdoc.Save(projectfilepath);
        }

        private static void AddReferenceToDoc(XDocument xdoc, XNamespace ns, string include, string hintpath)
        {
            XElement newref = hintpath == null
                ? new XElement(ns + "Reference", new XAttribute("Include", include))
                : new XElement(ns + "Reference", new XAttribute("Include", include), new XElement(ns + "HintPath", hintpath));

            // Sort insert
            XElement[] groups = [.. xdoc.Element(ns + "Project").Elements(ns + "ItemGroup").Where(el => el.Element(ns + "Reference") != null)];
            if (groups.Length == 0)
            {
                throw new NotImplementedException("Cannot insert reference!");
            }

            XElement[] refs = [.. groups.ElementAt(0).Elements(ns + "Reference").Where(el => el.Attribute("Include") != null).OrderBy(el => el.Attribute("Include").Value)];

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
                for (var i = 0; i < refs.Length - 1; i++)
                {
                    var inc1 = refs.ElementAt(i).Attribute("Include").Value;
                    var inc2 = refs.ElementAt(i + 1).Attribute("Include").Value;
                    if (include.CompareTo(inc1) > 0 && include.CompareTo(inc2) < 0)
                    {
                        refs.ElementAt(i).AddAfterSelf(newref);
                    }
                }
            }
        }
    }
}
