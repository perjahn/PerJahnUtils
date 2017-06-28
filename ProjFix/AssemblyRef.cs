using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml.Linq;

namespace ProjFix
{
    // Corresponds to a <Reference> tag in project file.
    class AssemblyRef : Reference
    {
        public string hintpath { get; set; }
        public bool? copylocal { get; set; }  // Xml tag name=Private

        // These 2 lists are used when loading, before validating/fixing, and compacting them to single items.
        public List<string> hintpaths { get; set; }
        public List<string> copylocals { get; set; }


        public void AddToDoc(XDocument xdoc, XNamespace ns)
        {
            ConsoleHelper.WriteLine($"  Adding assembly ref: '{include}", true);

            XElement newref;

            if (hintpath == null)
            {
                newref = new XElement(ns + "Reference",
                    new XAttribute("Include", include),
                    new XElement(ns + "SpecificVersion", "False")
                    );
            }
            else
            {
                newref = new XElement(ns + "Reference",
                    new XAttribute("Include", include),
                    new XElement(ns + "SpecificVersion", "False"),
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

        public void UpdateInDoc(XDocument xdoc, XNamespace ns)
        {
            // Update existing ref (hint path)

            List<XElement> references2 =
                (from el in xdoc.Element(ns + "Project").Elements(ns + "ItemGroup").Elements(ns + "Reference")
                 where el.Attribute("Include") != null && Project.GetShortRef(el.Attribute("Include").Value) == Project.GetShortRef(include)
                 select el)
            .ToList();

            if (references2.Count < 1)
            {
                ConsoleHelper.WriteLine($"  Error: Couldn't update assembly ref: '{include}': Didn't find reference in project file.", false);
                return;
            }
            if (references2.Count > 1)
            {
                ConsoleHelper.WriteLine($"  Error: Couldn't update assembly ref: '{include} ': Found too many matching references in project file.", false);
                return;
            }

            XElement reference = references2[0];


            XElement hintPath = reference.Element(ns + "HintPath");
            if (hintpath == null)
            {
                if (hintPath != null)
                {
                    string oldpath = hintPath.Value;
                    if (oldpath != hintpath)
                    {
                        ConsoleHelper.WriteLine($"  Updating assembly ref: Removing hintpath: '{include}': '{oldpath}'.", true);
                        hintPath.Remove();
                    }
                }
            }
            else
            {
                if (hintPath == null)
                {
                    ConsoleHelper.WriteLine($"  Updating assembly ref: Adding hintpath: '{include}', '{hintpath}'.", true);
                    hintPath = new XElement(ns + "HintPath", hintpath);
                    reference.Add(hintPath);
                }
                else
                {
                    string oldpath = hintPath.Value;
                    if (oldpath != hintpath)
                    {
                        ConsoleHelper.WriteLine($"  Updating assembly ref: Updating hintpath: '{include}': '{oldpath}' -> '{hintpath}'.", true);
                        hintPath.Value = hintpath;
                    }
                }
            }

            XAttribute includeattr = reference.Attribute("Include");
            if (includeattr.Value != include)
            {
                ConsoleHelper.WriteLine($"  Updating assembly ref: Updating include: '{includeattr.Value}' -> '{include}'.", true);
                includeattr.Value = include;
            }
        }
    }
}
