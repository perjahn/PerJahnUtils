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

            XElement newref = new(ns + "ProjectReference",
                new XAttribute("Include", Include),
                new XElement(ns + "Project", Project),
                new XElement(ns + "Name", Name));

            // Sort insert
            XElement[] groups = [.. xdoc.Element(ns + "Project").Elements(ns + "ItemGroup").Where(el => el.Element(ns + "ProjectReference") != null)];

            if (groups.Length == 0)
            {
                groups = [.. xdoc.Element(ns + "Project").Elements(ns + "ItemGroup")];

                XElement newgroup = new(ns + "ItemGroup", newref);
                groups.Last().AddAfterSelf(newgroup);
            }
            else
            {
                XElement[] refs = [.. groups.Elements(ns + "ProjectReference").Where(el => el.Attribute("Include") != null).OrderBy(el => el.Attribute("Include").Value)];

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
                    for (var i = 0; i < refs.Length - 1; i++)
                    {
                        var inc1 = refs.ElementAt(i).Attribute("Include").Value;
                        var inc2 = refs.ElementAt(i + 1).Attribute("Include").Value;
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
