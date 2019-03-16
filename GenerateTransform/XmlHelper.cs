using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;

namespace GenerateTransform
{
    class XmlHelper
    {
        public static string[] idattributenames = { "id", "key" };

        public static string GetElementPath(XElement ele)
        {
            return "/" + string.Join("/", ele.AncestorsAndSelf().Reverse().Select(el => el.Name.LocalName));
        }

        public static bool AreEqualId(XElement el1, XElement el2)
        {
            if (el1.Name.LocalName != el2.Name.LocalName)
            {
                return false;
            }

            foreach (string idname in idattributenames)
            {
                XAttribute a1 = el1.Attribute(idname);
                XAttribute a2 = el2.Attribute(idname);

                if ((a1 == null && a2 != null) || (a1 != null && a2 == null) ||
                    (a1 != null && a2 != null && a1.Value != a2.Value))
                {
                    return false;
                }
            }

            return true;
        }

        public static bool AreEqual(XElement el1, XElement el2)
        {
            int b = 123;
            if (el1.Name.LocalName == "add" && el1.Attribute("name") != null && el1.Attribute("name").Value == "membershipProvider")
            {
                b++;
            }

            if (el1.Name.LocalName != el2.Name.LocalName)
            {
                return false;
            }

            XAttribute[] attributes1 = el1.Attributes().ToArray();
            XAttribute[] attributes2 = el2.Attributes().ToArray();
            if (attributes1.Length != attributes2.Length)
            {
                return false;
            }

            if (Enumerable.SequenceEqual(
                attributes1.Select(a => new { name = a.Name.LocalName, value = a.Value }).OrderBy(a => a.name),
                attributes2.Select(a => new { name = a.Name.LocalName, value = a.Value }).OrderBy(a => a.name)))
            {
                return true;
            }
            else
            {
                return false;
            }
        }

        public static XElement FindExistingElement(XDocument xdoc, XElement el, string[] idattributenames)
        {
            return null;
        }

        public static XAttribute[] GetNewAndChangedAttributes(XElement el1, XElement el2)
        {
            throw new NotImplementedException();
        }

        public static XContainer CreateParentElements(XDocument xdoc, string path)
        {
            string nsstring = "http://schemas.microsoft.com/XML-Document-Transform";

            string buildpath = string.Empty;

            string parentpath = path.Substring(0, path.LastIndexOf('/'));

            XContainer result = xdoc;

            foreach (string elname in parentpath.Split('/').Where(elname => elname != string.Empty))
            {
                bool isroot = buildpath == string.Empty;

                buildpath += "/" + elname;

                XElement parentel = xdoc.Descendants().Where(el => buildpath == GetElementPath(el)).SingleOrDefault();

                if (parentel == null)
                {
                    Console.WriteLine($"Adding: {buildpath}");

                    XElement newel =
                        isroot ?
                        new XElement(elname, new XAttribute(XNamespace.Xmlns + "xdt", nsstring)) :
                        new XElement(elname);

                    result.Add(newel);
                    result = newel;
                }
                else
                {
                    result = parentel;
                }
            }

            return result;
        }
    }

    class XElementComparer : IComparer<XElement>
    {
        public int Compare(XElement el1, XElement el2)
        {
            if (el1.Name.LocalName.CompareTo(el2.Name.LocalName) < 0)
            {
                return -1;
            }
            if (el1.Name.LocalName.CompareTo(el2.Name.LocalName) > 0)
            {
                return 1;
            }

            // a=1, b=2
            // b=1, c=2
            // --> 1

            XAttribute[] attributes1 = el1.Attributes().Where(a => XmlHelper.idattributenames.Contains(a.Name.LocalName)).OrderBy(a => a).ToArray();
            XAttribute[] attributes2 = el2.Attributes().Where(a => XmlHelper.idattributenames.Contains(a.Name.LocalName)).OrderBy(a => a).ToArray();

            int[] results = attributes1.Zip(attributes2, (a1, a2) => a1.Value.CompareTo(a2.Value)).ToArray();
            if (results.Any(r => r < 0) && results.Any(r => r > 0))
            {
                string concat1 = string.Join(string.Empty, attributes1.Select(a => a.Name.LocalName));
                string concat2 = string.Join(string.Empty, attributes2.Select(a => a.Name.LocalName));
                Console.WriteLine($"WARNING: No common attributes, sorting using concat names: {el1.Name.LocalName} <-> {el2.Name.LocalName}: '{concat1}', '{concat2}'");

                return concat1.CompareTo(concat2);
            }

            if (results.Any(r => r < 0))
            {
                return -1;
            }
            if (results.Any(r => r > 0))
            {
                return 1;
            }

            return 0;
        }
    }
}
