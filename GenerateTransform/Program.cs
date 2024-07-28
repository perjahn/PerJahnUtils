using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml.Linq;

namespace GenerateTransform
{
    public enum Operation { insert, remove, update };

    class TransformElement
    {
        public Operation op;
        public string Path { get; set; }
        public XElement Xelement { get; set; }
    }

    class Program
    {
        static int Main(string[] args)
        {
            if (args.Length == 1 && args[0] == "-unittest")
            {
                UnitTest tests = new();
                return tests.Test();
            }

            if (args.Length != 3)
            {
                Console.WriteLine(
@"GenerateTransform 0.001 gamma - Creates transform xsd file from two config files.

Usage: <sourcefile> <targetfile> <transfile>

sourcefile:  Original file, usually from source control.
targetfile:  Config file, usually deployed to production environment.
transfile:   Transformation output file that will be created.");
                return 1;
            }

            CreateTransform(args[0], args[1], args[2]);

            return 0;
        }

        private static void CreateTransform(string sourcefile, string targetfile, string transfile)
        {
            List<TransformElement> transformElements = [];
            XDocument xsource, xtarget;

            try
            {
                xsource = XDocument.Load(sourcefile);
            }
            catch (IOException ex)
            {
                Console.WriteLine(ex.Message);
                return;
            }
            try
            {
                xtarget = XDocument.Load(targetfile);
            }
            catch (IOException ex)
            {
                Console.WriteLine(ex.Message);
                return;
            }

            XElement[] elements = [.. xsource.Descendants().Concat(xtarget.Descendants())];
            XElement[] sourceElements = [.. xsource.Descendants()];
            XElement[] targetElements = [.. xtarget.Descendants()];

            Console.WriteLine($"Total elements: {elements.Length} ({sourceElements.Length} + {targetElements.Length})");

            string[] elementpaths = [.. elements
                .Select(XmlHelper.GetElementPath)
                .Distinct()
                .OrderBy(p => p)];

            Console.WriteLine($"Distinct paths: {elementpaths.Length}");

            foreach (var elementpath in elementpaths)
            {
                sourceElements = [.. xsource.Descendants().Where(el => XmlHelper.GetElementPath(el) == elementpath)];
                targetElements = [.. xtarget.Descendants().Where(el => XmlHelper.GetElementPath(el) == elementpath)];

                foreach (var targetel in targetElements)
                {
                    var b = 123;
                    if (targetel.Name.LocalName == "add" && targetel.Attribute("name") != null && targetel.Attribute("name").Value == "membershipProvider")
                    {
                        b++;
                    }

                    // If exist, update. Else add.

                    XElement[] matchingElements = [.. sourceElements.Where(sourceel => XmlHelper.AreEqualId(targetel, sourceel))];
                    if (matchingElements.Length > 1)
                    {
                        // Dont know which sourceel targetel match.
                        Console.WriteLine($"Ignoring element: {elementpath}, matching too many elements: {matchingElements.Length}");
                        continue;
                    }
                    else if (matchingElements.Length == 1)
                    {
                        if (!XmlHelper.AreEqual(targetel, matchingElements[0]))
                        {
                            //Console.WriteLine($"Updating: {elementpath}");
                            Console.Write("u");
                            TransformElement updateel = new()
                            {
                                op = Operation.update,
                                Path = elementpath,
                                Xelement = new(targetel.Name.LocalName, targetel.Attributes())
                            };
                            transformElements.Add(updateel);
                        }
                    }
                    else
                    {
                        if (!sourceElements.Any(sourceel => XmlHelper.AreEqual(sourceel, targetel)))
                        {
                            //Console.WriteLine($"Insert: elementpath}");
                            Console.Write("i");
                            TransformElement insertel = new()
                            {
                                op = Operation.insert,
                                Path = elementpath,
                                Xelement = new(targetel.Name.LocalName, targetel.Attributes())
                            };
                            transformElements.Add(insertel);
                        }
                    }
                }

                foreach (var sourceel in sourceElements)
                {
                    if (!targetElements.Any(targetel => XmlHelper.AreEqualId(sourceel, targetel)))
                    {
                        Console.Write("d");
                        //Console.WriteLine($"Delete: {elementpath}");
                        TransformElement removeel = new()
                        {
                            op = Operation.remove,
                            Path = elementpath,
                            Xelement = new(sourceel.Name.LocalName,
                            sourceel.Attributes().Where(a => XmlHelper.idattributenames.Contains(a.Name.LocalName)).OrderBy(a => a))
                        };
                        transformElements.Add(removeel);
                    }
                }
            }

            WriteElements(transformElements, transfile);
        }

        private static void WriteElements(List<TransformElement> transformElements, string transfile)
        {
            XNamespace ns = @"http://schemas.microsoft.com/XML-Document-Transform";
            XDocument xtrans = new();

            string[] paths = [.. transformElements.Select(el => el.Path).Distinct()];

            Console.WriteLine($"Transform elements: {transformElements.Count}, paths: {paths.Length}");
            foreach (var path in paths)
            {
                foreach (var transel in transformElements
                    .Where(el => el.Path == path)
                    .OrderBy(el => el.Xelement, new XElementComparer()))
                {
                    switch (transel.op)
                    {
                        case Operation.insert:
                            {
                                //Console.WriteLine($"{path}: Insert");

                                var parent = XmlHelper.CreateParentElements(xtrans, path);
                                XElement newel = new(transel.Xelement.Name.LocalName);
                                foreach (var attr in transel.Xelement.Attributes())
                                {
                                    newel.Add(new XAttribute(attr.Name.LocalName, attr.Value));
                                }
                                newel.Add(new XAttribute(ns + "Transform", "Insert"));
                                parent.Add(newel);
                                break;
                            }

                        case Operation.update:
                            {
                                //Console.WriteLine($"{path}: Update");

                                var parent = XmlHelper.CreateParentElements(xtrans, path);
                                XElement newel = new(transel.Xelement.Name.LocalName);
                                foreach (var attr in transel.Xelement.Attributes())
                                {
                                    newel.Add(new XAttribute(attr.Name.LocalName, attr.Value));
                                }

                                var idstring = string.Join(",",
                                    transel.Xelement.Attributes().Select(a => a.Name.LocalName)
                                        .Where(i => XmlHelper.idattributenames.Contains(i))
                                        .OrderBy(i => i));

                                var valuestring = string.Join(",",
                                    transel.Xelement.Attributes().Select(a => a.Name.LocalName)
                                        .Where(i => !XmlHelper.idattributenames.Contains(i))
                                        .OrderBy(i => i));

                                newel.Add(new XAttribute(ns + "Transform", $"SetAttributes({valuestring})"));
                                newel.Add(new XAttribute(ns + "Locator", $"Match({idstring})"));
                                parent.Add(newel);
                                break;
                            }

                        case Operation.remove:
                            {
                                //Console.WriteLine($"{path}: Delete");

                                var parent = XmlHelper.CreateParentElements(xtrans, path);
                                XElement newel = new(transel.Xelement.Name.LocalName);
                                foreach (var attr in transel.Xelement.Attributes())
                                {
                                    newel.Add(new XAttribute(attr.Name.LocalName, attr.Value));
                                }

                                var idstring = string.Join(",",
                                    transel.Xelement.Attributes().Select(a => a.Name.LocalName)
                                        .Where(i => XmlHelper.idattributenames.Contains(i))
                                        .OrderBy(i => i));

                                newel.Add(new XAttribute(ns + "Transform", "Remove"));
                                newel.Add(new XAttribute(ns + "Locator", $"Match({idstring})"));
                                parent.Add(newel);
                                break;
                            }
                    }
                }
            }

            xtrans.Save(transfile);
        }
    }
}
