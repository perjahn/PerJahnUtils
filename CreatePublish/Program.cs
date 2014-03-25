using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml.Linq;

namespace CreatePublish
{
	class Program
	{
		private static string eol = Environment.NewLine;

		static void Main(string[] args)
		{
			string usage =
@"CreatePublish 1.1 - Program for creating msbuild publishing script of Web/MVC projects.

Usage: CreatePublish <solutionfile> <msbuildfile> <publishfolder>

Example: CreatePublish mysol.sln publishmvc.proj ..\Deploy";

			if (args.Length != 3)
			{
				Console.WriteLine(usage);
				return;
			}


			string solutionfile = args[0];
			string buildfile = args[1];
			string publishfolder = args[2];

			CreateBuildFile(solutionfile, buildfile, publishfolder);
		}

		static void CreateBuildFile(string solutionfile, string buildfile, string publishfolder)
		{
			Solution s = new Solution(solutionfile);

			string[] webmvcguids =
			{
				"{603C0E0B-DB56-11DC-BE95-000D561079B0}",
				"{F85E285D-A4E0-4152-9332-AB1D724D3325}",
				"{E53F8FEA-EAE0-44A6-8774-FFD645390401}",
				"{E3E379DF-F4C6-4180-9B81-6769533ABE47}",
				"{349C5851-65DF-11DA-9384-00065B846F21}"
			};
			webmvcguids = webmvcguids.Select(g => g.ToLower()).ToArray();

			List<Project> projects = s.LoadProjects();
			if (projects == null)
			{
				return;
			}

			List<Project> webmvcprojects = new List<Project>();

			foreach (Project project in projects)
			{
				if (project._ProjectTypeGuids.Select(g => g.ToLower()).Any(webmvcguids.Contains))
				{
					webmvcprojects.Add(project);
				}
			}


			if (webmvcprojects.Count == 0)
			{
				Console.WriteLine("No Web/MVC projects found.");
				return;
			}

			string xml1 = "<Project xmlns=\"http://schemas.microsoft.com/developer/msbuild/2003\">" + Environment.NewLine + "  <Target Name=\"Build\">" + Environment.NewLine;
			string xml2 = "  </Target>" + Environment.NewLine + "</Project>" + Environment.NewLine;

			string buf = xml1;
			string solutionname = Path.GetFileNameWithoutExtension(solutionfile).Replace(".", "");

			foreach (Project project in webmvcprojects.OrderBy(p => Path.GetFileNameWithoutExtension(p._sln_path)))
			{
				string filename = project._sln_path;

				if (filename.StartsWith(@".\"))
				{
					filename = filename.Substring(2);
				}

				string folder = FileHelper.GetRelativePath(Path.Combine(Path.GetDirectoryName(buildfile), project._sln_path), publishfolder);

				string projname = Path.GetFileNameWithoutExtension(project._sln_path).Replace(".", "");
				if (projname.ToLower().StartsWith(solutionname.ToLower()) && projname.Length > solutionname.Length)
				{
					projname = projname.Substring(solutionname.Length);
				}
                projname = projname.Trim();

				folder = Path.Combine(folder, projname);

				buf += "    <MSBuild Projects=\"" + filename + "\" Targets=\"PipelinePreDeployCopyAllFilesToOneFolder\" Properties=\"Configuration=Release;_PackageTempDir=" + folder + "\" />" + Environment.NewLine;
			}

			buf += xml2;

			File.WriteAllText(buildfile, buf);
		}
	}
}
