using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CheckNamespace
{
	class Program
	{
		static int Main(string[] args)
		{
			int result = RemoveFiles(args);

			if (Environment.UserInteractive)
			{
				Console.WriteLine(Environment.NewLine + "Press any key to continue...");
				Console.ReadKey(true);
			}

			return result;
		}

		static int RemoveFiles(string[] args)
		{
			string usage = @"RemoveMissingFiles 1.0

Usage: CheckNamespace <solution file>

Example: CheckNamespace hello.sln";


			if (args.Length != 1)
			{
				Console.WriteLine(usage);
				return 1;
			}

			string solutionfile = args[0];


			Solution s = new Solution(solutionfile);

			try
			{
				s.LoadProjects();
			}
			catch (ApplicationException)
			{
				return 2;
			}

			s.CheckProjects();


			return 0;
		}

	}
}
