using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Linq;

namespace FixSolutionDir
{
	class Program
	{
		static void Main(string[] args)
		{
			string usage =
@"FixSolutionDir 0.001 gamma - Replaces $(SolutionDir) in PostBuildEvent with current dir.

Usage: FixSolutionDir <projectfile>";

			if (args.Length != 1)
			{
				Console.WriteLine(usage);
				return;
			}

			FixSolutionDir(args[0]);
		}

		static void FixSolutionDir(string filename)
		{
			XDocument xdoc;

			try
			{
				xdoc = XDocument.Load(filename);
			}
			catch (IOException ex)
			{
				Console.Write("Couldn't load project: '" + filename + "': " + ex.Message);
				return;
			}
			catch (UnauthorizedAccessException ex)
			{
				Console.Write("Couldn't load project: '" + filename + "': " + ex.Message);
				return;
			}
			catch (ArgumentException ex)
			{
				Console.Write("Couldn't load project: '" + filename + "': " + ex.Message);
				return;
			}
			catch (XmlException ex)
			{
				Console.Write("Couldn't load project: '" + filename + "': " + ex.Message);
				return;
			}

			XNamespace ns = xdoc.Root.Name.Namespace;

			IEnumerable<XElement> events = xdoc.Descendants(ns + "PostBuildEvent");

			bool modified = false;

			foreach (XElement eventnode in events)
			{
				string innertext = eventnode.Value;

				eventnode.Value = innertext.Replace("$(SolutionDir)", Environment.CurrentDirectory + Path.DirectorySeparatorChar);
				if (eventnode.Value != innertext)
				{
					modified = true;
				}
			}

			if (modified)
			{
				Console.WriteLine("Updating: '" + filename + "'");

				FileAttributes fa = File.GetAttributes(filename);
				if ((fa & FileAttributes.ReadOnly) == FileAttributes.ReadOnly)
				{
					File.SetAttributes(filename, fa & ~FileAttributes.ReadOnly);
				}

				xdoc.Save(filename);
			}

			return;
		}
	}
}
