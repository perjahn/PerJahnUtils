// todo: Add namespace support

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml;

namespace xpath
{
    class Program
    {
        static int Main(string[] args)
        {
            if (!(args.Length == 2 || (args.Length == 3 && args[0] == "-f")))
            {
                Console.WriteLine(
                    "Usage: [-f] <xpath> <filename>" + Environment.NewLine +
                    "" + Environment.NewLine +
                    "-f:  Print filename if any match." + Environment.NewLine +
                    "" + Environment.NewLine +
                    "Returns number of nodes matched.");
                return 0;
            }

            if (args.Length == 2)
            {
                return Parse(args[0], args[1], false);
            }
            else
            {
                return Parse(args[1], args[2], true);
            }
        }

        static int Parse(string xpath, string filename, bool printFilenameIfMatch)
        {
            XmlDocument xdoc = new XmlDocument();

            string buf;
            using (System.IO.StreamReader sr = new System.IO.StreamReader(filename))
            {
                buf = sr.ReadToEnd();
            }

            if (buf.StartsWith("<!DOCTYPE"))
            {
                int pos = buf.IndexOf('>');
                buf = buf.Substring(pos + 1).Trim();
            }
            buf = buf.Replace("&reg;", "");

            xdoc.LoadXml(buf);

            XmlNodeList xlist;

            try
            {
                xlist = xdoc.SelectNodes(xpath);
            }
            catch (System.Xml.XPath.XPathException ex)
            {
                Console.WriteLine("Invalid XPath: " + ex.Message);
                return 0;
            }

            if (printFilenameIfMatch && xlist.Count > 0)
            {
                Console.WriteLine("-=-=- " + filename + " -=-=-");
            }

            foreach (XmlNode xnode in xlist)
            {
                XmlElement xele = xnode as XmlElement;
                if (xele != null)
                {
                    Console.WriteLine(xele.InnerText);
                }

                XmlAttribute xattr = xnode as XmlAttribute;
                if (xattr != null)
                {
                    Console.WriteLine(xattr.Value);
                }
            }

            return xlist.Count;
        }
    }
}
