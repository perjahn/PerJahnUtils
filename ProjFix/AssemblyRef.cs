using System;
using System.Collections.Generic;
using System.Linq;
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

            XElement newref = Hintpath == null
                ? new XElement(ns + "Reference",
                    new XAttribute("Include", Include),
                    new XElement(ns + "SpecificVersion", "False"))
                : new XElement(ns + "Reference",
                    new XAttribute("Include", Include),
                    new XElement(ns + "SpecificVersion", "False"),
                    new XElement(ns + "HintPath", Hintpath));

            // Sort insert
            XElement[] groups = [.. xdoc.Element(ns + "Project").Elements(ns + "ItemGroup").Where(el => el.Element(ns + "Reference") != null)];
            if (groups.Length == 0)
            {
                ConsoleHelper.ColorWrite(ConsoleColor.Yellow, $"NotImplementedException: Cannot insert reference!");
                return;
            }

            XElement[] refs = [.. groups.ElementAt(0).Elements(ns + "Reference").Where(el => el.Attribute("Include") != null).OrderBy(el => el.Attribute("Include").Value)];

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

        public void UpdateInDoc(XDocument xdoc, XNamespace ns)
        {
            // Update existing ref (hint path)

            List<XElement> references2 = [.. xdoc
                .Element(ns + "Project").Elements(ns + "ItemGroup").Elements(ns + "Reference")
                .Where(el => el.Attribute("Include") != null && Project.GetShortRef(el.Attribute("Include").Value) == Project.GetShortRef(Include))];

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

            var reference = references2[0];

            var hintPath = reference.Element(ns + "HintPath");
            if (Hintpath == null)
            {
                if (hintPath != null)
                {
                    var oldpath = hintPath.Value;
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
                    var oldpath = hintPath.Value;
                    if (oldpath != Hintpath)
                    {
                        ConsoleHelper.WriteLine($"  Updating assembly ref: Updating hintpath: '{Include}': '{oldpath}' -> '{Hintpath}'.", true);
                        hintPath.Value = Hintpath;
                    }
                }
            }

            var includeattr = reference.Attribute("Include");
            if (includeattr.Value != Include)
            {
                ConsoleHelper.WriteLine($"  Updating assembly ref: Updating include: '{includeattr.Value}' -> '{Include}'.", true);
                includeattr.Value = Include;
            }
        }
    }
}
