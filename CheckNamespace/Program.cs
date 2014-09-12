using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CheckNamespace
{
	class Program
	{
		static int Main(string[] args)
		{
			int result = CheckNamespace(args);

			return result;
		}

		static int CheckNamespace(string[] args)
		{
			string usage = @"CheckNamespace 1.1

Usage: CheckNamespace <solution files...>

Example: powershell CheckNamespace (dir C:\git\mycode -i *.sln -r)";


			if (args.Length == 0)
			{
				Console.WriteLine(usage);
				return 1;
			}


			List<Solution> solutions = LoadSolutions(args).ToList();

			List<Project> projects = solutions.SelectMany(s => s._projects).ToList();

			Console.WriteLine("Total projects: " + projects.Count);

			projects = projects
				.GroupBy(p => Path.Combine(Path.GetDirectoryName(p._solutionfile), p._sln_path))
				.Select(g => g.First())
				.ToList();

			Console.WriteLine("Unique projects: " + projects.Count);


			int failcount = 0;

			foreach (Project p in projects.OrderBy(p => Path.Combine(Path.GetDirectoryName(p._solutionfile), p._sln_path)))
			{
				failcount += p.CheckNamespace();
			}

			ConsoleHelper.WriteLineColor("Total inconsistencies: " + failcount, ConsoleColor.Cyan);

			return 0;
		}

		static private IEnumerable<Solution> LoadSolutions(string[] solpaths)
		{
			foreach (string path in solpaths)
			{
				Solution s;

				try
				{
					s = new Solution(path);
				}
				catch (ApplicationException ex)
				{
					ConsoleHelper.WriteLineColor(ex.Message, ConsoleColor.Red);
					continue;
				}

				yield return s;
			}
		}
	}
}
