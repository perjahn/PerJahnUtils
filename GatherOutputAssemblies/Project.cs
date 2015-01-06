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

        public string _proj_assemblyname { get; set; }
        public string _proj_guid { get; set; }
        public string _proj_outputtype { get; set; }  // Not used, yet.
        public List<string> _ProjectTypeGuids { get; set; }

        // Compacted into non-List types after load.
        public List<string> _proj_assemblynames { get; set; }
        public List<string> _proj_guids { get; set; }
        public List<string> _proj_outputtypes { get; set; }

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
            catch (IOException ex)
            {
                ConsoleHelper.ColorWrite(ConsoleColor.Red, "Couldn't load project: '" + fullfilename + "': " + ex.Message);
                return null;
            }
            catch (UnauthorizedAccessException ex)
            {
                ConsoleHelper.ColorWrite(ConsoleColor.Red, "Couldn't load project: '" + fullfilename + "': " + ex.Message);
                return null;
            }
            catch (ArgumentException ex)
            {
                ConsoleHelper.ColorWrite(ConsoleColor.Red, "Couldn't load project: '" + fullfilename + "': " + ex.Message);
                return null;
            }
            catch (System.Xml.XmlException ex)
            {
                ConsoleHelper.ColorWrite(ConsoleColor.Red, "Couldn't load project: '" + fullfilename + "': " + ex.Message);
                return null;
            }

            ns = xdoc.Root.Name.Namespace;


            try
            {
                newproj._proj_assemblynames = xdoc.Element(ns + "Project").Elements(ns + "PropertyGroup").Elements(ns + "AssemblyName").Select(a => a.Value).ToList();
            }
            catch (System.NullReferenceException)
            {
                ConsoleHelper.ColorWrite(ConsoleColor.Red, "Couldn't load project: '" + fullfilename + "': Missing AssemblyName.");
                return null;
            }
            try
            {
                newproj._proj_guids = xdoc.Element(ns + "Project").Elements(ns + "PropertyGroup").Elements(ns + "ProjectGuid").Select(g => g.Value).ToList();
            }
            catch (System.NullReferenceException)
            {
                ConsoleHelper.ColorWrite(ConsoleColor.Red, "Couldn't load project: '" + fullfilename + "': Missing ProjectGuid.");
                return null;
            }
            try
            {
                newproj._proj_outputtypes = xdoc.Element(ns + "Project").Elements(ns + "PropertyGroup").Elements(ns + "OutputType").Select(o => o.Value).ToList();
            }
            catch (System.NullReferenceException)
            {
                ConsoleHelper.ColorWrite(ConsoleColor.Red, "Couldn't load project: '" + fullfilename + "': Missing OutputType.");
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
                 select new OutputPath() { Condition = GetCondition(el), Path = el.Value })
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
            if (_proj_assemblynames.Count > 1)
            {
                ConsoleHelper.ColorWrite(ConsoleColor.Yellow,
                    "Warning: Corrupt project file: " + _sln_path +
                    ", multiple assembly names: '" + _proj_assemblynames.Count +
                    "', compacting Name elements.");
            }
            if (_proj_assemblynames.Count >= 1)
            {
                _proj_assemblyname = _proj_assemblynames[0];
                _proj_assemblynames = null;
            }

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

            if (_proj_outputtypes.Count > 1)
            {
                ConsoleHelper.ColorWrite(ConsoleColor.Yellow,
                    "Warning: Corrupt project file: " + _sln_path +
                    ", multiple output types: '" + _proj_outputtypes.Count +
                    "', compacting Private elements.");
            }
            if (_proj_outputtypes.Count >= 1)
            {
                _proj_outputtype = _proj_outputtypes[0];
                _proj_outputtypes = null;
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

        public bool CopyOutput(string solutionfile, string buildconfig, string targetpath, bool verbose)
        {
            var outputpaths = _outputpaths.Where(o => MatchCondition(o.Condition, buildconfig, false));
            int debug1 = outputpaths.Count();

            if (outputpaths.Count() != 1)
            {
                outputpaths = _outputpaths.Where(o => MatchCondition(o.Condition, buildconfig, true));
            }
            int debug2 = outputpaths.Count();

            if (outputpaths.Count() != 1)
            {
                outputpaths = _outputpaths
                    .Where(o => ContainsFiles(solutionfile, o.Path))
                    .GroupBy(o => o.Path)
                    .Select(g => g.First());
            }
            int debug3 = outputpaths.Count();


            var outdirs = _outdirs.Where(o => MatchCondition(o.Condition, buildconfig, false));
            int debug4 = outdirs.Count();

            if (outdirs.Count() != 1)
            {
                outdirs = _outdirs.Where(o => MatchCondition(o.Condition, buildconfig, true));
            }
            int debug5 = outdirs.Count();

            if (outdirs.Count() != 1)
            {
                outdirs = _outdirs
                    .Where(o => ContainsFiles(solutionfile, o.Path))
                    .GroupBy(o => o.Path)
                    .Select(g => g.First());
            }
            int debug6 = outdirs.Count();


            if (outputpaths.Count() != 1 && outdirs.Count() != 1)
            {
                if (debug1 == 0 && debug2 == 0 && debug3 == 0 && debug4 == 0 && debug5 == 0 && debug6 == 0)
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
                    ConsoleHelper.ColorWrite(ConsoleColor.Red, "    OutputPath count (unstrict)  : " + debug1);
                    ConsoleHelper.ColorWrite(ConsoleColor.Red, "    OutputPath count (strict)    : " + debug2);
                    ConsoleHelper.ColorWrite(ConsoleColor.Red, "    OutputPath count (has files) : " + debug3);
                    ConsoleHelper.ColorWrite(ConsoleColor.Red, "    OutDir count (unstrict)      : " + debug4);
                    ConsoleHelper.ColorWrite(ConsoleColor.Red, "    OutDir count (strict)        : " + debug5);
                    ConsoleHelper.ColorWrite(ConsoleColor.Red, "    OutDir count (has files)     : " + debug6);
                }

                return false;
            }


            string path;
            if (outputpaths.Count() == 1)
            {
                path = outputpaths.Single().Path;
            }
            else
            {
                path = outdirs.Single().Path;
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
