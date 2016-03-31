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
        public string _sln_path { get; set; }

        public string _proj_guid { get; set; }
        public List<string> _ProjectTypeGuids { get; set; }

        public List<string> _proj_guids { get; set; }

        public List<OutputPath> _outputpaths { get; set; }
        public List<OutputPath> _outdirs { get; set; }
        public List<Reference> _projectReferences { get; set; }

        public static Project LoadProject(string solutionfile, string projectfilepath)
        {
            Project newproj = new Project();
            XDocument xdoc;
            XNamespace ns;

            string fullfilename = Path.Combine(Path.GetDirectoryName(solutionfile), projectfilepath);

            try
            {
                xdoc = XDocument.Load(fullfilename);
            }
            catch (Exception ex) when (ex is IOException || ex is UnauthorizedAccessException || ex is ArgumentException || ex is System.Xml.XmlException)
            {
                ConsoleHelper.ColorWrite(ConsoleColor.Red, "Couldn't load project: '" + fullfilename + "': " + ex.Message);
                return null;
            }

            ns = xdoc.Root.Name.Namespace;


            try
            {
                newproj._proj_guids = xdoc
                    .Element(ns + "Project")
                    .Elements(ns + "PropertyGroup")
                    .Elements(ns + "ProjectGuid")
                    .Select(g => g.Value)
                    .ToList();
            }
            catch (System.NullReferenceException)
            {
                ConsoleHelper.ColorWrite(ConsoleColor.Red, "Couldn't load project: '" + fullfilename + "': Missing ProjectGuid.");
                return null;
            }
            try
            {
                newproj._ProjectTypeGuids = xdoc
                    .Element(ns + "Project")
                    .Elements(ns + "PropertyGroup")
                    .Elements(ns + "ProjectTypeGuids")
                    .SelectMany(g => g.Value.Split(';'))
                    .ToList();
            }
            catch (System.NullReferenceException)
            {
                ConsoleHelper.ColorWrite(ConsoleColor.Red, "Couldn't load project: '" + fullfilename + "': Missing ProjectTypeGuids.");
                return null;
            }


            newproj._outputpaths =
                (from el in xdoc.Element(ns + "Project").Elements(ns + "PropertyGroup").Elements(ns + "OutputPath")
                 select new OutputPath() { Condition = GetCondition(el.Parent), Path = el.Value })
                .ToList();

            newproj._outdirs =
                (from el in xdoc.Element(ns + "Project").Elements(ns + "PropertyGroup").Elements(ns + "OutDir")
                 select new OutputPath() { Condition = GetCondition(el.Parent), Path = el.Value })
                .ToList();


            newproj._projectReferences =
                (from el in xdoc.Element(ns + "Project").Elements(ns + "ItemGroup").Elements(ns + "ProjectReference")
                 where el.Attribute("Include") != null
                 orderby Path.GetFileNameWithoutExtension(el.Attribute("Include").Value)
                 select new Reference
                 {
                     include = el.Attribute("Include").Value,
                     shortinclude = Path.GetFileNameWithoutExtension(el.Attribute("Include").Value),
                     names = (from elName in el.Elements(ns + "Name")
                              orderby elName.Value
                              select elName.Value).ToList(),
                     name = null
                 })
                .ToList();


            return newproj;
        }

        private static string GetCondition(XElement el)
        {
            XAttribute xattr = el.Attribute("Condition");
            if (xattr == null)
            {
                return null;
            }

            return xattr.Value;
        }

        public void Compact()
        {
            if (_proj_guids.Count > 1)
            {
                ConsoleHelper.ColorWrite(ConsoleColor.Yellow,
                    "Warning: Corrupt project file: " + _sln_path +
                    ", multiple guids: '" + _proj_guids.Count +
                    "', compacting HintPath elements.");
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
                    ConsoleHelper.ColorWrite(ConsoleColor.Yellow,
                        "Warning: Corrupt project file: " + _sln_path +
                        ", project reference: '" + projref.include +
                        "', compacting Name elements.");
                }
                if (projref.names.Count >= 1)
                {
                    projref.name = projref.names[0];
                    projref.names = null;
                }
            }

            return;
        }

        public void FixVariables(string solutionfile, string buildconfig)
        {
            string SolutionDir = Path.GetDirectoryName(solutionfile);

            foreach (var path in _outputpaths)
            {
                path.Path = path.Path
                        .Replace("$(SolutionDir)", SolutionDir + Path.DirectorySeparatorChar)
                        .Replace("$(Configuration)", buildconfig);
            }

            foreach (var path in _outdirs)
            {
                path.Path = path.Path
                        .Replace("$(SolutionDir)", SolutionDir + Path.DirectorySeparatorChar)
                        .Replace("$(Configuration)", buildconfig);
            }
        }

        public bool CopyOutput(string solutionfile, string buildconfig, string targetpath, bool verbose)
        {
            string path1, path2, path3;

            List<List<OutputPath>> matchresults = new List<List<OutputPath>>();

            path1 = GetDistinctPath(solutionfile, buildconfig, _outputpaths, matchresults);

            path2 = GetDistinctPath(solutionfile, buildconfig, _outdirs, matchresults);

            path3 = null;
            if (path1 == null && path2 == null)
            {
                string SolutionDir = Path.GetDirectoryName(solutionfile);
                path3 = Path.Combine(SolutionDir, buildconfig);
                if (!Directory.Exists(path3) || Directory.GetFiles(path3, "*", SearchOption.AllDirectories).Length == 0)
                {
                    path3 = null;
                }
            }

            string path = path1 ?? path2 ?? path3;


            if (path == null)
            {
                if (matchresults.All(m => m.Count() == 0))
                {
                    ConsoleHelper.ColorWrite(ConsoleColor.Red,
                        "'" + _sln_path + "': Couldn't find any matching OutputPath or OutDir in the project file. " +
                        "Also keep in mind, the specified buildconfig condition must match a single OutputPath or Outdir which contains actual files!");
                    return false;
                }
                else
                {
                    ConsoleHelper.ColorWrite(ConsoleColor.Red,
                        "'" + _sln_path + "': Couldn't find any unambiguous OutputPath or OutDir in the project file. " +
                        "You might want to specify a more narrow buildconfig condition. " +
                        "Also keep in mind, the specified buildconfig condition must match a single OutputPath or Outdir which contains actual files!");
                }

                if (verbose)
                {
                    ConsoleHelper.ColorWrite(ConsoleColor.Red, "  Specified buildconfig: >>>", false);
                    ConsoleHelper.ColorWrite(ConsoleColor.Green, buildconfig, false);
                    ConsoleHelper.ColorWrite(ConsoleColor.Red, "<<<");

                    ConsoleHelper.ColorWrite(ConsoleColor.Red, "  Possible matches:");
                    _outputpaths.ForEach(o =>
                    {
                        ConsoleHelper.ColorWrite(ConsoleColor.Red, "    OutputPath: >>>", false);
                        ConsoleHelper.ColorWrite(ConsoleColor.Green, o.Condition, false);
                        ConsoleHelper.ColorWrite(ConsoleColor.Red, "<<<");
                    });
                    _outdirs.ForEach(o =>
                    {
                        ConsoleHelper.ColorWrite(ConsoleColor.Red, "    OutDir: >>>", false);
                        ConsoleHelper.ColorWrite(ConsoleColor.Green, o.Condition, false);
                        ConsoleHelper.ColorWrite(ConsoleColor.Red, "<<<");
                    });

                    ConsoleHelper.ColorWrite(ConsoleColor.Red, "  Resulting matches:");
                    ConsoleHelper.ColorWrite(ConsoleColor.Red, "    OutputPath count (unstrict)  : " + matchresults[0].Count());
                    ConsoleHelper.ColorWrite(ConsoleColor.Red, "    OutputPath count (strict)    : " + matchresults[1].Count());
                    ConsoleHelper.ColorWrite(ConsoleColor.Red, "    OutputPath count (has files) : " + matchresults[2].Count());
                    ConsoleHelper.ColorWrite(ConsoleColor.Red, "    OutDir count (unstrict)      : " + matchresults[3].Count());
                    ConsoleHelper.ColorWrite(ConsoleColor.Red, "    OutDir count (strict)        : " + matchresults[4].Count());
                    ConsoleHelper.ColorWrite(ConsoleColor.Red, "    OutDir count (has files)     : " + matchresults[5].Count());
                    ConsoleHelper.ColorWrite(ConsoleColor.Red, "    VC default count (has files) : 0");
                }

                return false;
            }

            if (path.Contains("$(Configuration)") && buildconfig.Contains("|"))
            {
                string configuration = buildconfig.Split('|')[0];
                path = path.Replace("$(Configuration)", configuration);
            }

            string sourcepath = Path.Combine(Path.GetDirectoryName(solutionfile), Path.GetDirectoryName(_sln_path), path);

            ConsoleHelper.ColorWrite(ConsoleColor.Cyan, "Copying folder: '" + sourcepath + "' -> '" + targetpath + "'");

            return CopyFolder(new DirectoryInfo(sourcepath), new DirectoryInfo(targetpath));
        }

        private string GetDistinctPath(string solutionfile, string buildconfig, List<OutputPath> possiblepaths,
            List<List<OutputPath>> matchresults)
        {
            var paths = possiblepaths
                .Where(o => MatchCondition(o.Condition, buildconfig, false))
                .GroupBy(o => o.Path)
                .Select(g => g.First())
                .ToList();
            matchresults.Add(paths);

            if (paths.Count() > 1)
            {
                paths = possiblepaths
                    .Where(o => MatchCondition(o.Condition, buildconfig, true))
                    .GroupBy(o => o.Path)
                    .Select(g => g.First())
                    .ToList();
            }
            matchresults.Add(paths);

            if (paths.Count() > 1)
            {
                paths = possiblepaths
                    .Where(o => ContainsFiles(solutionfile, o.Path))
                    .GroupBy(o => o.Path)
                    .Select(g => g.First())
                    .ToList();
            }
            matchresults.Add(paths);

            if (paths.Count() == 1)
            {
                return paths.Single().Path;
            }

            return null;
        }

        //  <PropertyGroup Condition=" '$(Configuration)|$(Platform)' == 'Debug|AnyCPU' ">
        private bool MatchCondition(string condition, string buildconfig, bool strict)
        {
            int index = condition.IndexOf("==");
            if (index == -1)
            {
                ConsoleHelper.ColorWrite(ConsoleColor.Red, "'" + _sln_path + "': Malformed PropertyGroup Condition: '" + condition + "'");
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

        private bool ContainsFiles(string solutionfile, string outputpath)
        {
            string sourcepath = Path.Combine(Path.GetDirectoryName(solutionfile), Path.GetDirectoryName(_sln_path), outputpath);
            return Directory.Exists(sourcepath) && Directory.GetFiles(sourcepath, "*", SearchOption.AllDirectories).Length > 0;
        }

        private static bool CopyFolder(DirectoryInfo source, DirectoryInfo target)
        {
            if (!source.Exists)
            {
                ConsoleHelper.ColorWrite(ConsoleColor.Red, "Ignoring folder, it does not exist: '" + source.FullName + "'");
                return false;
            }

            if (!target.Exists)
            {
                Console.WriteLine("Creating folder: '" + target.FullName + "'");
                Directory.CreateDirectory(target.FullName);
            }

            foreach (FileInfo fi in source.GetFiles())
            {
                string sourcefile = fi.FullName;
                string targetfile = Path.Combine(target.FullName, fi.Name);
                Console.WriteLine("Copying file: '" + sourcefile + "' -> '" + targetfile + "'");
                File.Copy(sourcefile, targetfile, true);
            }

            foreach (DirectoryInfo di in source.GetDirectories())
            {
                DirectoryInfo targetSubdir = new DirectoryInfo(Path.Combine(target.FullName, di.Name));
                CopyFolder(di, targetSubdir);
            }

            return true;
        }
    }
}
