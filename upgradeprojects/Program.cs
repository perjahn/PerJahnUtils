﻿using System;
using System.IO;
using System.Linq;
using System.Xml;
using System.Xml.Linq;

namespace upgradeprojects
{
    class Program
    {
        static void Main(string[] args)
        {
            var rootpath = args.Length == 0 ? "." : args[0];

            var removeUselessPropertyGroups = !args.Contains("-DontRemoveUselessPropertyGroups");
            var removeUselessFiles = !args.Contains("-DontRemoveUselessFiles");
            var dryRun = args.Contains("-DryRun");

            Upgrade(rootpath, removeUselessPropertyGroups, dryRun);

            if (removeUselessFiles)
            {
                RemoveUselessFiles(rootpath, dryRun);
            }
        }

        static void RemoveUselessFiles(string rootpath, bool dryRun)
        {
            string[] files = [..
                Directory.GetFiles(rootpath, "App.config", SearchOption.AllDirectories).Concat(
                Directory.GetFiles(rootpath, "packages.config", SearchOption.AllDirectories).Concat(
                Directory.GetFiles(rootpath, "AssemblyInfo.cs", SearchOption.AllDirectories)))
                .Select(f => f.StartsWith($".{Path.DirectorySeparatorChar}") || f.StartsWith($".{Path.AltDirectorySeparatorChar}") ? f[2..] : f)];

            Array.Sort(files);

            foreach (var filename in files)
            {
                Console.WriteLine($"Deleting file: '{filename}'");
                if (!dryRun)
                {
                    File.Delete(filename);
                }
            }
        }

        static void Upgrade(string rootpath, bool removeUselessPropertyGroups, bool dryRun)
        {
            string[] solutionsFiles = [.. Directory.GetFiles(rootpath, "*.sln", SearchOption.AllDirectories)
                .Select(f => f.StartsWith($".{Path.DirectorySeparatorChar}") || f.StartsWith($".{Path.AltDirectorySeparatorChar}") ? f[2..] : f)];

            Console.WriteLine($"Found {solutionsFiles.Length} solutions.");

            foreach (var filename in solutionsFiles)
            {
                UpgradeSolution(filename, dryRun);
            }


            string[] projectFiles = [.. Directory.GetFiles(rootpath, "*.*proj", SearchOption.AllDirectories)
                .Select(f => f.StartsWith($".{Path.DirectorySeparatorChar}") || f.StartsWith($".{Path.AltDirectorySeparatorChar}") ? f[2..] : f)];

            Console.WriteLine($"Found {projectFiles.Length} projects.");

            foreach (var filename in projectFiles)
            {
                UpgradeProject(filename, removeUselessPropertyGroups, dryRun);
            }
        }

        static void UpgradeSolution(string filename, bool dryRun)
        {
            Console.WriteLine($"Reading: '{filename}'");

            var oldBytes = File.ReadAllBytes(filename);
            using MemoryStream ms1 = new(oldBytes);
            using StreamReader reader = new(ms1);
            var content = reader.ReadToEnd();

            content = content.Replace(
                "\nProject(\"{9A19103F-16F7-4668-BE54-9A1E7A4F7556}\")",
                "\nProject(\"{FAE04EC0-301F-11D3-BF4B-00C04F79EFBC}\")");

            using MemoryStream ms2 = new();
            using StreamWriter writer = new(ms2);
            writer.Write(content);
            writer.Flush();
            var bytes = ms2.ToArray();

            bytes = AddBom(bytes);

            if (bytes.SequenceEqual(oldBytes))
            {
                return;
            }

            Console.WriteLine($"Saving: '{filename}'");
            if (!dryRun)
            {
                File.WriteAllBytes(filename, bytes);
            }
        }

        static void UpgradeProject(string filename, bool removeUselessPropertyGroups, bool dryRun)
        {
            var result = LoadXDocument(filename, out byte[] oldBytes);
            if (result == null)
            {
                return;
            }
            var xdoc = result;

            xdoc.Descendants()
                .Attributes()
                .Where(x => x.IsNamespaceDeclaration)
                .Remove();

            foreach (var e in xdoc.Descendants())
            {
                e.Name = e.Name.LocalName;
            }

            xdoc.Root?.Attributes().Remove();

            XAttribute attribute = new("Sdk", "Microsoft.NET.Sdk");
            xdoc.Root?.Add(attribute);

            var references = xdoc.Elements("Project").Elements("ItemGroup").Elements("Reference");
            foreach (var reference in references)
            {
                reference.Name = "PackageReference";
                reference.Elements().Remove();
                var includeAttribute = reference.Attribute("Include");
                if (includeAttribute != null)
                {
                    string[] includeValues = [.. includeAttribute.Value.Split(',').Select(t => t.Trim()).Where(t => t != string.Empty)];

                    includeAttribute.Value = includeValues.Length == 0 ? string.Empty : includeValues[0];

                    var version = includeValues.FirstOrDefault(t => t.StartsWith("Version="));
                    if (version != null)
                    {
                        var value = version[8..].Trim();
                        XAttribute attr = new("Version", value);
                        reference.Add(attr);
                    }
                }
            }

            var projectReferences = xdoc.Elements("Project").Elements("ItemGroup").Elements("ProjectReference");
            foreach (var projectReference in projectReferences)
            {
                projectReference.Elements().Remove();
            }

            if (removeUselessPropertyGroups)
            {
                xdoc.Elements("Project").Elements("PropertyGroup")
                    .Where(e => e.Attributes("Condition").Any())
                    .Remove();
            }

            var firstTargetFrameworkVersion = xdoc.Elements("Project").Elements("PropertyGroup").Elements("TargetFrameworkVersion").FirstOrDefault();
            if (firstTargetFrameworkVersion != null)
            {
                firstTargetFrameworkVersion.Name = "TargetFramework";
                var oldValue = firstTargetFrameworkVersion.Value;
                string newValue = oldValue;
                if (newValue.StartsWith('v'))
                {
                    newValue = "net" + newValue[1..];
                }
                newValue = newValue.Replace(".", string.Empty);
                if (newValue != oldValue)
                {
                    Console.WriteLine($"TargetFrameworkVersion>{oldValue}< -> TargetFramework>{newValue}<");
                    firstTargetFrameworkVersion.Value = newValue;
                }
            }

            string[] removePropertyGroupChildren = [
                "AppDesignerFolder",
                "AssemblyName",
                "AutoGenerateBindingRedirects" ,
                "Configuration",
                "FileAlignment",
                "IsCodedUITest",
                "NuGetPackageImportStamp",
                "Platform",
                "ProjectGuid",
                "ProjectTypeGuids",
                "ReferencePath",
                "RootNamespace",
                "TargetFrameworkVersion",
                "TestProjectType",
                "VisualStudioVersion",
                "VSToolsPath" ];

            xdoc.Elements("Project").Elements("PropertyGroup").Elements()
                .Where(e => removePropertyGroupChildren.Contains(e.Name.LocalName))
                .Remove();

            xdoc.Root?.Elements("Import")
                .Remove();

            xdoc.DescendantNodes().OfType<XComment>()
                .Remove();

            xdoc.Elements("Project").Elements("ItemGroup").Elements("Compile")
                .Remove();

            xdoc.Elements("Project").Elements("ItemGroup").Elements()
                .Where(e => e.Attribute("Include")?.Value == "packages.config")
                .Remove();

            xdoc.Elements("Project").Elements("ItemGroup")
                .Where(i => !i.HasElements)
                .Remove();

            SaveXDocument(xdoc, filename, oldBytes, dryRun);
        }

        static XDocument? LoadXDocument(string filename, out byte[] bytes)
        {
            Console.WriteLine($"Reading: '{filename}'");
            try
            {
                bytes = File.ReadAllBytes(filename);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Couldn't read xmlfile, ignoring: '{filename}': {ex.Message}");
                bytes = [];
                return null;
            }

            XDocument xdoc;

            try
            {
                using MemoryStream ms = new(bytes);
                xdoc = XDocument.Load(ms);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Couldn't parse xmlfile, ignoring: '{filename}': {ex.Message}");
                return null;
            }

            return xdoc;
        }

        static void SaveXDocument(XDocument xdoc, string filename, byte[] oldBytes, bool dryRun)
        {
            var settings = new XmlWriterSettings
            {
                OmitXmlDeclaration = true,
                Indent = true
            };

            using MemoryStream ms = new();
            using var xw = XmlWriter.Create(ms, settings);
            xdoc.Save(xw);
            xw.Flush();

            var bytes = ms.ToArray();

            bytes = RemoveBom(bytes);

            if (bytes.SequenceEqual(oldBytes))
            {
                return;
            }

            Console.WriteLine($"Saving: '{filename}'");
            if (!dryRun)
            {
                File.WriteAllBytes(filename, bytes);
            }
        }

        static byte[] AddBom(byte[] bytes)
        {
            var utf8bom = new byte[] { 0xEF, 0xBB, 0xBF };

            return bytes[0] == utf8bom[0] && bytes[1] == utf8bom[1] && bytes[2] == utf8bom[2] ? bytes : [.. utf8bom, .. bytes];
        }

        static byte[] RemoveBom(byte[] bytes)
        {
            var utf8bom = new byte[] { 0xEF, 0xBB, 0xBF };

            return bytes[0] == utf8bom[0] && bytes[1] == utf8bom[1] && bytes[2] == utf8bom[2] ? [.. bytes.Skip(3)] : bytes;
        }
    }
}
