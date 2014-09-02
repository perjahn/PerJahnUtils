using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Xml.Linq;

namespace CheckNamespace
{
	class Project
	{
		public string _sln_package { get; set; }
		public string _sln_path { get; set; }
		public string _rootnamespace { get; set; }
		public List<string> _allfiles { get; set; }

		private static string[] excludedtags = {
			"Reference", "Folder", "Service", "BootstrapperPackage", "CodeAnalysisDependentAssemblyPaths",
			"WCFMetadata", "WebReferences", "WCFMetadataStorage", "WebReferenceUrl" };

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
				throw new ApplicationException("Couldn't load project: '" + fullfilename + "': " + ex.Message);
			}
			catch (UnauthorizedAccessException ex)
			{
				throw new ApplicationException("Couldn't load project: '" + fullfilename + "': " + ex.Message);
			}
			catch (ArgumentException ex)
			{
				throw new ApplicationException("Couldn't load project: '" + fullfilename + "': " + ex.Message);
			}
			catch (System.Xml.XmlException ex)
			{
				throw new ApplicationException("Couldn't load project: '" + fullfilename + "': " + ex.Message);
			}

			ns = xdoc.Root.Name.Namespace;

			// File names are, believe it or not, percent encoded. Although space is encoded as space, not as +.

			IEnumerable<string> namespaces =
					from el in xdoc.Element(ns + "Project").Elements(ns + "PropertyGroup").Elements(ns + "RootNamespace")
					select el.Value;

			int count = namespaces.Count();
			if (count < 1)
			{
				ConsoleHelper.WriteLineColor(projectfilepath + ": No RootNamespace found.", ConsoleColor.Red);
			}
			else if (count > 1)
			{
				ConsoleHelper.WriteLineColor(projectfilepath + ": " + count + " RootNamespace found.", ConsoleColor.Red);
			}
			else
			{
				newproj._rootnamespace = namespaces.Single();
			}

			newproj._allfiles =
					(from el in xdoc.Element(ns + "Project").Elements(ns + "ItemGroup").Elements()
					 where el.Attribute("Include") != null && !excludedtags.Contains(el.Name.LocalName)
					 orderby el.Attribute("Include").Value
					 select System.Uri.UnescapeDataString(el.Attribute("Include").Value))
					 .ToList();


			return newproj;
		}

		public int CheckNamespace(string solutionfile)
		{
			int failcount = 0;

			foreach (string filename in _allfiles.Where(f => System.IO.Path.GetExtension(f).ToLower() == ".cs"))
			{
				// Files must exist in file system.
				string fullfilename = Path.Combine(
						Path.GetDirectoryName(solutionfile),
						Path.GetDirectoryName(_sln_path),
						filename);
				if (!File.Exists(fullfilename))
				{
					ConsoleHelper.WriteLineColor(
							"File not found: Project path: '" + _sln_path + "', File path: '" + filename + "'.",
							ConsoleColor.Red);
					return 0;
				}

				string[] rows = System.IO.File.ReadAllLines(fullfilename);
				int rownum = 1;
				foreach (string row in rows)
				{
					if (row.TrimStart().StartsWith("namespace"))
					{
						string ns = row.TrimStart().Substring(9).TrimStart();
						int index = ns.IndexOfAny(new char[] { ' ', '\t' });
						if (index != -1)
						{
							ns = ns.Substring(0, index);
						}
						if (ns != _rootnamespace && !ns.StartsWith(_rootnamespace + "."))
						{
							Console.WriteLine("Inconsistent namespace: " + _sln_path + ", '" + filename + "' (" + rownum + "): '" + _rootnamespace + "' <-> '" + ns + "'");
							failcount++;
						}
					}
					rownum++;
				}
			}

			return failcount;
		}
	}
}
