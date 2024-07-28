// todo: Add namespace support

using System;
using System.IO;
using System.Xml;
using System.Xml.XPath;

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

            return args.Length == 2 ? Parse(args[0], args[1], false) : Parse(args[1], args[2], true);
        }

        static int Parse(string xpath, string filename, bool printFilenameIfMatch)
        {
            XmlDocument xdoc = new();

            string buf;
            using StreamReader sr = new(filename);
            buf = sr.ReadToEnd();

            if (buf.StartsWith("<!DOCTYPE"))
            {
                var pos = buf.IndexOf('>');
                buf = buf[(pos + 1)..].Trim();
            }
            buf = buf.Replace("&reg;", "");

            xdoc.LoadXml(buf);

            XmlNodeList? xlist;

            try
            {
                xlist = xdoc.SelectNodes(xpath);
            }
            catch (XPathException ex)
            {
                Console.WriteLine($"Invalid XPath: {ex.Message}");
                return 0;
            }

            if (xlist == null)
            {
                Console.WriteLine($"Couldn't find any xml nodes in file: {filename}");
                return 0;
            }

            if (printFilenameIfMatch && xlist.Count > 0)
            {
                Console.WriteLine($"-=-=- {filename} -=-=-");
            }

            foreach (var xnode in xlist)
            {
                if (xnode is XmlElement xele)
                {
                    Console.WriteLine(xele.InnerText);
                }

                if (xnode is XmlAttribute xattr)
                {
                    Console.WriteLine(xattr.Value);
                }
            }

            return xlist.Count;
        }
    }
}
