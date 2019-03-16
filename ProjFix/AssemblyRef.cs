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
        public string Hintpath { get; set; }
        public bool? Copylocal { get; set; }  // Xml tag name=Private

        // These 2 lists are used when loading, before validating/fixing, and compacting them to single items.
        public List<string> Hintpaths { get; set; }
        public List<string> Copylocals { get; set; }


        public void AddToDoc(XDocument xdoc, XNamespace ns)
        {
            ConsoleHelper.WriteLine($"  Adding assembly ref: '{Include}", true);

            XElement newref;

            if (Hintpath == null)
            {
                newref = new XElement(ns + "Reference",
                    new XAttribute("Include", Include),
                    new XElement(ns + "SpecificVersion", "False")
                    );
            }
            else
            {
                newref = new XElement(ns + "Reference",
                    new XAttribute("Include", Include),
                    new XElement(ns + "SpecificVersion", "False"),
                    new XElement(ns + "HintPath", Hintpath)
                    );
            }


            // Sort insert
            var groups = from el in xdoc.Element(ns + "Project").Elements(ns + "ItemGroup")
                         where el.Element(ns + "Reference") != null
                         select el;
            if (groups.Count() == 0)
            {
                ConsoleHelper.ColorWrite(ConsoleColor.Yellow, $"NotImplementedException: Cannot insert reference!");
                return;
            }

            var refs = from el in groups.ElementAt(0).Elements(ns + "Reference")
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

        public void UpdateInDoc(XDocument xdoc, XNamespace ns)
        {
            // Update existing ref (hint path)

            List<XElement> references2 =
                (from el in xdoc.Element(ns + "Project").Elements(ns + "ItemGroup").Elements(ns + "Reference")
                 where el.Attribute("Include") != null && Project.GetShortRef(el.Attribute("Include").Value) == Project.GetShortRef(Include)
                 select el)
            .ToList();

            if (references2.Count < 1)
            {
                ConsoleHelper.WriteLine($"  Error: Couldn't update assembly ref: '{Include}': Didn't find reference in project file.", false);
                return;
            }
            if (references2.Count > 1)
            {
                ConsoleHelper.WriteLine($"  Error: Couldn't update assembly ref: '{Include} ': Found too many matching references in project file.", false);
                return;
            }

            XElement reference = references2[0];


            XElement hintPath = reference.Element(ns + "HintPath");
            if (Hintpath == null)
            {
                if (hintPath != null)
                {
                    string oldpath = hintPath.Value;
                    if (oldpath != Hintpath)
                    {
                        ConsoleHelper.WriteLine($"  Updating assembly ref: Removing hintpath: '{Include}': '{oldpath}'.", true);
                        hintPath.Remove();
                    }
                }
            }
            else
            {
                if (hintPath == null)
                {
                    ConsoleHelper.WriteLine($"  Updating assembly ref: Adding hintpath: '{Include}', '{Hintpath}'.", true);
                    hintPath = new XElement(ns + "HintPath", Hintpath);
                    reference.Add(hintPath);
                }
                else
                {
                    string oldpath = hintPath.Value;
                    if (oldpath != Hintpath)
                    {
                        ConsoleHelper.WriteLine($"  Updating assembly ref: Updating hintpath: '{Include}': '{oldpath}' -> '{Hintpath}'.", true);
                        hintPath.Value = Hintpath;
                    }
                }
            }

            XAttribute includeattr = reference.Attribute("Include");
            if (includeattr.Value != Include)
            {
                ConsoleHelper.WriteLine($"  Updating assembly ref: Updating include: '{includeattr.Value}' -> '{Include}'.", true);
                includeattr.Value = Include;
            }
        }
    }
}
