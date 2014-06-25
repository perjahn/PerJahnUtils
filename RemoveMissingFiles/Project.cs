using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Xml.Linq;

namespace RemoveMissingFiles
{
	class Project
	{
		public string _sln_package { get; set; }
		public string _sln_path { get; set; }
		public List<string> _allfiles { get; set; }
		public int _removedfiles { get; set; }

		private static string[] excludedtags = { "Reference", "Folder", "Service" };

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

			newproj._allfiles =
				(from el in xdoc.Element(ns + "Project").Elements(ns + "ItemGroup").Elements()
				 where el.Attribute("Include") != null && !excludedtags.Contains(el.Name.LocalName)
				 orderby el.Attribute("Include").Value
				 select System.Uri.UnescapeDataString(el.Attribute("Include").Value))
				 .ToList();


			return newproj;
		}

		public void Fix(string solutionfile, List<Project> projects)
		{
			List<string> existingfiles = new List<string>();

			foreach (string include in _allfiles)
			{
				// Files must exist in file system.
				string fullfilename = Path.Combine(
					Path.GetDirectoryName(solutionfile),
					Path.GetDirectoryName(_sln_path),
					include);
				if (!File.Exists(fullfilename))
				{
					ConsoleHelper.WriteLineColor(
						"File not found: Project path: '" + _sln_path + "', File path: '" + include + "'.",
						ConsoleColor.Red);
					_removedfiles++;
				}
				else
				{
					existingfiles.Add(include);
				}
			}

			_allfiles = existingfiles;

			return;
		}

		public void WriteProject(string solutionfile, bool simulate)
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


			UpdateFiles(xdoc, solutionfile);


			Console.WriteLine("Writing file: '" + fullfilename + "'.");
			if (!simulate)
			{
				FileHelper.RemoveRO(fullfilename);
				xdoc.Save(fullfilename);
			}

			return;
		}

		// Todo: check case sensitivity
		public void UpdateFiles(XDocument xdoc, string solutionfile)
		{
			XNamespace ns = xdoc.Root.Name.Namespace;

			List<XElement> fileitems =
				(from el in xdoc.Element(ns + "Project").Elements(ns + "ItemGroup").Elements()
				 where el.Attribute("Include") != null && !excludedtags.Contains(el.Name.LocalName)
				 orderby el.Attribute("Include").Value
				 select el)
				 .ToList();

			foreach (XElement fileitem in fileitems)
			{
				string filename = System.Uri.UnescapeDataString(fileitem.Attribute("Include").Value);

				if (!_allfiles.Contains(filename))
				{
					//Console.WriteLine("Removing file: '" + filename + "'");
					fileitem.Remove();
				}
			}

			return;
		}
	}
}
