using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Xml;
using System.Xml.Linq;
using System.Xml.XPath;

namespace CleanDiff
{
    class Program
    {
        static bool FlagRemoveComments, FlagSortAttributes, FlagSortElements, FlagCollapse, FlagWinDiff, FlagDontDiffIfEqual;

        static List<string> SearchPaths = [];

        static int Main(string[] args)
        {
            var usage =
                @"CleanDiff 1.2 - Compare normalized xml files

Usage: CleanDiff [flags] <filename1> <filename2>

Optional flags:
-DontRemoveComments  - Don't remove comments
-DontSortAttributes  - Don't sort attributes
-DontSortElements    - Don't sort elements
-DontCollapse        - Don't collapse empty elements
-DontWinDiff         - Don't start WinDiff
-DontDiffIfEqual     - Only start WinDiff if different.";

            if (args.Length < 2)
            {
                Console.WriteLine(usage);
                return -1;
            }

            for (var arg = 0; arg < args.Length - 2; arg++)
            {
                if (!CheckArg(args[arg]))
                {
                    Console.WriteLine(usage);
                    return -1;
                }
            }

            var file1 = args[^2];
            var file2 = args[^1];
            SetFlags(args, args.Length - 1);

            var windiffpath = FindWinDiff();
            if (windiffpath == null)
            {
                Console.WriteLine($"Couldn't find windiff.exe, the following paths was searched:{Environment.NewLine}" +
                    $"'{string.Join($"'{Environment.NewLine}'", SearchPaths)}'");
                return -1;
            }

            Console.WriteLine($"Using windiff: '{windiffpath}'");

            var result = DiffXml(windiffpath, file1, file2);

            return result ? 1 : 0;
        }

        static string FindWinDiff()
        {
            string[] windiffpaths =
            [
                Environment.CurrentDirectory,
                @"C:\Program Files (x86)\Microsoft SDKs\Windows\v7.0A\bin",
                @"C:\Utils"
            ];

            SearchPaths.AddRange(windiffpaths);

            SearchPaths.AddRange(Environment.GetEnvironmentVariable("path").Split(';'));

            var asspath = Assembly.GetExecutingAssembly().Location;
            SearchPaths.Add(Path.GetDirectoryName(asspath));

            foreach (var path in SearchPaths)
            {
                var windiffpath = Path.Combine(path, "windiff.exe");
                if (File.Exists(windiffpath))
                {
                    return windiffpath;
                }
            }

            return null;
        }

        static bool CheckArg(string arg)
        {
            string[] allowedFlags = ["-DontRemoveComments", "-DontSortAttributes", "-DontSortElements", "-DontCollapse", "-DontWinDiff", "-DontDiffIfEqual"];

            if (!allowedFlags.Contains(arg))
            {
                Console.WriteLine($"Unrecognized argument: '{arg}'");
                return false;
            }

            return true;
        }

        static void SetFlags(string[] args, int flags)
        {
            FlagRemoveComments = FlagSortAttributes = FlagSortElements = FlagCollapse = FlagWinDiff = FlagDontDiffIfEqual = true;

            for (var i = 0; i < flags; i++)
            {
                if (args[i] == "-DontRemoveComments")
                {
                    FlagRemoveComments = false;
                }
                if (args[i] == "-DontSortAttributes")
                {
                    FlagSortAttributes = false;
                }
                if (args[i] == "-DontSortElements")
                {
                    FlagSortElements = false;
                }
                if (args[i] == "-DontCollapse")
                {
                    FlagCollapse = false;
                }
                if (args[i] == "-DontWinDiff")
                {
                    FlagWinDiff = false;
                }
                if (args[i] == "-DontDiffIfEqual")
                {
                    FlagDontDiffIfEqual = false;
                }
            }
        }

        static string GetSemiUniqueFileName(string filepath, string ext)
        {
            for (var i = 1; i <= 10000; i++)
            {
                var fullfilename = $"{filepath}.CleanDiff{i}{ext}";
                if (File.GetLastWriteTime(fullfilename) < DateTime.Now.AddMinutes(-5))
                {
                    return fullfilename;
                }
            }

            throw new Exception("Please clean your temp folder!");
        }

        static bool DiffXml(string windiffpath, string file1, string file2)
        {
            var tempfolder = Path.GetTempPath();

            var outfile1 = GetSemiUniqueFileName(Path.Combine(tempfolder, Path.GetFileNameWithoutExtension(file1)), ".xml");
            if (!CleanFile(file1, outfile1))
            {
                return false;
            }

            var outfile2 = GetSemiUniqueFileName(Path.Combine(tempfolder, Path.GetFileNameWithoutExtension(file2)), ".xml");
            if (!CleanFile(file2, outfile2))
            {
                return false;
            }

            bool diff;

            var buf1 = File.ReadAllBytes(outfile1);
            var buf2 = File.ReadAllBytes(outfile2);

            IStructuralEquatable eqa1 = buf1;
            diff = !eqa1.Equals(buf2, StructuralComparisons.StructuralEqualityComparer);

            if (FlagDontDiffIfEqual || diff)
            {
                if (FlagWinDiff)
                {
                    Console.WriteLine($"Diffing: '{file1}' and '{file2}'");
                    _ = Process.Start(windiffpath, $"\"{outfile1}\" \"{outfile2}\"");
                }
            }

            return diff;
        }

        static bool CleanFile(string infile, string outfile)
        {
            XDocument xdoc;

            try
            {
                xdoc = XDocument.Load(infile);
            }
            catch (Exception ex) when (ex is IOException or XmlException)
            {
                Console.WriteLine(ex.Message);
                return false;
            }

            if (FlagRemoveComments)
            {
                List<XComment> comments = [.. ((IEnumerable)xdoc.XPathEvaluate("//comment()")).Cast<XComment>()];
                foreach (var comment in comments)
                {
                    comment.Remove();
                }
            }

            if (FlagCollapse)
            {
                CollapseEmptyElements(xdoc.Root);
            }

            if (FlagSortAttributes)
            {
                SortAttributes(xdoc.Root);
            }

            if (FlagSortElements)
            {
                SortElements(xdoc.Root);
            }

            xdoc.Save(outfile);

            return true;
        }

        static void SortAttributes(XElement xele)
        {
            Dictionary<XName, string> attrs = [];

            foreach (var xattr in xele.Attributes())
            {
                attrs.Add(xattr.Name, xattr.Value);
            }

            xele.RemoveAttributes();

            foreach (var attr in attrs.OrderBy(a => a.Key.LocalName))
            {
                xele.Add(new XAttribute(attr.Key, attr.Value));
            }

            foreach (var child in xele.Elements())
            {
                SortAttributes(child);
            }
        }

        static void SortElements(XElement xele)
        {
            XElement[] elements = [.. xele.Elements().OrderBy(GetUniqueElementValue)];

            foreach (var child in elements)
            {
                child.Remove();
            }

            foreach (var child in elements)
            {
                xele.Add(child);
            }

            foreach (var child in xele.Elements())
            {
                SortElements(child);
            }
        }

        static string GetUniqueElementValue(XElement xele)
        {
            return xele.Attributes().Any() ? xele.ToString() : $"<{xele.Name.LocalName}";
        }

        static void CollapseEmptyElements(XElement xele)
        {
            foreach (var ele in xele.Descendants().ToArray()) // Create a temp copy
            {
                if (!ele.IsEmpty && !ele.HasElements && ele.Value.Trim() == string.Empty)
                {
                    ele.AddAfterSelf(new XElement(ele.Name, ele.Attributes()));
                    ele.Remove();
                }
            }
        }
    }
}
