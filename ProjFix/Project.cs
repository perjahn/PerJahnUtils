﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml;
using System.Xml.Linq;

namespace ProjFix
{
    class Project
    {
        public string _sln_package { get; set; }
        public string _sln_shortfilename { get; set; }
        public string _sln_guid { get; set; }
        public string _sln_path { get; set; }

        public string _proj_assemblyname { get; set; }
        public string _proj_guid { get; set; }
        public string _proj_outputtype { get; set; }  // Not used, yet.

        public List<string> _proj_assemblynames { get; set; }  // Compacted into _proj_assemblyname after load.
        public List<string> _proj_guids { get; set; }  // Compacted into _proj_guid after load.
        public List<string> _proj_outputtypes { get; set; }  // Compacted into _proj_outputtype after load.

        public List<string> _outputpaths { get; set; }
        public List<AssemblyRef> _references { get; set; }
        public List<ProjectRef> _projectReferences { get; set; }

        public bool _modified { get; set; }

        public void Restore(string solutionfile)
        {
            var fullfilename = Path.Combine(Path.GetDirectoryName(solutionfile), _sln_path);
            var bakfile = $"{fullfilename}.bak.xml";
            if (File.Exists(bakfile))
            {
                ConsoleHelper.WriteLine($"'{bakfile}' -> '{fullfilename}'", false);
                if (File.Exists(fullfilename))
                {
                    FileHelper.RemoveRO(fullfilename);
                    File.Delete(fullfilename);
                }
                File.Move(bakfile, fullfilename);
            }
        }

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

            try
            {
                newproj._proj_assemblynames = [.. xdoc
                    .Elements(ns + "Project")
                    .Elements(ns + "PropertyGroup")
                    .Elements(ns + "AssemblyName")
                    .Select(a => a.Value)];
            }
            catch (NullReferenceException)
            {
                ConsoleHelper.ColorWrite(ConsoleColor.Red, $"Couldn't load project: '{fullfilename}': Missing AssemblyName.");
                return null;
            }

            try
            {
                newproj._proj_guids = [.. xdoc
                    .Elements(ns + "Project")
                    .Elements(ns + "PropertyGroup")
                    .Elements(ns + "ProjectGuid")
                    .Select(g => g.Value)];
            }
            catch (NullReferenceException)
            {
                ConsoleHelper.ColorWrite(ConsoleColor.Red, $"Couldn't load project: '{fullfilename}': Missing ProjectGuid.");
                return null;
            }

            try
            {
                newproj._proj_outputtypes = [.. xdoc
                    .Elements(ns + "Project")
                    .Elements(ns + "PropertyGroup")
                    .Elements(ns + "OutputType")
                    .Select(o => o.Value)];
            }
            catch (NullReferenceException)
            {
                ConsoleHelper.ColorWrite(ConsoleColor.Red, $"Couldn't load project: '{fullfilename} ': Missing OutputType.");
                return null;
            }

            newproj._outputpaths = [.. xdoc
                .Elements(ns + "Project")
                .Elements(ns + "PropertyGroup")
                .Elements(ns + "OutputPath")
                .Select(el => el.Value)];

            newproj._references = [.. xdoc
                .Elements(ns + "Project")
                .Elements(ns + "ItemGroup")
                .Elements(ns + "Reference")
                .Where(el => el.Attribute("Include") != null)
                .OrderBy(el => GetShortRef(el.Attribute("Include").Value))
                .Select(el => new AssemblyRef
                {
                    Include = el.Attribute("Include").Value,
                    Shortinclude = GetShortRef(el.Attribute("Include").Value),
                    Names = [.. el.Elements(ns + "Name")
                        .OrderBy(elName => elName.Value)
                        .Select(elName => elName.Value)],
                    Hintpaths = [.. el.Elements(ns + "HintPath")
                        .OrderBy(elHintPath => elHintPath.Value)
                        .Select(elHintPath => elHintPath.Value)],
                    Copylocals = [.. el.Elements(ns + "Private")
                        .OrderBy(elName => elName.Value)
                        .Select(elName => elName.Value)],
                    Name = null,
                    Hintpath = null,
                    Copylocal = null
                })];

            newproj._projectReferences = [.. xdoc
                .Elements(ns + "Project")
                .Elements(ns + "ItemGroup")
                .Elements(ns + "ProjectReference")
                .Where(el => el.Attribute("Include") != null)
                .OrderBy(el => Path.GetFileNameWithoutExtension(el.Attribute("Include").Value))
                .Select(el => new ProjectRef
                {
                    Include = el.Attribute("Include").Value,
                    Shortinclude = Path.GetFileNameWithoutExtension(el.Attribute("Include").Value),
                    Names = [.. el.Elements(ns + "Name")
                        .OrderBy(elName => elName.Value)
                        .Select(elName => elName.Value)],
                    Projects = [.. el.Elements(ns + "Project")
                        .OrderBy(elProject => elProject.Value)
                        .Select(elProject => elProject.Value)],
                    Packages = [.. el.Elements(ns + "Package")
                        .OrderBy(elPackage => elPackage.Value)
                        .Select(elPackage => elPackage.Value)],
                    Name = null,
                    Project = null,
                    Package = null
                })];

            return newproj;
        }

        // Vendor.Product.Something, Version=1.2.3.4, Culture=neutral, processorArchitecture=MSIL
        // ->
        // Vendor.Product.Something
        public static string GetShortRef(string s)
        {
            return s.Split(',')[0];
        }

        public void Compact()
        {
            // _proj_assemblynames -> _proj_assemblyname
            // _proj_guids         -> _proj_guid
            // _proj_outputtypes   -> _proj_outputtype

            if (_proj_assemblynames.Count > 1)
            {
                ConsoleHelper.ColorWrite(ConsoleColor.Yellow,
                    $"Warning: Corrupt project file: {_sln_path}, multiple assembly names: '{_proj_assemblynames.Count}', compacting Name elements.");
                _modified = true;
            }
            if (_proj_assemblynames.Count >= 1)
            {
                _proj_assemblyname = _proj_assemblynames[0];
                _proj_assemblynames = null;
            }

            if (_proj_guids.Count > 1)
            {
                ConsoleHelper.ColorWrite(ConsoleColor.Yellow,
                    $"Warning: Corrupt project file: {_sln_path}, multiple guids: '{_proj_guids.Count}', compacting ProjectGuid elements.");
                _modified = true;
            }
            if (_proj_guids.Count >= 1)
            {
                _proj_guid = _proj_guids[0];
                _proj_guids = null;
            }

            if (_proj_outputtypes.Count > 1)
            {
                ConsoleHelper.ColorWrite(ConsoleColor.Yellow,
                    $"Warning: Corrupt project file: {_sln_path}, multiple output types: '{_proj_outputtypes.Count}', compacting Private elements.");
                _modified = true;
            }
            if (_proj_outputtypes.Count >= 1)
            {
                _proj_outputtype = _proj_outputtypes[0];
                _proj_outputtypes = null;
            }
        }

        public void CompactRefs()
        {
            foreach (var assref in _references)
            {
                if (assref.Names.Count > 1)
                {
                    ConsoleHelper.ColorWrite(ConsoleColor.Yellow,
                        $"Warning: Corrupt project file: {_sln_path}, reference: '{assref.Include}', compacting Name elements.");
                    _modified = true;
                }
                if (assref.Names.Count >= 1)
                {
                    assref.Name = assref.Names[0];
                    assref.Names = null;
                }

                if (assref.Hintpaths.Count > 1)
                {
                    ConsoleHelper.ColorWrite(ConsoleColor.Yellow,
                        $"Warning: Corrupt project file: {_sln_path}, reference: '{assref.Include}', compacting HintPath elements.");
                    _modified = true;
                }
                if (assref.Hintpaths.Count >= 1)
                {
                    assref.Hintpath = assref.Hintpaths[0];
                    assref.Hintpaths = null;
                }

                if (assref.Copylocals.Count > 1)
                {
                    ConsoleHelper.ColorWrite(ConsoleColor.Yellow,
                        $"Warning: Corrupt project file: {_sln_path}, reference: '{assref.Include}', compacting Private elements.");
                    _modified = true;
                }
                if (assref.Copylocals.Count >= 1)
                {
                    if (bool.TryParse(assref.Copylocals[0], out bool b))
                    {
                        assref.Copylocal = b;
                    }
                    assref.Copylocals = null;
                }
            }

            foreach (var projref in _projectReferences)
            {
                if (projref.Names.Count > 1)
                {
                    ConsoleHelper.ColorWrite(ConsoleColor.Yellow,
                        $"Warning: Corrupt project file: {_sln_path}, project reference: '{projref.Include}', compacting Name elements.");
                    _modified = true;
                }
                if (projref.Names.Count >= 1)
                {
                    projref.Name = projref.Names[0];
                    projref.Names = null;
                }

                if (projref.Projects.Count > 1)
                {
                    ConsoleHelper.ColorWrite(ConsoleColor.Yellow,
                        $"Warning: Corrupt project file: {_sln_path}, project reference: '{projref.Include}', compacting Project elements.");
                    _modified = true;
                }
                if (projref.Projects.Count >= 1)
                {
                    projref.Project = projref.Projects[0];
                    projref.Projects = null;
                }

                if (projref.Packages.Count > 1)
                {
                    ConsoleHelper.ColorWrite(ConsoleColor.Yellow,
                        $"Warning: Corrupt project file: {_sln_path}, project reference: '{projref.Include}', compacting Package elements.");
                    _modified = true;
                }
                if (projref.Packages.Count >= 1)
                {
                    projref.Package = projref.Packages[0];
                    projref.Packages = null;
                }
            }
        }

        public bool Validate(string solutionfile, List<Project> projects)
        {
            var valid = true;

            if (_sln_guid != null && _proj_guid != null && string.Compare(_sln_guid, _proj_guid, true) != 0)
            {
                ConsoleHelper.ColorWrite(ConsoleColor.Red,
                    $"Mismatched guid for project '{_sln_path}': Guid in solution: '{_sln_guid}'. Guid in project: '{_proj_guid}'.");
                valid = false;
            }
            if (string.Compare(_sln_shortfilename, Path.GetFileNameWithoutExtension(_sln_path), true) != 0)
            {
                ConsoleHelper.ColorWrite(ConsoleColor.Red,
                    $"Mismatched name for project '{_sln_path}': Project name in solution: '{_sln_shortfilename}'. File name: '{Path.GetFileNameWithoutExtension(_sln_path)}'.");
                valid = false;
            }

            CheckName(_sln_path, _proj_assemblyname);

            var afterthis = false;
            foreach (var proj in projects.OrderBy(p => p._sln_path))
            {
                if (proj == this)
                {
                    afterthis = true;
                    continue;
                }
                if (!afterthis)
                {
                    continue;
                }

                if (_proj_assemblyname == proj._proj_assemblyname)
                {
                    ConsoleHelper.ColorWrite(ConsoleColor.Red,
                        $"Error: Projects have identical assembly names: '{_proj_assemblyname}': '{_sln_path}' and '{proj._sln_path}'.");
                    valid = false;
                }
            }

            foreach (var assref in _references)
            {
                if (string.Compare(assref.Shortinclude, _proj_assemblyname, true) == 0 ||
                    string.Compare(assref.Shortinclude, _sln_shortfilename, true) == 0)
                {
                    ConsoleHelper.ColorWrite(ConsoleColor.Red,
                        $"Error: Project have reference to itself: '{_sln_path}'. Reference: '{assref.Shortinclude}'.");
                    valid = false;
                }

                // This might bail on unknown project types which later could have been converted
                // to project references. In those cases a warning should have been enough.
                if (assref.Hintpath != null)
                {
                    var path = assref.Hintpath;

                    string[] exts = [".dll", ".exe"];
                    if (!exts.Any(e => path.EndsWith(e, StringComparison.InvariantCultureIgnoreCase)))
                    {
                        ConsoleHelper.ColorWrite(ConsoleColor.Red,
                            $"Error: Invalid reference type: '{_sln_path}'. Ext: '{Path.GetExtension(path)}'. Path: '{path}'.");
                        valid = false;
                    }
                }
            }

            foreach (var projref in _projectReferences)
            {
                if (string.Compare(projref.Shortinclude, _proj_assemblyname, true) == 0 ||
                    string.Compare(projref.Shortinclude, _sln_shortfilename, true) == 0)
                {
                    ConsoleHelper.ColorWrite(ConsoleColor.Red,
                        $"Error: Project have reference to itself: '{_sln_path}'. Project reference: '{projref.Shortinclude}'.");
                    valid = false;
                }

                // This might bail on names which later could have been converted
                // to assembly references. In those cases a warning should have been enough.
                var shortinclude = projref.Shortinclude;
                if (projects.Any(p => string.Compare(p._sln_shortfilename, shortinclude, true) == 0) &&
                    !projects.Any(p => string.Compare(p._sln_shortfilename, shortinclude, false) == 0))
                {
                    ConsoleHelper.ColorWrite(ConsoleColor.Red,
                        $"Error: Reference has mismatched casing: Project: '{_sln_path} '. Project reference: '{shortinclude}'. Target project: '{projects.First(p => string.Compare(p._sln_shortfilename, shortinclude, true) == 0)._sln_shortfilename}'.");
                    valid = false;
                }

                // Project references which we need must atleast exist in file system.
                var fullfilename = Path.Combine(Path.GetDirectoryName(solutionfile), Path.GetDirectoryName(_sln_path), projref.Include);
                if (!projects.Any(p => p._sln_shortfilename == projref.Shortinclude) && !File.Exists(fullfilename))
                {
                    ConsoleHelper.ColorWrite(ConsoleColor.Red,
                        $"Error: Project reference does not exist: Project: '{_sln_path}'. Project reference path: '{fullfilename}'.");
                    valid = false;
                }
            }

            return valid;
        }

        private static void CheckName(string path, string assemblyname)
        {
            if (assemblyname == null)
            {
                return;
            }

            var filename = Path.GetFileNameWithoutExtension(path);
            var pos = filename.LastIndexOf('.');
            if (pos >= 0)
            {
                filename = filename[(pos + 1)..];
            }

            var assname = assemblyname;
            pos = assname.LastIndexOf('.');
            if (pos >= 0)
            {
                assname = assname[(pos + 1)..];
            }
            var wrotemessage = false;
            if (assname != filename &&
                $"{assname}Lib" != filename &&
                $"{assname}CSharp" != filename &&
                !assemblyname.Replace(".", "").EndsWith(filename))
            {
                ConsoleHelper.ColorWrite(ConsoleColor.Yellow,
                    $"Warning: Mismatched name for project '{path}': Assembly name: '{assemblyname}'. File name: '{Path.GetFileNameWithoutExtension(path)}'.");
                wrotemessage = true;
            }

            // Egentligen borde det göras en rak jämförelse mellan projektfilsnamn och assemblyname,
            // men då skulle det varnas på de flesta projekt. Vi skriver ut detta "spam"
            // som verbose information i stället.

            if (!wrotemessage)
            {
                filename = Path.GetFileNameWithoutExtension(path);

                if (filename != assemblyname)
                {
                    ConsoleHelper.WriteLine(
                        $"  Warning: Mismatched name for project '{path}': Assembly name: '{assemblyname}'. File name: '{filename}'.",
                        true);
                }
                else
                {
                    ConsoleHelper.WriteLine(
                        $"  Very good: Name for project '{path}': Assembly name==File name: '{assemblyname}'.",
                        true);
                }
            }
        }

        public bool Fix(string solutionfile, List<Project> projects, List<string> hintpaths, bool removeversion)
        {
            ConsoleHelper.WriteLineDeferred($"-=-=- Fixing project: '{_sln_path}' -=-=-");

            // ass -> proj
            foreach (var assref in _references.OrderBy(r => r.Shortinclude))
            {
                var exists = projects.Any(p => p._proj_assemblyname == assref.Shortinclude);
                if (exists)
                {
                    var projref = CreateProjectReferenceFromReference(projects, assref);
                    if (assref.Shortinclude == projref.Shortinclude)
                    {
                        ConsoleHelper.WriteLine($"  ref -> projref: '{assref.Shortinclude}'.", true);
                    }
                    else
                    {
                        ConsoleHelper.WriteLine($"  ref -> projref: '{assref.Shortinclude}' -> '{projref.Shortinclude}'.", true);
                    }

                    _projectReferences.Add(projref);
                    _references.Remove(assref);
                    _modified = true;
                }
            }

            // proj -> ass
            foreach (var projref in _projectReferences.OrderBy(r => r.Shortinclude))
            {
                var exists = projects.Any(p => p._sln_shortfilename == projref.Shortinclude);
                if (!exists)
                {
                    AssemblyRef assref = CreateReferenceFromProjectReference(solutionfile, projects, hintpaths, projref);
                    if (projref.Shortinclude == assref.Shortinclude)
                    {
                        ConsoleHelper.WriteLine($"  projref -> ref: '{projref.Shortinclude}'.", true);
                    }
                    else
                    {
                        ConsoleHelper.WriteLine($"  projref -> ref: '{projref.Shortinclude}' -> '{assref.Shortinclude}'.", true);
                    }

                    _references.Add(assref);
                    _projectReferences.Remove(projref);
                    _modified = true;
                }
            }

            // Fix hint paths
            foreach (var assref in _references.OrderBy(r => r.Shortinclude))
            {
                FixHintPath(solutionfile, hintpaths, assref);
            }

            if (removeversion)
            {
                foreach (var assref in _references.OrderBy(r => r.Shortinclude))
                {
                    var shortref = GetShortRef(assref.Include);
                    if (shortref != assref.Include)
                    {
                        ConsoleHelper.WriteLine($"  ref: removing version: '{assref.Include}' -> '{shortref}'.", true);
                        assref.Include = shortref;
                        assref.Shortinclude = shortref;

                        _modified = true;
                    }
                }
            }

            ConsoleHelper.WriteLineDeferred(null);

            return true;
        }

        private ProjectRef CreateProjectReferenceFromReference(List<Project> projects, AssemblyRef assref)
        {
            Project referencedProject;
            try
            {
                referencedProject = projects.SingleOrDefault(p => p._proj_assemblyname == assref.Shortinclude);
            }
            catch (InvalidOperationException)
            {
                // Early validation prevents this exception
                ConsoleHelper.ColorWrite(ConsoleColor.Red, "Error: Projects have identical assembly names.");
                throw;
            }

            var relpath = FileHelper.GetRelativePath(_sln_path, referencedProject._sln_path);

            return new ProjectRef
            {
                Include = relpath,
                Shortinclude = referencedProject._sln_shortfilename,
                Name = referencedProject._sln_shortfilename,
                Project = referencedProject._proj_guid,
                Package = referencedProject._sln_package,
                Names = null,
                Projects = null,
                Packages = null
            };
        }

        private AssemblyRef CreateReferenceFromProjectReference(string solutionfile, List<Project> projects, List<string> hintpaths, ProjectRef projref)
        {
            // Look for assembly name in external project file. The project file might not exist though.

            TryToRetrieveAssemblyInfoOfProjectReference(solutionfile, projects, projref, out string assemblyname, out string outputtype);

            // Guess assembly name = proj name
            assemblyname ??= projref.Shortinclude;

            // Guess output type = Library
            outputtype ??= "Library";

            var ext = outputtype switch
            {
                "Library" or "Database" => ".dll",
                "WinExe" or "Exe" => ".exe",
                _ => throw new Exception($"Unsupported project type: '{assemblyname}' '{outputtype}'."),
            };

            // Locate assembly
            // if we had used OutputFolder from projref project (instead of hintpaths),
            // debug/release may have caused problems
            var asspath = LocateAssemblyInHintPaths(solutionfile, hintpaths, assemblyname, ext);

            return new AssemblyRef
            {
                Include = assemblyname,
                Shortinclude = assemblyname,
                Name = assemblyname,
                Hintpath = asspath,
                Names = null,
                Hintpaths = null
            };
        }

        // Validate existence of assembly file in hint paths.
        // Notice: newly created assembly refs are searched again, but (if assembly was found earlier) no "->"-message will be shown.
        // Handles absolute paths (not verified much).
        // Always try to replace path, but keep original assembly path if file didn't exist in any specified hintpath.
        // If gac registered dll does exist in any hintpath, add/update hintpath.
        // (If gac registered dll with hintpath doesn't exist in any hintpath: remove hintpath (no matter if it exist in original path -
        //   except if it's private/copylocal))
        private void FixHintPath(string solutionfile, List<string> hintpaths, AssemblyRef assref)
        {
            /*
            Om det inte gick att ta reda på en assref's typ (dll/exe), antar vi dll.
            Det finns då en risk att exe konverteras till dll om vi hittar en dllfil
            med samma namn i någon hint katalog. Detta är oavsett om assembly referensen
            är skapad från projref eller inladdad rakt av.
            */

            var ext = assref.Hintpath == null ? ".dll" : Path.GetExtension(assref.Hintpath);
            var asspath_new = LocateAssemblyInHintPaths(solutionfile, hintpaths, assref.Shortinclude, ext);

            if (assref.Hintpath == null)
            {
                if (asspath_new == null)
                {
                    if (!Gac.IsSystemAssembly(assref.Shortinclude, out _, true))
                    {
                        // Error - no existing hint path, and no file found in any specified hint path.
                        ConsoleHelper.ColorWrite(ConsoleColor.Yellow, $"Warning: Couldn't find assembly: '{assref.Shortinclude}{ext}'.");
                    }
                }
                else
                {
                    // Ok - replacing null with new hint path
                    ConsoleHelper.WriteLine($"  Found assembly in hintpath: '{assref.Shortinclude}': -> '{asspath_new}'. Ext: '{ext}'.", true);
                    assref.Hintpath = asspath_new;
                    _modified = true;
                }
            }
            else
            {
                if (asspath_new == null)
                {
                    if (Gac.IsSystemAssembly(assref.Shortinclude, out _, true) && (!assref.Copylocal.HasValue || !assref.Copylocal.Value))
                    {
                        // Remove path to gac, even if it's valid on this computer,
                        // the specified command args hintpaths are the only allowed.

                        ConsoleHelper.WriteLine($"  Didn't find gac assembly in any specified hintpath: '{assref.Shortinclude}'. Removing hintpath: '{assref.Hintpath}'.", true);
                        assref.Hintpath = null;
                        _modified = true;
                    }
                    else
                    {
                        var asspath = Path.IsPathRooted(assref.Hintpath)
                            ? assref.Hintpath
                            : Path.Combine(Path.GetDirectoryName(solutionfile), Path.GetDirectoryName(_sln_path), assref.Hintpath);

                        if (!File.Exists(asspath))
                        {
                            // Error - no file in existing hint path, and no file found in any specified hint path.
                            ConsoleHelper.ColorWrite(ConsoleColor.Yellow, $"Warning: Couldn't find assembly: '{assref.Shortinclude}': File not found: '{asspath}'.");
                        }
                    }
                }
                else
                {
                    // Ok - if diff, replace existing hint path with new hint path
                    if (string.Compare(asspath_new, assref.Hintpath, true) != 0)
                    {
                        ConsoleHelper.WriteLine($"  Found assembly in specified hintpath: '{assref.Shortinclude}': '{assref.Hintpath}' -> '{asspath_new}'.", true);
                        assref.Hintpath = asspath_new;
                        _modified = true;
                    }
                }
            }
        }

        private string LocateAssemblyInHintPaths(string solutionfile, List<string> hintpaths, string assemblyname, string ext)
        {
            if (hintpaths == null)
            {
                return null;
            }

            string asspath = null;
            var projfilepath = Path.Combine(Path.GetDirectoryName(solutionfile), _sln_path);

            foreach (var path in hintpaths)
            {
                var relpath = FileHelper.GetRelativePath(projfilepath, Path.Combine(path, $"{assemblyname}{ext}"));
                var testpath = Path.Combine(Path.GetDirectoryName(projfilepath), relpath);

                if (File.Exists(testpath))
                {
                    asspath = relpath;
                    break;
                }
            }

            return asspath;
        }

        private bool TryToRetrieveAssemblyInfoOfProjectReference(string solutionfile, List<Project> projects, ProjectRef projref, out string assemblyname, out string outputtype)
        {
            XDocument xdoc;

            var fullfilename = Path.Combine(Path.GetDirectoryName(solutionfile), Path.GetDirectoryName(_sln_path), projref.Include);

            ConsoleHelper.WriteLine($"  Loading external project: '{fullfilename}'.", true);

            try
            {
                // Delve greedily and deep into the external project file.
                xdoc = XDocument.Load(fullfilename);
            }
            catch (Exception ex) when (ex is IOException or XmlException)
            {
                ConsoleHelper.ColorWrite(ConsoleColor.Red, $"Couldn't load project: '{fullfilename}': {ex.Message}");
                assemblyname = outputtype = null;
                return false;
            }

            var ns = xdoc.Root.Name.Namespace;

            XElement[] assemblynames = [.. xdoc.Elements(ns + "Project").Elements(ns + "PropertyGroup").Elements(ns + "AssemblyName")];
            if (assemblynames.Length == 0)
            {
                ConsoleHelper.ColorWrite(ConsoleColor.Yellow, $"Couldn't load project: '{fullfilename}': Missing AssemblyName.");
                assemblyname = null;
            }
            else
            {
                assemblyname = assemblynames.Single().Value;
            }

            XElement[] outputtypes = [.. xdoc.Elements(ns + "Project").Elements(ns + "PropertyGroup").Elements(ns + "OutputType")];
            if (outputtypes.Length == 0)
            {
                ConsoleHelper.ColorWrite(ConsoleColor.Yellow, $"Couldn't load project: '{fullfilename}': Missing OutputType.");
                outputtype = null;
            }
            else
            {
                outputtype = outputtypes.Single().Value;
            }

            if (assemblyname != null)
            {
                // Caution: External project (its assembly name) may conflict with a loaded project name
                var afterthis = false;
                foreach (var proj in projects.OrderBy(p => p._sln_path))
                {
                    if (proj == this)
                    {
                        afterthis = true;
                        continue;
                    }
                    if (!afterthis)
                    {
                        continue;
                    }

                    if (assemblyname == proj._proj_assemblyname)
                    {
                        ConsoleHelper.ColorWrite(ConsoleColor.Red, $"Error: Projects have identical assembly names: '{assemblyname}': '{fullfilename}' and '{proj._sln_path}'.");
                        throw new Exception("Error");
                    }
                }
            }

            return true;
        }

        public void WriteProject(string solutionfile, bool simulate, bool nobackup)
        {
            XDocument xdoc;
            var fullfilename = Path.Combine(Path.GetDirectoryName(solutionfile), _sln_path);

            try
            {
                xdoc = XDocument.Load(fullfilename);
            }
            catch (Exception ex) when (ex is IOException or XmlException)
            {
                Console.WriteLine($"Couldn't load project: '{fullfilename}': {ex.Message}");
                return;
            }

            ConsoleHelper.WriteLineDeferred($"-=-=- Saving project: '{_sln_path}' -=-=-");

            UpdateReferences(xdoc);
            UpdateProjectReferences(xdoc);

            var bakfile = $"{fullfilename}.bak.xml";
            ConsoleHelper.WriteLine($"  Writing file: '{fullfilename}'.", true);
            if (!simulate)
            {
                if (!nobackup)
                {
                    if (File.Exists(bakfile))
                    {
                        FileHelper.RemoveRO(bakfile);
                        File.Delete(bakfile);
                    }

                    File.Move(fullfilename, bakfile);
                }

                xdoc.Save(fullfilename);
            }

            ConsoleHelper.WriteLineDeferred(null);
        }

        // Todo: check case sensitivity
        public void UpdateReferences(XDocument xdoc)
        {
            var ns = xdoc.Root.Name.Namespace;

            string[] references = [.. xdoc
                .Element(ns + "Project").Elements(ns + "ItemGroup").Elements(ns + "Reference")
                .Where(el => el.Attribute("Include") != null)
                .OrderBy(el => GetShortRef(el.Attribute("Include").Value))
                .Select(el => GetShortRef(el.Attribute("Include").Value))];

            // Remove references
            foreach (var reference in references)
            {
                if (!_references.Any(r => r.Shortinclude == reference))
                {
                    XElement[] refs = [.. xdoc
                        .Element(ns + "Project").Elements(ns + "ItemGroup").Elements(ns + "Reference")
                        .Where(el => el.Attribute("Include") != null && GetShortRef(el.Attribute("Include").Value) == reference)];
                    foreach (var el in refs)
                    {
                        ConsoleHelper.WriteLine($"  Removing assembly ref: '{reference}'", true);
                        el.Remove();
                    }
                }
            }

            // Add/update references
            foreach (var assref in _references.OrderBy(r => r.Shortinclude))
            {
                if (!references.Contains(assref.Shortinclude))
                {
                    assref.AddToDoc(xdoc, ns);
                }
                else
                {
                    assref.UpdateInDoc(xdoc, ns);
                }
            }
        }

        // Todo: check case sensitivity
        public void UpdateProjectReferences(XDocument xdoc)
        {
            var ns = xdoc.Root.Name.Namespace;

            string[] projectReferences = [.. xdoc
                .Element(ns + "Project").Elements(ns + "ItemGroup").Elements(ns + "ProjectReference")
                .Where(el => el.Attribute("Include") != null)
                .OrderBy(el => Path.GetFileNameWithoutExtension(el.Attribute("Include").Value))
                .Select(el => Path.GetFileNameWithoutExtension(el.Attribute("Include").Value))];

            // Remove project references
            foreach (var reference in projectReferences)
            {
                if (!_projectReferences.Any(r => r.Shortinclude == reference))
                {
                    XElement[] refs = [.. xdoc
                        .Element(ns + "Project").Elements(ns + "ItemGroup").Elements(ns + "ProjectReference")
                        .Where(el => el.Attribute("Include") != null && Path.GetFileNameWithoutExtension(el.Attribute("Include").Value) == reference)];
                    foreach (var el in refs)
                    {
                        ConsoleHelper.WriteLine($"  Removing proj ref: '{reference}'", true);
                        el.Remove();
                    }
                }
            }

            // Add/update project references
            foreach (var projref in _projectReferences.OrderBy(r => r.Shortinclude))
            {
                if (!projectReferences.Contains(projref.Shortinclude))
                {
                    projref.AddToDoc(xdoc, ns);
                }
                else
                {
                    //throw new NotImplementedException("Todo: update existing proj path!");
                }
            }
        }

        public void UpdateOutputPath(XDocument xdoc, string outputpath)
        {
            var ns = xdoc.Root.Name.Namespace;

            XElement[] OutputPaths = [.. xdoc.Element(ns + "Project").Elements(ns + "PropertyGroup").Elements(ns + "OutputPath")];

            foreach (var el in OutputPaths)
            {
                if (el.Value != outputpath)
                {
                    Console.WriteLine($"XXX: '{el.Value}' -> '{outputpath}'");
                    el.Value = outputpath;
                }
            }
        }
    }
}
