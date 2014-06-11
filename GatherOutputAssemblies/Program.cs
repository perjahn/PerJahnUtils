﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;

namespace GatherOutputAssemblies
{
	class Program
	{
		static int Main(string[] args)
		{
			// Make all string comparisons (and sort/order) invariant of current culture
			// Thus, project output files is written in a consistent manner
			System.Threading.Thread.CurrentThread.CurrentCulture = System.Globalization.CultureInfo.InvariantCulture;

			string usage =
@"GatherOutputAssemblies 1.0 - Program for gathering compiled output from Visual Studio.

Usage: GatherOutputAssemblies <solutionfile> <buildconfig> <outputfolder> +include1... -exclude1...

Example: GatherOutputAssemblies mysol.sln ""Release|AnyCPU"" artifacts";

			List<string> includeProjects =
				args
				.Where(a => a.StartsWith("+"))
				.Select(a => a.Substring(1))
				.ToList();

			List<string> excludeProjects =
				args
				.Where(a => a.StartsWith("-"))
				.Select(a => a.Substring(1))
				.ToList();

			args =
				args
				.Where(a => !a.StartsWith("+"))
				.Where(a => !a.StartsWith("-"))
				.ToArray();

			if (args.Length != 3)
			{
				Console.WriteLine(usage);
				return 0;
			}

			string solutionfile = args[0];
			string buildconfig = args[1];
			string outputpath = args[2];


			Solution s = new Solution(solutionfile);

			List<Project> projects = s.LoadProjects();
			if (projects == null)
			{
				return 1;
			}

			int result = s.CopyProjectOutput(projects, buildconfig, outputpath, includeProjects, excludeProjects);

			return result;
		}
	}
}