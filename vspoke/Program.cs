using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Xml;

namespace vspoke
{
    class Program
    {
        static void Main(string[] args)
        {
            if (args.Length != 2)
            {
                Console.WriteLine(
@"Usage: vspoke <nodepath> <value>

Parameters:
  nodepath: A xpath-like element path. Conditional configurations and/or platforms can be specified.
  value:    Text written to xml element. \n -> CR LF.

Notice:
  All *.csproj files from current directory including subdirectories are parsed.
  Read-only attributes are removed if needed.

Examples:
  vspoke /Project/PropertyGroup[Debug]/CodeAnalysisRuleSet MinimumRecommendedRules.ruleset
  vspoke /Project/PropertyGroup[Debug]/RunCodeAnalysis true
  vspoke /Project/PropertyGroup[Debug]/CodeAnalysisIgnoreGeneratedCode false

  vspoke /Project/PropertyGroup[NEW]/PreBuildEvent ""a b\nc d""");

                return;
            }

            SetNodeValue(args[0], args[1].Replace(@"\n", "\r\n"));

            return;
        }

        static void SetNodeValue(string nodepath, string value)
        {
            string[] files = System.IO.Directory.GetFiles(".", "*.csproj", System.IO.SearchOption.AllDirectories);

            foreach (string filename in files)
            {
                SetNodeValue(filename, nodepath, value);
            }
        }

        static void SetNodeValue(string filename, string nodepath, string value)
        {
            string vsuri = "http://schemas.microsoft.com/developer/msbuild/2003";

            XmlDocument xdoc = new XmlDocument();
            bool modified = false;

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

            System.Xml.XmlNamespaceManager nsm = new System.Xml.XmlNamespaceManager(xdoc.NameTable);
            nsm.AddNamespace("xs", vsuri);


            Console.WriteLine("File: '" + filename + "'");

            string[] tokens = nodepath.Split('/');

            //string path = string.Empty;
            XmlNode xnode = xdoc;
            for (int depth = 0; depth < tokens.Length; depth++)
            {
                string token = tokens[depth];

                if (token == string.Empty)
                    continue;

                //path += "/" + token;


                string configuration;
                int pos1 = token.IndexOf('[');
                int pos2 = token.IndexOf(']');
                string elementname;
                if (pos1 >= 0 && pos2 > pos1)
                {
                    elementname = token.Substring(0, pos1);
                    configuration = token.Substring(pos1 + 1, pos2 - pos1 - 1);
                }
                else
                {
                    elementname = token;
                    configuration = string.Empty;
                }
                string nodequery = "xs:" + elementname;

                bool found = false;

                XmlNodeList xlist = xnode.SelectNodes(nodequery, nsm);
                foreach (XmlNode xnode2 in xlist)
                {
                    if (xnode2 is XmlElement)
                    {
                        bool newnode = configuration == "NEW" ? true : false;

                        if (newnode)
                        {
                            XmlAttribute xattr = xnode2.Attributes["Condition"];
                            if (xattr != null)
                                continue;

                            if (depth == tokens.Length - 1)
                            {
                                // Reached end node - set value
                                XmlElement xele = xnode2 as XmlElement;
                                string oldvalue = xele.InnerText;
                                if (oldvalue != value)
                                {
                                    xele.InnerText = value;
                                    modified = true;
                                    Console.WriteLine("'" + xele.Name + "': '" + oldvalue + "' -> '" + xele.InnerText + "'");
                                }
                            }
                            else
                            {
                                if (xnode2.SelectNodes("xs:" + tokens[depth + 1], nsm).Count == 0)
                                {
                                    continue;
                                }
                            }
                        }
                        else
                        {
                            if (configuration != string.Empty)
                            {
                                XmlAttribute xattr = xnode2.Attributes["Condition"];
                                if (xattr == null)
                                    continue;
                                string condition = xattr.Value;
                                int pos3 = condition.IndexOf("==");
                                if (pos3 >= 0)
                                {
                                    string[] conditionvalues = condition.Substring(pos3 + 2).Trim().Trim('\'').Split('|');
                                    if (!conditionvalues.Any(c => c.Trim() == configuration))
                                        continue;
                                }
                            }
                        }

                        xnode = xnode2;
                        found = true;

                        if (depth == tokens.Length - 1)
                        {
                            // Reached end node - set value
                            XmlElement xele = xnode2 as XmlElement;
                            string oldvalue = xele.InnerText;
                            if (oldvalue != value)
                            {
                                xele.InnerText = value;
                                modified = true;
                                Console.WriteLine("'" + xele.Name + "': '" + oldvalue + "' -> '" + xele.InnerText + "'");
                            }
                        }
                    }
                }
                if (!found)
                {
                    // Didn't find any child node - create it
                    XmlElement xele = xdoc.CreateElement(elementname, vsuri);
                    xnode.AppendChild(xele);

                    xnode = xele;

                    if (depth == tokens.Length - 1)
                    {
                        // Created end node - set value
                        xele.InnerText = value;
                        modified = true;
                        Console.WriteLine("'" + xele.Name + "': '" + xele.InnerText + "'");
                    }
                }
            }

            if (modified)
            {
                FileAttributes fa = File.GetAttributes(filename);
                if ((fa & FileAttributes.ReadOnly) == FileAttributes.ReadOnly)
                {
                    File.SetAttributes(filename, fa & ~FileAttributes.ReadOnly);
                }

                xdoc.Save(filename);
            }
        }
    }
}
