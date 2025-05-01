using System;
using System.IO;
using System.Linq;
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
            var files = Directory.GetFiles(".", "*.csproj", SearchOption.AllDirectories);

            foreach (var filename in files)
            {
                SetNodeValue(filename, nodepath, value);
            }
        }

        static void SetNodeValue(string filename, string nodepath, string value)
        {
            var vsuri = "http://schemas.microsoft.com/developer/msbuild/2003";

            XmlDocument xdoc = new();
            var modified = false;

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

            XmlNamespaceManager nsm = new(xdoc.NameTable);
            nsm.AddNamespace("xs", vsuri);

            Console.WriteLine("File: '" + filename + "'");

            var tokens = nodepath.Split('/');

            //string path = string.Empty;
            XmlNode xnode = xdoc;
            for (var depth = 0; depth < tokens.Length; depth++)
            {
                var token = tokens[depth];

                if (token == string.Empty)
                {
                    continue;
                }

                string configuration;
                var pos1 = token.IndexOf('[');
                var pos2 = token.IndexOf(']');
                string elementname;
                if (pos1 >= 0 && pos2 > pos1)
                {
                    elementname = token[..pos1];
                    configuration = token.Substring(pos1 + 1, pos2 - pos1 - 1);
                }
                else
                {
                    elementname = token;
                    configuration = string.Empty;
                }
                var nodequery = "xs:" + elementname;

                var found = false;

                var xlist = xnode.SelectNodes(nodequery, nsm);
                foreach (var xnode2 in xlist)
                {
                    if (xnode2 is not XmlElement xnode3)
                    {
                        continue;
                    }

                    var newnode = configuration == "NEW";

                    if (newnode)
                    {
                        var xattr = xnode3.Attributes["Condition"];
                        if (xattr != null)
                        {
                            continue;
                        }

                        if (depth == tokens.Length - 1)
                        {
                            // Reached end node - set value
                            var xele = xnode2 as XmlElement;
                            var oldvalue = xele.InnerText;
                            if (oldvalue != value)
                            {
                                xele.InnerText = value;
                                modified = true;
                                Console.WriteLine("'" + xele.Name + "': '" + oldvalue + "' -> '" + xele.InnerText + "'");
                            }
                        }
                        else
                        {
                            if (xnode3.SelectNodes("xs:" + tokens[depth + 1], nsm).Count == 0)
                            {
                                continue;
                            }
                        }
                    }
                    else
                    {
                        if (configuration != string.Empty)
                        {
                            var xattr = xnode3.Attributes["Condition"];
                            if (xattr == null)
                            {
                                continue;
                            }
                            var condition = xattr.Value;
                            var pos3 = condition.IndexOf("==");
                            if (pos3 >= 0)
                            {
                                string[] conditionvalues = condition[(pos3 + 2)..].Trim().Trim('\'').Split('|');
                                if (!conditionvalues.Any(c => c.Trim() == configuration))
                                {
                                    continue;
                                }
                            }
                        }
                    }

                    xnode = xnode3;
                    found = true;

                    if (depth == tokens.Length - 1)
                    {
                        // Reached end node - set value
                        var xele = xnode2 as XmlElement;
                        var oldvalue = xele.InnerText;
                        if (oldvalue != value)
                        {
                            xele.InnerText = value;
                            modified = true;
                            Console.WriteLine("'" + xele.Name + "': '" + oldvalue + "' -> '" + xele.InnerText + "'");
                        }
                    }
                }
                if (!found)
                {
                    // Didn't find any child node - create it
                    var xele = xdoc.CreateElement(elementname, vsuri);
                    _ = xnode.AppendChild(xele);

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
                var fa = File.GetAttributes(filename);
                if ((fa & FileAttributes.ReadOnly) == FileAttributes.ReadOnly)
                {
                    File.SetAttributes(filename, fa & ~FileAttributes.ReadOnly);
                }

                xdoc.Save(filename);
            }
        }
    }
}
