using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace GenerateTransform
{
    public enum operation { insert, remove, update };
    class TransformElement
    {
        public operation op;
        public string path { get; set; }
        public XElement xelement { get; set; }
    }

    class Program
    {
        static int Main(string[] args)
        {
            if (args.Length == 1 && args[0] == "-unittest")
            {
                UnitTest tests = new UnitTest();
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
            List<TransformElement> transformElements = new List<TransformElement>();

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

            XElement[] elements = xsource
                .Descendants()
                .Concat(xtarget.Descendants())
                .ToArray();

            XElement[] sourceElements = xsource
                .Descendants()
                .ToArray();

            XElement[] targetElements = xtarget
                .Descendants()
                .ToArray();

            Console.WriteLine("Total elements: " + elements.Length + " (" + sourceElements.Length + " + " + targetElements.Length + ")");

            string[] elementpaths = elements
                .Select(e => XmlHelper.GetElementPath(e))
                .Distinct()
                .OrderBy(p => p)
                .ToArray();

            Console.WriteLine("Distinct paths: " + elementpaths.Length);

            foreach (string elementpath in elementpaths)
            {
                sourceElements = xsource
                    .Descendants()
                    .Where(el => XmlHelper.GetElementPath(el) == elementpath)
                    .ToArray();

                targetElements = xtarget
                    .Descendants()
                    .Where(el => XmlHelper.GetElementPath(el) == elementpath)
                    .ToArray();

                foreach (XElement targetel in targetElements)
                {
                    int b = 123;
                    if (targetel.Name.LocalName == "add" && targetel.Attribute("name") != null && targetel.Attribute("name").Value == "membershipProvider")
                    {
                        b++;
                    }

                    // If exist, update. Else add.

                    XElement[] matchingElements = sourceElements
                        .Where(sourceel => XmlHelper.AreEqualId(targetel, sourceel))
                        .ToArray();
                    if (matchingElements.Length > 1)
                    {
                        // Dont know which sourceel targetel match.
                        Console.WriteLine("Ignoring element: " + elementpath + ", matching too many elements: " + matchingElements.Length);
                        continue;
                    }
                    else if (matchingElements.Length == 1)
                    {
                        if (!XmlHelper.AreEqual(targetel, matchingElements[0]))
                        {
                            //Console.WriteLine("Updating: " + elementpath);
                            Console.Write("u");
                            TransformElement updateel = new TransformElement();
                            updateel.op = operation.update;
                            updateel.path = elementpath;
                            updateel.xelement = new XElement(targetel.Name.LocalName, targetel.Attributes());
                            transformElements.Add(updateel);
                        }
                    }
                    else
                    {
                        if (!sourceElements.Any(sourceel => XmlHelper.AreEqual(sourceel, targetel)))
                        {
                            //Console.WriteLine("Insert: " + elementpath);
                            Console.Write("i");
                            TransformElement insertel = new TransformElement();
                            insertel.op = operation.insert;
                            insertel.path = elementpath;
                            insertel.xelement = new XElement(targetel.Name.LocalName, targetel.Attributes());
                            transformElements.Add(insertel);
                        }
                    }
                }

                foreach (XElement sourceel in sourceElements)
                {
                    if (!targetElements.Any(targetel => XmlHelper.AreEqualId(sourceel, targetel)))
                    {
                        Console.Write("d");
                        //Console.WriteLine("Delete: " + elementpath);
                        TransformElement removeel = new TransformElement();
                        removeel.op = operation.remove;
                        removeel.path = elementpath;
                        removeel.xelement = new XElement(sourceel.Name.LocalName,
                            sourceel.Attributes().Where(a => XmlHelper.idattributenames.Contains(a.Name.LocalName)).OrderBy(a => a));
                        transformElements.Add(removeel);
                    }
                }
            }


            WriteElements(transformElements, transfile);
        }

        private static void WriteElements(List<TransformElement> transformElements, string transfile)
        {
            XNamespace ns = @"http://schemas.microsoft.com/XML-Document-Transform";
            XDocument xtrans = new XDocument();

            string[] paths = transformElements.Select(el => el.path).Distinct().ToArray();

            Console.WriteLine("Transform elements: " + transformElements.Count() + ", paths: " + paths.Length);
            foreach (string path in paths)
            {
                foreach (TransformElement transel in transformElements
                    .Where(el => el.path == path)
                    .OrderBy(el => el.xelement, new XElementComparer()))
                {
                    switch (transel.op)
                    {
                        case operation.insert:
                            {
                                //Console.WriteLine($"{path}: Insert");

                                XContainer parent = XmlHelper.CreateParentElements(xtrans, path);
                                XElement newel = new XElement(transel.xelement.Name.LocalName);
                                foreach (XAttribute attr in transel.xelement.Attributes())
                                {
                                    newel.Add(new XAttribute(attr.Name.LocalName, attr.Value));
                                }
                                newel.Add(new XAttribute(ns + "Transform", "Insert"));
                                parent.Add(newel);
                                break;
                            }

                        case operation.update:
                            {
                                //Console.WriteLine($"{path}: Update");

                                XContainer parent = XmlHelper.CreateParentElements(xtrans, path);
                                XElement newel = new XElement(transel.xelement.Name.LocalName);
                                foreach (XAttribute attr in transel.xelement.Attributes())
                                {
                                    newel.Add(new XAttribute(attr.Name.LocalName, attr.Value));
                                }

                                string idstring = string.Join(",",
                                    transel.xelement.Attributes().Select(a => a.Name.LocalName)
                                        .Where(i => XmlHelper.idattributenames.Contains(i))
                                        .OrderBy(i => i));

                                string valuestring = string.Join(",",
                                    transel.xelement.Attributes().Select(a => a.Name.LocalName)
                                        .Where(i => !XmlHelper.idattributenames.Contains(i))
                                        .OrderBy(i => i));

                                newel.Add(new XAttribute(ns + "Transform", $"SetAttributes({valuestring})"));
                                newel.Add(new XAttribute(ns + "Locator", $"Match({idstring})"));
                                parent.Add(newel);
                                break;
                            }

                        case operation.remove:
                            {
                                //Console.WriteLine($"{path}: Delete");

                                XContainer parent = XmlHelper.CreateParentElements(xtrans, path);
                                XElement newel = new XElement(transel.xelement.Name.LocalName);
                                foreach (XAttribute attr in transel.xelement.Attributes())
                                {
                                    newel.Add(new XAttribute(attr.Name.LocalName, attr.Value));
                                }

                                string idstring = string.Join(",",
                                    transel.xelement.Attributes().Select(a => a.Name.LocalName)
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

            return;
        }
    }
}
