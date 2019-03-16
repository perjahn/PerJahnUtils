using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;

namespace ProjFix
{
    // Corresponds to a <ProjectReference> tag in project file.
    class ProjectRef : Reference
    {
        public string Project { get; set; }
        public string Package { get; set; }

        // These 2 lists are used when loading, before validating/fixing, and compacting them to single items.
        public List<string> Projects { get; set; }
        public List<string> Packages { get; set; }


        public void AddToDoc(XDocument xdoc, XNamespace ns)
        {
            ConsoleHelper.WriteLine($"  Adding proj ref: '{Include}'", true);

            XElement newref = new XElement(ns + "ProjectReference",
                new XAttribute("Include", Include),
                new XElement(ns + "Project", Project),
                new XElement(ns + "Name", Name)
                );

            // Sort insert
            var groups = from el in xdoc.Element(ns + "Project").Elements(ns + "ItemGroup")
                         where el.Element(ns + "ProjectReference") != null
                         select el;

            if (groups.Count() == 0)
            {
                groups = from el in xdoc.Element(ns + "Project").Elements(ns + "ItemGroup")
                         select el;

                XElement newgroup = new XElement(ns + "ItemGroup", newref);
                groups.Last().AddAfterSelf(newgroup);
            }
            else
            {
                var refs = from el in groups.Elements(ns + "ProjectReference")
                           where el.Attribute("Include") != null
                           orderby el.Attribute("Include").Value
                           select el;

                if (Include.CompareTo(refs.First().Attribute("Include").Value) < 0)
                {
                    groups.ElementAt(0).AddFirst(newref);
                }
                else if (Include.CompareTo(refs.Last().Attribute("Include").Value) > 0)
                {
                    refs.Last().AddAfterSelf(newref);
                }
                else
                {
                    for (int i = 0; i < refs.Count() - 1; i++)
                    {
                        string inc1 = refs.ElementAt(i).Attribute("Include").Value;
                        string inc2 = refs.ElementAt(i + 1).Attribute("Include").Value;
                        if (Include.CompareTo(inc1) > 0 && Include.CompareTo(inc2) < 0)
                        {
                            refs.ElementAt(i).AddAfterSelf(newref);
                        }
                    }
                }
            }
        }
    }
}
