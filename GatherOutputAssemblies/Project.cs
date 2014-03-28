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
		public List<string> _proj_assemblynames { get; set; }  // Compacted into _proj_assemblyname after load.

		public List<OutputPath> _outputpaths { get; set; }
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


			newproj._outputpaths =
				(from el in xdoc.Element(ns + "Project").Elements(ns + "PropertyGroup").Elements(ns + "OutputPath")
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

		public bool CopyOutput(string solutionfile, string buildconfig, string targetpath)
		{
			var outputpaths = _outputpaths.Where(o => MatchCondition(o.Condition, buildconfig, false));

			if (outputpaths.Count() > 1)
			{
				outputpaths = _outputpaths.Where(o => MatchCondition(o.Condition, buildconfig, true));
			}

			if (outputpaths.Count() != 1)
			{
				ConsoleHelper.ColorWrite(ConsoleColor.Red, "'" + _sln_path + "': Couldn't find an unambiguous PropertyGroup Condition");
				return false;
			}

			string sourcepath = Path.Combine(Path.GetDirectoryName(solutionfile), Path.GetDirectoryName(_sln_path), outputpaths.Single().Path);

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
