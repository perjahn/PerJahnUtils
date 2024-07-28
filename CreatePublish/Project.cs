﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml;
using System.Xml.Linq;

namespace CreatePublish
{
    class Project
    {
        public string _sln_shortfilename { get; set; }
        public string _sln_path { get; set; }

        public List<string> _ProjectTypeGuids { get; set; }

        public static Project LoadProject(string solutionfile, string projectfilepath)
        {
            Project newproj = new();
            XDocument xdoc;

            var fullfilename = Path.Combine(Path.GetDirectoryName(solutionfile), projectfilepath);

            try
            {
                xdoc = XDocument.Load(fullfilename);
            }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or ArgumentException or XmlException)
            {
                ConsoleHelper.ColorWrite(ConsoleColor.Red, $"Couldn't load project: '{fullfilename}': {ex.Message}");
                return null;
            }

            var ns = xdoc.Root.Name.Namespace;

            string[][] guidsarr = [.. xdoc
                .Element(ns + "Project").Elements(ns + "PropertyGroup").Elements(ns + "ProjectTypeGuids")
                .Select(el => el.Value.Split(';'))];

            newproj._ProjectTypeGuids = [];
            foreach (var guids in guidsarr)
            {
                foreach (var guid in guids)
                {
                    newproj._ProjectTypeGuids.Add(guid);
                }
            }

            return newproj;
        }
    }
}
