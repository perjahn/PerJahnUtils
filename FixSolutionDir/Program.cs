using System;
using System.IO;
using System.Xml;
using System.Xml.Linq;

namespace FixSolutionDir
{
    class Program
    {
        static void Main(string[] args)
        {
            var usage =
@"FixSolutionDir 0.001 gamma - Replaces $(SolutionDir) in PostBuildEvent with current dir.

Usage: FixSolutionDir <projectfile>";

            if (args.Length != 1)
            {
                Console.WriteLine(usage);
                return;
            }

            FixSolutionDir(args[0]);
        }

        static void FixSolutionDir(string filename)
        {
            XDocument xdoc;

            try
            {
                xdoc = XDocument.Load(filename);
            }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or ArgumentException or XmlException)
            {
                Console.Write($"Couldn't load project: '{filename}': {ex.Message}");
                return;
            }

            var ns = xdoc.Root.Name.Namespace;

            XElement[] events = [.. xdoc.Descendants(ns + "PostBuildEvent")];

            var modified = false;

            foreach (var eventnode in events)
            {
                var innertext = eventnode.Value;

                eventnode.Value = innertext.Replace("$(SolutionDir)", Environment.CurrentDirectory + Path.DirectorySeparatorChar);
                if (eventnode.Value != innertext)
                {
                    modified = true;
                }
            }

            if (modified)
            {
                Console.WriteLine($"Updating: '{filename}'");

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
