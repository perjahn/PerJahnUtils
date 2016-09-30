using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Xml.Linq;

namespace GatherOutputAssemblies
{
    class Project
    {
        public string _path { get; set; }
        public string[] _solutionfiles { get; set; }

        public string _proj_guid { get; set; }
        public List<string> _ProjectTypeGuids { get; set; }

        public List<string> _proj_guids { get; set; }

        public List<OutputPath> _outputpaths { get; set; }
        public List<OutputPath> _outdirs { get; set; }
        public List<Reference> _projectReferences { get; set; }

        public static Project LoadProject(string filename)
        {
            Project newproj = new Project();
            XDocument xdoc;
            XNamespace ns;

            newproj._path = filename;

            try
            {
                xdoc = XDocument.Load(newproj._path);
            }
            catch (Exception ex) when (ex is IOException || ex is UnauthorizedAccessException || ex is ArgumentException || ex is System.Xml.XmlException)
            {
                ConsoleHelper.ColorWriteLine(ConsoleColor.Red, "Couldn't load project: '" + newproj._path + "': " + ex.Message);
                return null;
            }

            ns = xdoc.Root.Name.Namespace;


            newproj._proj_guids = xdoc
                .Elements(ns + "Project")
                .Elements(ns + "PropertyGroup")
                .Elements(ns + "ProjectGuid")
                .Select(g => g.Value)
                .ToList();

            newproj._ProjectTypeGuids = xdoc
                .Elements(ns + "Project")
                .Elements(ns + "PropertyGroup")
                .Elements(ns + "ProjectTypeGuids")
                .SelectMany(g => g.Value.Split(';'))
                .ToList();

            newproj._outputpaths = xdoc
                .Elements(ns + "Project")
                .Elements(ns + "PropertyGroup")
                .Elements(ns + "OutputPath")
                .Select(el => new OutputPath() { Condition = GetCondition(el), Path = el.Value })
                .ToList();

            newproj._outdirs = xdoc
                .Elements(ns + "Project")
                .Elements(ns + "PropertyGroup")
                .Elements(ns + "OutDir")
                .Select(el => new OutputPath() { Condition = GetCondition(el), Path = el.Value })
                .ToList();

            newproj._projectReferences = xdoc
                .Elements(ns + "Project")
                .Elements(ns + "ItemGroup")
                .Elements(ns + "ProjectReference")
                .Where(el => el.Attribute("Include") != null)
                .OrderBy(el => Path.GetFileNameWithoutExtension(el.Attribute("Include").Value))
                .Select(el => new Reference
                {
                    include = el.Attribute("Include").Value,
                    shortinclude = Path.GetFileNameWithoutExtension(el.Attribute("Include").Value),
                    names = el
                        .Elements(ns + "Name")
                        .OrderBy(elName => elName.Value)
                        .Select(elName => elName.Value)
                        .ToList(),
                    name = null
                })
                .ToList();

            newproj.Compact();

            return newproj;
        }

        private static string GetCondition(XElement el)
        {
            XElement nearestconditionel = el
                .AncestorsAndSelf()
                .Where(a => a.Attribute("Condition") != null)
                .FirstOrDefault();

            if (nearestconditionel == null)
            {
                return null;
            }

            return nearestconditionel.Attribute("Condition").Value;
        }

        private void Compact()
        {
            if (_proj_guids.Count > 1)
            {
                ConsoleHelper.ColorWriteLine(ConsoleColor.Yellow,
                    "Warning: Corrupt project file: " + _path +
                    ", multiple guids: '" + _proj_guids.Count +
                    "', selecting first ProjectGuid element.");
            }
            if (_proj_guids.Count >= 1)
            {
                _proj_guid = _proj_guids[0];
                _proj_guids = null;
            }

            foreach (Reference projref in _projectReferences)
            {
                if (projref.names.Count > 1)
                {
                    ConsoleHelper.ColorWriteLine(ConsoleColor.Yellow,
                        "Warning: Corrupt project file: " + _path +
                        ", project reference: '" + projref.include +
                        "', selecting first Name in ProjectReference element.");
                }
                if (projref.names.Count >= 1)
                {
                    projref.name = projref.names[0];
                    projref.names = null;
                }
            }

            return;
        }

        public string GetOutputFolder(string buildconfig, bool verbose)
        {
            // Multiple outputpaths and outdirs can be defined in each project,
            // each with a solutiondir parameter in the path (and each project can
            // be included in multiple solutions residing in different folders).
            // I.e. there's a multitude of different possible paths, with falling
            // probability/priority, that binaries could have been compiled to.
            // If no outputpath/outdir has been defined, a folder named by the
            // buildconfig can have been generated by VS in the project folder.

            string outpath = GetDistinctPath(buildconfig);

            if (outpath == null)
            {
                if (verbose)
                {
                    ConsoleHelper.ColorWriteLine(ConsoleColor.Red,
                        _path + ": Couldn't find any matching OutputPath or OutDir in the project file. " +
                        "Also keep in mind, the specified buildconfig condition must match a single OutputPath or Outdir which contains actual files!");
                }
                else
                {
                    ConsoleHelper.ColorWriteLine(ConsoleColor.Red, "No useful OutputPath or OutDir found: '" + _path + "'");
                }
                return null;
            }

            string sourcepath = FileHelper.CompactPath(outpath);

            return sourcepath;
        }

        public OutputPath[] GetSolutionExpandedPaths(string buildconfig)
        {
            List<OutputPath> resultfolders = new List<OutputPath>();

            foreach (string solutiondir in _solutionfiles.Select(s => Path.GetDirectoryName(s)))
            {
                foreach (var path in _outputpaths)
                {
                    OutputPath p = path;
                    p.Path = p.Path
                        .Replace("$(SolutionDir)", solutiondir + Path.DirectorySeparatorChar)
                        .Replace("$(Configuration)", buildconfig.Split('|')[0]);

                    resultfolders.Add(p);
                }

                foreach (var path in _outdirs)
                {
                    OutputPath p = path;
                    p.Path = p.Path
                        .Replace("$(SolutionDir)", solutiondir + Path.DirectorySeparatorChar)
                        .Replace("$(Configuration)", buildconfig.Split('|')[0]);

                    resultfolders.Add(p);
                }
            }

            return resultfolders
                .Distinct()
                .ToArray();
        }

        private string GetDistinctPath(string buildconfig)
        {
            var solutionExpandedPaths = GetSolutionExpandedPaths(buildconfig);
            int count1 = 0;
            int count2 = 0;
            int count3 = 0;
            int count4 = 0;

            // 1. Test strict
            // 2. Test non-strict
            // 3. Test all
            // 4. Test folder named buildconfig

            // Release|AnyCPU
            List<string> paths = solutionExpandedPaths
                .Where(o => MatchCondition(o.Condition, buildconfig, true) && ContainsFiles(o.Path))
                .Select(o => o.Path)
                .Distinct()
                .ToList();
            count1 = paths.Count();
            if (count1 == 1)
            {
                return Path.Combine(Path.GetDirectoryName(_path), paths[0]);
            }


            // Release
            paths = solutionExpandedPaths
                .Where(o => MatchCondition(o.Condition, buildconfig, false) && ContainsFiles(o.Path))
                .Select(o => o.Path)
                .Distinct()
                .ToList();
            count2 = paths.Count();
            if (count2 == 1)
            {
                return Path.Combine(Path.GetDirectoryName(_path), paths[0]);
            }


            // buildconfig folder in project folder, default for vcx projects
            string projectsubfolder = Path.Combine(Path.GetDirectoryName(_path), buildconfig.Split('|')[0]);
            if (Directory.Exists(projectsubfolder))
            {
                return projectsubfolder;
            }
            count3 = 0;


            // Try all possible folders.
            paths = solutionExpandedPaths
                .Where(o => ContainsFiles(o.Path))
                .Select(o => o.Path)
                .Distinct()
                .ToList();
            count4 = paths.Count();
            if (count4 == 1)
            {
                return Path.Combine(Path.GetDirectoryName(_path), paths[0]);
            }


            if (paths.Count() == 0)
            {
                ConsoleHelper.ColorWriteLine(ConsoleColor.Yellow, _path +
                    ": Couldn't find any path (matched " + count1 + "," + count2 + "," + count3 + "," + count4 + ").");
            }

            if (paths.Count() > 1)
            {
                ConsoleHelper.ColorWriteLine(ConsoleColor.Yellow, _path +
                    ": Couldn't find distinct path (matched " + count1 + "," + count2 + "," + count3 + "," + count4 + ").");
            }

            return null;
        }

        //  <PropertyGroup Condition=" '$(Configuration)|$(Platform)' == 'Debug|AnyCPU' ">
        public bool MatchCondition(string condition, string buildconfig, bool strict)
        {
            if (condition == null)
            {
                return false;
            }

            int index = condition.IndexOf("==");
            if (index == -1)
            {
                ConsoleHelper.ColorWriteLine(ConsoleColor.Red, "'" + _path + "': Malformed PropertyGroup Condition: '" + condition + "'");
                return false;
            }
            string c = condition.Substring(index + 2).Trim().Trim('\'');

            if (strict)
            {
                return c == buildconfig;
            }
            else
            {
                string[] values = c.Split('|');
                return values.Contains(buildconfig);
            }
        }

        private bool ContainsFiles(string outputpath)
        {
            string sourcepath = Path.Combine(Path.GetDirectoryName(_path), outputpath);
            return Directory.Exists(sourcepath) && Directory.GetFiles(sourcepath, "*", SearchOption.AllDirectories).Length > 0;
        }
    }
}
