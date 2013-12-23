using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml.Linq;
using System.IO;

namespace ProjFix
{
	class AssemblyRef
	{
		public string include { get; set; }
		public string shortinclude { get; set; }  // Assembly name
		public string name { get; set; }
		public string hintpath { get; set; }
		public bool? copylocal { get; set; }  // Xml tag name=Private

		// Used when loading, before validating/fixing
		public List<string> names { get; set; }
		public List<string> hintpaths { get; set; }
		public List<string> copylocals { get; set; }
	}

	class ProjectRef
	{
		public string include { get; set; }
		public string shortinclude { get; set; }  // Project file name
		public string name { get; set; }
		public string project { get; set; }
		public string package { get; set; }

		// Used when loading, before validating/fixing
		public List<string> names { get; set; }
		public List<string> projects { get; set; }
		public List<string> packages { get; set; }
	}

	class Project
	{
		public string _sln_package { get; set; }
		public string _sln_shortfilename { get; set; }
		public string _sln_guid { get; set; }
		public string _sln_path { get; set; }

		public string _proj_assemblyname { get; set; }
		public string _proj_guid { get; set; }
		public string _proj_outputtype { get; set; }  // Not used, yet.

		public List<string> _outputpaths { get; set; }
		public List<AssemblyRef> _references { get; set; }
		public List<ProjectRef> _projectReferences { get; set; }

		public bool _modified { get; set; }


		public void Restore(string solutionfile)
		{
			string fullfilename = Path.Combine(Path.GetDirectoryName(solutionfile), _sln_path);
			string bakfile = fullfilename + ".bak.xml";
			if (File.Exists(bakfile))
			{
				ConsoleHelper.WriteLine("'" + bakfile + "' -> '" + fullfilename + "'", false);
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
				newproj._proj_guid = xdoc.Element(ns + "Project").Element(ns + "PropertyGroup").Element(ns + "ProjectGuid").Value;
			}
			catch (System.NullReferenceException)
			{
				ConsoleHelper.ColorWrite(ConsoleColor.Red, "Couldn't load project: '" + fullfilename + "': Missing ProjectGuid.");
				return null;
			}
			try
			{
				newproj._proj_assemblyname = xdoc.Element(ns + "Project").Element(ns + "PropertyGroup").Element(ns + "AssemblyName").Value;
			}
			catch (System.NullReferenceException)
			{
				ConsoleHelper.ColorWrite(ConsoleColor.Red, "Couldn't load project: '" + fullfilename + "': Missing AssemblyName.");
				return null;
			}
			try
			{
				newproj._proj_outputtype = xdoc.Element(ns + "Project").Element(ns + "PropertyGroup").Element(ns + "OutputType").Value;
			}
			catch (System.NullReferenceException)
			{
				ConsoleHelper.ColorWrite(ConsoleColor.Red, "Couldn't load project: '" + fullfilename + "': Missing OutputType.");
				return null;
			}


			newproj._outputpaths =
					(from el in xdoc.Element(ns + "Project").Elements(ns + "PropertyGroup").Elements(ns + "OutputPath")
					 select el.Value)
					.ToList();


			newproj._references =
					(from el in xdoc.Element(ns + "Project").Elements(ns + "ItemGroup").Elements(ns + "Reference")
					 where el.Attribute("Include") != null
					 orderby GetShortRef(el.Attribute("Include").Value)
					 select new AssemblyRef
					 {
						 include = el.Attribute("Include").Value,
						 shortinclude = GetShortRef(el.Attribute("Include").Value),
						 names = (from elName in el.Elements(ns + "Name")
											orderby elName.Value
											select elName.Value).ToList(),
						 hintpaths = (from elHintPath in el.Elements(ns + "HintPath")
													orderby elHintPath.Value
													select elHintPath.Value).ToList(),
						 copylocals = (from elName in el.Elements(ns + "Private")
													 orderby elName.Value
													 select elName.Value).ToList(),
						 name = null,
						 hintpath = null,
						 copylocal = null
					 })
					 .ToList();


			newproj._projectReferences =
					(from el in xdoc.Element(ns + "Project").Elements(ns + "ItemGroup").Elements(ns + "ProjectReference")
					 where el.Attribute("Include") != null
					 orderby Path.GetFileNameWithoutExtension(el.Attribute("Include").Value)
					 select new ProjectRef
					 {
						 include = el.Attribute("Include").Value,
						 shortinclude = Path.GetFileNameWithoutExtension(el.Attribute("Include").Value),
						 names = (from elName in el.Elements(ns + "Name")
											orderby elName.Value
											select elName.Value).ToList(),
						 projects = (from elProject in el.Elements(ns + "Project")
												 orderby elProject.Value
												 select elProject.Value).ToList(),
						 packages = (from elPackage in el.Elements(ns + "Package")
												 orderby elPackage.Value
												 select elPackage.Value).ToList(),
						 name = null,
						 project = null,
						 package = null
					 })
					.ToList();


			return newproj;
		}

		// Wipcore.Enova.Something, Version=1.2.3.4, Culture=neutral, processorArchitecture=MSIL
		// ->
		// Wipcore.Enova.Something
		private static string GetShortRef(string s)
		{
			if (s.IndexOf(',') >= 0)
			{
				return s.Substring(0, s.IndexOf(','));
			}

			return s;
		}

		public void CompactRefs()
		{
			foreach (AssemblyRef assref in _references)
			{
				if (assref.names.Count > 1)
				{
					ConsoleHelper.ColorWrite(ConsoleColor.Yellow,
						"Warning: Corrupt project file: " + _sln_path +
						", reference: '" + assref.include +
						"', compacting Name elements.");
					_modified = true;
				}
				if (assref.names.Count >= 1)
				{
					assref.name = assref.names[0];
					assref.names = null;
				}

				if (assref.hintpaths.Count > 1)
				{
					ConsoleHelper.ColorWrite(ConsoleColor.Yellow,
						"Warning: Corrupt project file: " + _sln_path +
						", reference: '" + assref.include +
						"', compacting HintPath elements.");
					_modified = true;
				}
				if (assref.hintpaths.Count >= 1)
				{
					assref.hintpath = assref.hintpaths[0];
					assref.hintpaths = null;
				}

				if (assref.copylocals.Count > 1)
				{
					ConsoleHelper.ColorWrite(ConsoleColor.Yellow,
						"Warning: Corrupt project file: " + _sln_path +
						", reference: '" + assref.include +
						"', compacting Private elements.");
					_modified = true;
				}
				if (assref.copylocals.Count >= 1)
				{
					bool b;
					if (bool.TryParse(assref.copylocals[0], out b))
					{
						assref.copylocal = b;
					}
					assref.copylocals = null;
				}
			}

			foreach (ProjectRef projref in _projectReferences)
			{
				if (projref.names.Count > 1)
				{
					ConsoleHelper.ColorWrite(ConsoleColor.Yellow,
						"Warning: Corrupt project file: " + _sln_path +
						", project reference: '" + projref.include +
						"', compacting Name elements.");
					_modified = true;
				}
				if (projref.names.Count >= 1)
				{
					projref.name = projref.names[0];
					projref.names = null;
				}

				if (projref.projects.Count > 1)
				{
					ConsoleHelper.ColorWrite(ConsoleColor.Yellow,
						"Warning: Corrupt project file: " + _sln_path +
						", project reference: '" + projref.include +
						"', compacting Project elements.");
					_modified = true;
				}
				if (projref.projects.Count >= 1)
				{
					projref.project = projref.projects[0];
					projref.projects = null;
				}

				if (projref.packages.Count > 1)
				{
					ConsoleHelper.ColorWrite(ConsoleColor.Yellow,
						"Warning: Corrupt project file: " + _sln_path +
						", project reference: '" + projref.include +
						"', compacting Package elements.");
					_modified = true;
				}
				if (projref.packages.Count >= 1)
				{
					projref.package = projref.packages[0];
					projref.packages = null;
				}
			}

			return;
		}

		public bool Validate(string solutionfile, List<Project> projects)
		{
			bool valid = true;

			if (string.Compare(_sln_guid, _proj_guid, true) != 0)
			{
				ConsoleHelper.ColorWrite(ConsoleColor.Red,
					"Mismatched guid for project '" + _sln_path +
					"': Guid in solution: '" + _sln_guid +
					"'. Guid in project: '" + _proj_guid + "'.");
				valid = false;
			}
			if (string.Compare(_sln_shortfilename, Path.GetFileNameWithoutExtension(_sln_path), true) != 0)
			{
				ConsoleHelper.ColorWrite(ConsoleColor.Red,
					"Mismatched name for project '" + _sln_path +
					"': Project name in solution: '" + _sln_shortfilename +
					"'. File name: '" + Path.GetFileNameWithoutExtension(_sln_path) + "'.");
				valid = false;
			}

			CheckName(solutionfile, _sln_path, _proj_assemblyname);


			bool afterthis = false;
			foreach (Project proj in projects.OrderBy(p => p._sln_path))
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
						"Error: Projects have identical assembly names: '" + _proj_assemblyname + "': '" +
						_sln_path + "' and '" + proj._sln_path + "'.");
					valid = false;
				}
			}


			foreach (AssemblyRef assref in _references)
			{
				if (string.Compare(assref.shortinclude, _proj_assemblyname, true) == 0 ||
						string.Compare(assref.shortinclude, _sln_shortfilename, true) == 0)
				{
					ConsoleHelper.ColorWrite(ConsoleColor.Red,
						"Error: Project have reference to itself: '" + _sln_path +
						"'. Reference: '" + assref.shortinclude + "'.");
					valid = false;
				}


				// This might bail on unknown project types which later could have been converted
				// to project references. In those cases a warning should have been enough.
				if (assref.hintpath != null)
				{
					string path = assref.hintpath;

					string[] exts = { ".dll", ".exe" };
					if (!exts.Any(e => path.EndsWith(e, StringComparison.InvariantCultureIgnoreCase)))
					{
						ConsoleHelper.ColorWrite(ConsoleColor.Red,
							"Error: Invalid reference type: '" + _sln_path +
							"'. Ext: '" + Path.GetExtension(path) +
							"'. Path: '" + path + "'.");
						valid = false;
					}
				}
			}

			foreach (ProjectRef projref in _projectReferences)
			{
				if (string.Compare(projref.shortinclude, _proj_assemblyname, true) == 0 ||
						string.Compare(projref.shortinclude, _sln_shortfilename, true) == 0)
				{
					ConsoleHelper.ColorWrite(ConsoleColor.Red,
						"Error: Project have reference to itself: '" + _sln_path +
						"'. Project reference: '" + projref.shortinclude + "'.");
					valid = false;
				}


				// This might bail on names which later could have been converted
				// to assembly references. In those cases a warning should have been enough.
				string shortinclude = projref.shortinclude;
				if (projects.Any(p => string.Compare(p._sln_shortfilename, shortinclude, true) == 0) &&
						!projects.Any(p => string.Compare(p._sln_shortfilename, shortinclude, false) == 0))
				{
					ConsoleHelper.ColorWrite(ConsoleColor.Red,
						"Error: Reference has mismatched casing: Project: '" + _sln_path +
						"'. Project reference: '" + shortinclude +
						"'. Target project: '" + projects.First(p => string.Compare(p._sln_shortfilename, shortinclude, true) == 0)._sln_shortfilename + "'.");
					valid = false;
				}


				// Project references which we need must atleast exist in file system.
				string fullfilename = Path.Combine(
						Path.GetDirectoryName(solutionfile),
						Path.GetDirectoryName(_sln_path),
						projref.include);
				if (!projects.Any(p => p._sln_shortfilename == projref.shortinclude) && !File.Exists(fullfilename))
				{
					ConsoleHelper.ColorWrite(ConsoleColor.Red,
						"Error: Project reference does not exist: Project: '" + _sln_path +
						"'. Project reference path: '" + fullfilename + "'.");
					valid = false;
				}
			}


			return valid;
		}

		private static void CheckName(string solutionfile, string path, string assemblyname)
		{
			int pos;
			string filename = Path.GetFileNameWithoutExtension(path);
			pos = filename.LastIndexOf('.');
			if (pos >= 0)
			{
				filename = filename.Substring(pos + 1);
			}

			string assname = assemblyname;
			pos = assname.LastIndexOf('.');
			if (pos >= 0)
			{
				assname = assname.Substring(pos + 1);
			}
			bool wrotemessage = false;
			if (assname != filename &&
					assname + "Lib" != filename &&
					assname + "CSharp" != filename &&
					!assemblyname.Replace(".", "").EndsWith(filename))
			{
				ConsoleHelper.ColorWrite(ConsoleColor.Yellow,
					"Warning: Mismatched name for project '" + path +
					"': Assembly name: '" + assemblyname +
					"'. File name: '" + Path.GetFileNameWithoutExtension(path) + "'.");
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
						"  Warning: Mismatched name for project '" + path +
						"': Assembly name: '" + assemblyname +
						"'. File name: '" + filename + "'.",
						true);
				}
				else
				{
					ConsoleHelper.WriteLine(
						"  Very good: Name for project '" + path +
						"': Assembly name==File name: '" + assemblyname + "'.",
						true);
				}
			}
		}

		public bool Fix(string solutionfile, List<Project> projects, List<string> hintpaths, string outputpath, bool copylocal, bool removeversion)
		{
			ConsoleHelper.WriteLineDeferred("-=-=- Fixing project: '" + _sln_path + "' -=-=-");

			// ass -> proj
			foreach (AssemblyRef assref in _references.OrderBy(r => r.shortinclude))
			{
				bool exists = projects.Any(p => p._proj_assemblyname == assref.shortinclude);
				if (exists)
				{
					ProjectRef projref = CreateProjectReferenceFromReference(solutionfile, projects, assref);
					if (assref.shortinclude == projref.shortinclude)
						ConsoleHelper.WriteLine("  ref -> projref: '" + assref.shortinclude + "'.", true);
					else
						ConsoleHelper.WriteLine("  ref -> projref: '" + assref.shortinclude + "' -> '" + projref.shortinclude + "'.", true);

					_projectReferences.Add(projref);
					_references.Remove(assref);
					_modified = true;
				}
			}


			// proj -> ass
			foreach (ProjectRef projref in _projectReferences.OrderBy(r => r.shortinclude))
			{
				bool exists = projects.Any(p => p._sln_shortfilename == projref.shortinclude);
				if (!exists)
				{
					AssemblyRef assref = CreateReferenceFromProjectReference(solutionfile, projects, hintpaths, projref);
					if (projref.shortinclude == assref.shortinclude)
						ConsoleHelper.WriteLine("  projref -> ref: '" + projref.shortinclude + "'.", true);
					else
						ConsoleHelper.WriteLine("  projref -> ref: '" + projref.shortinclude + "' -> '" + assref.shortinclude + "'.", true);

					_references.Add(assref);
					_projectReferences.Remove(projref);
					_modified = true;
				}
			}


			// Fix hint paths
			foreach (AssemblyRef assref in _references.OrderBy(r => r.shortinclude))
			{
				FixHintPath(solutionfile, hintpaths, assref);
			}


			if (outputpath != null)
			{
				/*// todo: Abs -> Rel?
				bool diff = _outputpaths.Any(o => o != outputpath);
				if (diff)
				{
						_outputpaths = new List<string>();
						_outputpaths.Add(outputpath);
						_modified = true;
				}*/
			}

			if (removeversion)
			{
				foreach (AssemblyRef assref in _references.OrderBy(r => r.shortinclude))
				{
					string include = GetShortRef(assref.include);

					if (include != assref.shortinclude)
						ConsoleHelper.WriteLine("  ref: removing version: '" + assref.shortinclude + "' -> '" + include + "'.", true);

					_modified = true;
				}
			}

			ConsoleHelper.WriteLineDeferred(null);

			return true;
		}

		private ProjectRef CreateProjectReferenceFromReference(string solutionfile, List<Project> projects, AssemblyRef assref)
		{
			Project referencedProject;
			try
			{
				referencedProject = projects.SingleOrDefault(p => p._proj_assemblyname == assref.shortinclude);
			}
			catch (System.InvalidOperationException)
			{
				// Early validation prevents this exception
				ConsoleHelper.ColorWrite(ConsoleColor.Red, "Error: Projects have identical assembly names.");
				throw;
			}

			string relpath = FileHelper.GetRelativePath(_sln_path, referencedProject._sln_path);

			return new ProjectRef
			{
				include = relpath,
				shortinclude = referencedProject._sln_shortfilename,
				name = referencedProject._sln_shortfilename,
				project = referencedProject._proj_guid,
				package = referencedProject._sln_package,
				names = null,
				projects = null,
				packages = null
			};
		}

		private AssemblyRef CreateReferenceFromProjectReference(string solutionfile, List<Project> projects, List<string> hintpaths, ProjectRef projref)
		{
			// Look for assembly name in external project file. The project file might not exist though.
			string assemblyname;
			string outputtype;

			TryToRetrieveAssemblyInfoOfProjectReference(solutionfile, projects, projref, out assemblyname, out outputtype);

			//if (projref.shortinclude != assemblyname)
			{
				//ConsoleHelper.ColorWrite(ConsoleColor.Yellow, "Warning: '" + projref.shortinclude + "' -> '" + assemblyname + "'.");
			}

			if (assemblyname == null)
			{
				// Guess assembly name = proj name
				assemblyname = projref.shortinclude;
			}


			string ext;
			switch (outputtype)
			{
				case "Library":
					ext = ".dll";
					break;
				case "WinExe":
					ext = ".exe";
					break;
				case "Exe":
					ext = ".exe";
					break;
				default:
					throw new Exception("Unsupported project type: '" + assemblyname + "' '" + outputtype + "'.");
			}

			// Locate assembly
			// if we had used OutputFolder from projref project (instead of hintpaths),
			// debug/release may have caused problems
			string asspath = LocateAssemblyInHintPaths(solutionfile, hintpaths, assemblyname, ext);

			return new AssemblyRef
			{
				include = assemblyname,
				shortinclude = assemblyname,
				name = assemblyname,
				hintpath = asspath,
				names = null,
				hintpaths = null
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
			//string ext = GetAssRefExt(assref.hintpath);

			//string asspath_new2 = LocateAssemblyInHintPaths(solutionfile, hintpaths, assref.shortinclude, "Library");

			/*
			Om det inte gick att ta reda på en assref's typ (dll/exe), antar vi dll.
			Det finns då en risk att exe konverteras till dll om vi hittar en dllfil
			med samma namn i någon hint katalog. Detta är oavsett om assembly referensen
			är skapad från projref eller inladdad rakt av.
			*/

			string ext;
			if (assref.hintpath == null)
			{
				ext = ".dll";
			}
			else
			{
				ext = Path.GetExtension(assref.hintpath);
			}

			string asspath_new = LocateAssemblyInHintPaths(solutionfile, hintpaths, assref.shortinclude, ext);
			if (assref.hintpath == null)
			{
				if (asspath_new == null)
				{
					string dummy;
					if (!gac.IsSystemAssembly(assref.shortinclude, out dummy, true))
					{
						// Error - no existing hint path, and no file found in any specified hint path.
						ConsoleHelper.ColorWrite(ConsoleColor.Yellow,
							"Warning: Couldn't find assembly: '" + assref.shortinclude + ext + "'.");
					}
				}
				else
				{
					// Ok - replacing null with new hint path
					ConsoleHelper.WriteLine(
						"  Found assembly in hintpath: '" + assref.shortinclude +
						"': -> '" + asspath_new +
						"'. Ext: '" + ext + "'.", true);
					assref.hintpath = asspath_new;
					_modified = true;
				}
			}
			else
			{
				if (asspath_new == null)
				{
					string dummy;
					if (gac.IsSystemAssembly(assref.shortinclude, out dummy, true) && (!assref.copylocal.HasValue || assref.copylocal.Value == false))
					{
						// Remove path to gac, even if it's valid on this computer,
						// the specified command args hintpaths are the only allowed.

						ConsoleHelper.WriteLine(
							"  Didn't find gac assembly in any specified hintpath: '" + assref.shortinclude +
							"'. Removing hintpath: '" + assref.hintpath + "'.", true);
						assref.hintpath = null;
						_modified = true;
					}
					else
					{
						string asspath;
						if (Path.IsPathRooted(assref.hintpath))
						{
							asspath = assref.hintpath;
						}
						else
						{
							asspath = Path.Combine(
								Path.GetDirectoryName(solutionfile),
								Path.GetDirectoryName(_sln_path),
								assref.hintpath);
						}

						if (!File.Exists(asspath))
						{
							// Error - no file in existing hint path, and no file found in any specified hint path.
							ConsoleHelper.ColorWrite(ConsoleColor.Yellow,
								"Warning: Couldn't find assembly: '" + assref.shortinclude +
								"': File not found: '" + asspath + "'.");
						}
					}
				}
				else
				{
					// Ok - if diff, replace existing hint path with new hint path
					if (string.Compare(asspath_new, assref.hintpath, true) != 0)
					{
						ConsoleHelper.WriteLine(
							"  Found assembly in specified hintpath: '" + assref.shortinclude +
							"': '" + assref.hintpath +
							"' -> '" + asspath_new + "'.", true);
						assref.hintpath = asspath_new;
						_modified = true;
					}
				}
			}

			return;
		}

		private string GetAssRefExt(string assref)
		{
			return "";
		}

		private string LocateAssemblyInHintPaths(string solutionfile, List<string> hintpaths, string assemblyname, string ext)
		{
			if (hintpaths == null)
			{
				return null;
			}

			string asspath = null;
			string projfilepath = Path.Combine(
				Path.GetDirectoryName(solutionfile),
				_sln_path);

			foreach (string path in hintpaths)
			{
				string relpath = FileHelper.GetRelativePath(projfilepath, Path.Combine(path, assemblyname + ext));

				string testpath = Path.Combine(
					Path.GetDirectoryName(projfilepath),
					relpath);

				if (File.Exists(testpath))
				{
					asspath = relpath;
					break;
				}
			}

			return asspath;
		}

		private bool TryToRetrieveAssemblyInfoOfProjectReference(string solutionfile, List<Project> projects, ProjectRef projref,
				out string assemblyname, out string outputtype)
		{
			XDocument xdoc;
			XNamespace ns;

			string fullfilename = Path.Combine(
				Path.GetDirectoryName(solutionfile),
				Path.GetDirectoryName(_sln_path),
				projref.include);

			ConsoleHelper.WriteLine("  Loading external project: '" + fullfilename + "'.", true);

			try
			{
				// Delve greedily and deep into the external project file.
				xdoc = XDocument.Load(fullfilename);
			}
			catch (IOException ex)
			{
				ConsoleHelper.ColorWrite(ConsoleColor.Red, "Couldn't load project: '" + fullfilename + "': " + ex.Message);
				assemblyname = outputtype = null;
				return false;
			}
			catch (System.Xml.XmlException ex)
			{
				ConsoleHelper.ColorWrite(ConsoleColor.Red, "Couldn't load project: '" + fullfilename + "': " + ex.Message);
				assemblyname = outputtype = null;
				return false;
			}

			ns = xdoc.Root.Name.Namespace;


			try
			{
				assemblyname = xdoc.Element(ns + "Project").Element(ns + "PropertyGroup").Element(ns + "AssemblyName").Value;
			}
			catch (System.NullReferenceException)
			{
				ConsoleHelper.ColorWrite(ConsoleColor.Red, "Couldn't load project: '" + fullfilename + "': Missing AssemblyName.");
				assemblyname = outputtype = null;
				return false;
			}
			try
			{
				outputtype = xdoc.Element(ns + "Project").Element(ns + "PropertyGroup").Element(ns + "OutputType").Value;
			}
			catch (System.NullReferenceException)
			{
				ConsoleHelper.ColorWrite(ConsoleColor.Red, "Couldn't load project: '" + fullfilename + "': Missing OutputType.");
				assemblyname = outputtype = null;
				return false;
			}


			// Caution: External project (it's assembly name) may conflict with a loaded project name
			bool afterthis = false;
			foreach (Project proj in projects.OrderBy(p => p._sln_path))
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
					ConsoleHelper.ColorWrite(ConsoleColor.Red,
						"Error: Projects have identical assembly names: '" + assemblyname + "': '" +
						fullfilename + "' and '" + proj._sln_path + "'.");
					throw new Exception("Error");
				}
			}

			return true;
		}

		public void WriteProject(string solutionfile, bool simulate, bool nobackup)
		{
			XDocument xdoc;
			string fullfilename = Path.Combine(Path.GetDirectoryName(solutionfile), _sln_path);

			try
			{
				xdoc = XDocument.Load(fullfilename);
			}
			catch (IOException ex)
			{
				Console.WriteLine("Couldn't load project: '" + fullfilename + "': " + ex.Message);
				return;
			}
			catch (System.Xml.XmlException ex)
			{
				Console.WriteLine("Couldn't load project: '" + fullfilename + "': " + ex.Message);
				return;
			}


			ConsoleHelper.WriteLineDeferred("-=-=- Saving project: '" + _sln_path + "' -=-=-");

			UpdateReferences(xdoc);
			UpdateProjectReferences(xdoc, solutionfile);

			/*if (_outputpath != null)
			{
					UpdateOutputPath(xdoc, solutionfile, outputpath);
			}*/


			string bakfile = fullfilename + ".bak.xml";
			ConsoleHelper.WriteLine("  Writing file: '" + fullfilename + "'.", true);
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

			return;
		}

		// Todo: check case sensitivity
		public void UpdateReferences(XDocument xdoc)
		{
			XNamespace ns = xdoc.Root.Name.Namespace;

			List<string> references =
				(from el in xdoc.Element(ns + "Project").Elements(ns + "ItemGroup").Elements(ns + "Reference")
				 where el.Attribute("Include") != null
				 orderby GetShortRef(el.Attribute("Include").Value)
				 select GetShortRef(el.Attribute("Include").Value))
				.ToList();

			// Remove references
			foreach (string reference in references)
			{
				if (!_references.Any(r => r.shortinclude == reference))
				{
					var refs = from el in xdoc.Element(ns + "Project").Elements(ns + "ItemGroup").Elements(ns + "Reference")
										 where el.Attribute("Include") != null && GetShortRef(el.Attribute("Include").Value) == reference
										 select el;
					foreach (XElement el in refs)
					{
						ConsoleHelper.WriteLine("  Removing assembly ref: '" + reference + "'", true);
						el.Remove();
					}
				}
			}

			// Add references
			foreach (AssemblyRef assref in _references.OrderBy(r => r.shortinclude))
			{
				/*string dummy;
				if (gac.IsSystemAssembly(assref.shortinclude, out dummy, true))
				{
						continue;
				}*/

				if (!references.Contains(assref.shortinclude))
				{
					ConsoleHelper.WriteLine("  Adding assembly ref: '" + assref.include + "'", true);

					XElement newref;

					if (assref.hintpath == null)
					{
						newref = new XElement(ns + "Reference",
							new XAttribute("Include", assref.include),
							new XElement(ns + "SpecificVersion", "False")
							);
					}
					else
					{
						newref = new XElement(ns + "Reference",
							new XAttribute("Include", assref.include),
							new XElement(ns + "SpecificVersion", "False"),
							new XElement(ns + "HintPath", assref.hintpath)
							);
					}


					// Sort insert
					var groups = from el in xdoc.Element(ns + "Project").Elements(ns + "ItemGroup")
											 where el.Element(ns + "Reference") != null
											 select el;
					if (groups.Count() == 0)
					{
						throw new NotImplementedException("Cannot insert reference!");
					}

					var refs = from el in groups.ElementAt(0).Elements(ns + "Reference")
										 where el.Attribute("Include") != null
										 orderby el.Attribute("Include").Value
										 select el;

					if (assref.include.CompareTo(refs.First().Attribute("Include").Value) < 0)
					{
						groups.ElementAt(0).AddFirst(newref);
					}
					else if (assref.include.CompareTo(refs.Last().Attribute("Include").Value) > 0)
					{
						refs.Last().AddAfterSelf(newref);
					}
					else
					{
						for (int i = 0; i < refs.Count() - 1; i++)
						{
							string inc1 = refs.ElementAt(i).Attribute("Include").Value;
							string inc2 = refs.ElementAt(i + 1).Attribute("Include").Value;
							if (assref.include.CompareTo(inc1) > 0 && assref.include.CompareTo(inc2) < 0)
							{
								refs.ElementAt(i).AddAfterSelf(newref);
							}
						}
					}
				}
				else
				{
					// update existing hint path

					List<XElement> references2 =
						(from el in xdoc.Element(ns + "Project").Elements(ns + "ItemGroup").Elements(ns + "Reference")
						 where el.Attribute("Include") != null && el.Attribute("Include").Value == assref.include
						 select el)
					.ToList();

					if (references2.Count != 1)
					{
						ConsoleHelper.WriteLine("  Error: Couldn't update assembly ref: '" + assref.include + "'", false);
						return;
					}

					XElement reference = references2[0];


					XElement hintPath = reference.Element(ns + "HintPath");
					if (assref.hintpath == null)
					{
						if (hintPath != null)
						{
							string oldpath = hintPath.Value;
							if (oldpath != assref.hintpath)
							{
								ConsoleHelper.WriteLine("  Updating assembly ref: Removing hintpath: '" + assref.include +
									"': '" + oldpath + "'.", true);
								hintPath.Remove();
							}
						}
					}
					else
					{
						if (hintPath == null)
						{
							ConsoleHelper.WriteLine("  Updating assembly ref: Adding hintpath: '" + assref.include +
								"', '" + assref.hintpath + "'.", true);
							hintPath = new XElement(ns + "HintPath", assref.hintpath);
							reference.Add(hintPath);
						}
						else
						{
							string oldpath = hintPath.Value;
							if (oldpath != assref.hintpath)
							{
								ConsoleHelper.WriteLine("  Updating assembly ref: Updating hintpath: '" + assref.include +
									"': '" + oldpath + "' -> '" + assref.hintpath + "'.", true);
								hintPath.Value = assref.hintpath;
							}
						}
					}
				}
			}

			return;
		}

		// Todo: check case sensitivity
		public void UpdateProjectReferences(XDocument xdoc, string solutionfile)
		{
			XNamespace ns = xdoc.Root.Name.Namespace;

			List<string> projectReferences =
				(from el in xdoc.Element(ns + "Project").Elements(ns + "ItemGroup").Elements(ns + "ProjectReference")
				 where el.Attribute("Include") != null
				 orderby Path.GetFileNameWithoutExtension(el.Attribute("Include").Value)
				 select Path.GetFileNameWithoutExtension(el.Attribute("Include").Value))
				.ToList();

			// Remove project references
			foreach (string reference in projectReferences)
			{
				if (!_projectReferences.Any(r => r.shortinclude == reference))
				{
					var refs = from el in xdoc.Element(ns + "Project").Elements(ns + "ItemGroup").Elements(ns + "ProjectReference")
										 where el.Attribute("Include") != null && Path.GetFileNameWithoutExtension(el.Attribute("Include").Value) == reference
										 select el;
					foreach (XElement el in refs)
					{
						ConsoleHelper.WriteLine("  Removing proj ref: '" + reference + "'", true);
						el.Remove();
					}
				}
			}

			// Add project references
			foreach (ProjectRef projref in _projectReferences.OrderBy(r => r.shortinclude))
			{
				if (!projectReferences.Contains(projref.shortinclude))
				{
					ConsoleHelper.WriteLine("  Adding proj ref: '" + projref.include + "'", true);

					XElement newref = new XElement(ns + "ProjectReference",
						new XAttribute("Include", projref.include),
						new XElement(ns + "Project", projref.project),
						new XElement(ns + "Name", projref.name)
						);

					// Sort insert
					var groups = from el in xdoc.Element(ns + "Project").Elements(ns + "ItemGroup")
											 where el.Element(ns + "ProjectReference") != null
											 select el;

					if (groups.Count() == 0)
					{
						groups = from el in xdoc.Element(ns + "Project").Elements(ns + "ItemGroup")
										 select el;

						XElement newgroup = new XElement(ns + "ItemGroup", newref);
						groups.Last().AddAfterSelf(newgroup);
					}
					else
					{
						var refs = from el in groups.Elements(ns + "ProjectReference")
											 where el.Attribute("Include") != null
											 orderby el.Attribute("Include").Value
											 select el;

						if (projref.include.CompareTo(refs.First().Attribute("Include").Value) < 0)
						{
							groups.ElementAt(0).AddFirst(newref);
						}
						else if (projref.include.CompareTo(refs.Last().Attribute("Include").Value) > 0)
						{
							refs.Last().AddAfterSelf(newref);
						}
						else
						{
							for (int i = 0; i < refs.Count() - 1; i++)
							{
								string inc1 = refs.ElementAt(i).Attribute("Include").Value;
								string inc2 = refs.ElementAt(i + 1).Attribute("Include").Value;
								if (projref.include.CompareTo(inc1) > 0 && projref.include.CompareTo(inc2) < 0)
								{
									refs.ElementAt(i).AddAfterSelf(newref);
								}
							}
						}
					}
				}
				else
				{
					// todo: update existing proj path
				}
			}

			return;
		}

		public void UpdateOutputPath(XDocument xdoc, string solutionfile, string outputpath)
		{
			XNamespace ns = xdoc.Root.Name.Namespace;

			var OutputPaths =
				from el in xdoc.Element(ns + "Project").Elements(ns + "PropertyGroup").Elements(ns + "OutputPath")
				select el;

			foreach (XElement el in OutputPaths)
			{
				if (el.Value != outputpath)
				{
					Console.WriteLine("XXX: '" + el.Value + "' -> '" + outputpath + "'");
					el.Value = outputpath;
				}
			}

			return;
		}

		/*private static string NullFix(string s)
		{
				if (s == null)
						return "<null>";
				else
						return "'" + s + "'";
		}*/
	}
}
