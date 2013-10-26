using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;

namespace DllDep
{
	class Program
	{
		private static int Main(string[] args)
		{
			string usage =
					"DllDep 1.6\n" +
					"\n" +
					"Usage: DllDep [path] [-rExcludeReferences] [-aExcludeAssemblies]\n" +
					"path:         Path to directory of assemblies.\n" +
					"exclude references: Comma separated list of references to exclude (without \".dll\").\n" +
					"exclude assemblies: Comma separated list of assemblies to exclude (without \".dll\").\n" +
					"\n" +
					"Return values:\n" +
					" 3 - Fatal errors occured.\n" +
					" 2 - Missing dll files.\n" +
					" 1 - Version mismatch (no missing files).\n" +
					" 0 - Have a nice day!\n";

			string path = null;
			string[] excluderefs = null, excludeassemblies = null;

			for (int arg = 0; arg < args.Length; arg++)
			{
				if (args[arg].StartsWith("-r") && excluderefs == null)
				{
					excluderefs = args[arg].Substring(2).Split(',');
				}
				else if (args[arg].StartsWith("-a") && excludeassemblies == null)
				{
					excludeassemblies = args[arg].Substring(2).Split(',');
				}
				else if (path == null)
				{
					path = args[arg];
				}
				else
				{
					Console.WriteLine(usage);
					return 3;
				}
			}

			if (path == null)
			{
				path = System.IO.Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
			}


			int result;

			if (!System.IO.Directory.Exists(path))
			{
				ColorWriteLine(ConsoleColor.Red, "ERROR: Couldn't find directory: '" + path + "'");
				return 3;
			}

			if (args.Length < 1)
			{
				Console.WriteLine(usage);
			}

			if (excluderefs == null)
			{
				excluderefs = new string[] { };
			}
			if (excludeassemblies == null)
			{
				excludeassemblies = new string[] { };
			}

			result = ParseDirectory(path, excluderefs, excludeassemblies);

			if (args.Length < 1)
			{
				Console.WriteLine("Press Enter to exit!");
				Console.ReadLine();
			}

			return result;
		}

		private static int ParseDirectory(string path, string[] excluderefs, string[] excludeassemblies)
		{
			bool mismatch = false;
			bool missing = false;


			ColorWriteLine(ConsoleColor.Cyan, "Parsing directory: '" + path + "'");

			var rootAssemblies = GetRootAssemblies(path);


			foreach (Assembly assembly in rootAssemblies.OrderBy(a => a.FullName))
			{
				AssemblyName ass1 = assembly.GetName();

				if (IncludeAssembly(ass1, excludeassemblies))
				{
					if (ass1.Name != "vshost" && ass1.Name != "vshost32")
					{
						Console.WriteLine("{0} - {1}", ass1.Name, ass1.Version.ToString());
						foreach (AssemblyName ass2 in assembly.GetReferencedAssemblies().OrderBy(a => a.FullName))
						{
							if (IncludeAssembly(ass2, excluderefs))
							{
								if (rootAssemblies.Any(a => { return a.GetName().FullName == ass2.FullName; }))
								{
									Console.WriteLine("\t{0} - {1}", ass2.Name, ass2.Version.ToString());
								}
								else if (rootAssemblies.Any(a => { return a.GetName().Name == ass2.Name; }))
								{
									Version existingfilever = GetAssemblyVersion(rootAssemblies, ass2);
									mismatch = true;
									ColorWrite(ConsoleColor.Yellow, "ERROR:\t{0} - {1} ({2})",
											ass2.Name, ass2.Version.ToString(), existingfilever.ToString());
								}
								else
								{
									missing = true;
									ColorWrite(ConsoleColor.Red, "ERROR:\t{0} - {1}",
											ass2.Name, ass2.Version.ToString());
								}
							}
						}
					}
					Console.WriteLine();
				}
			}

			if (missing)
			{
				return 2;
			}
			if (mismatch)
			{
				return 1;
			}
			return 0;
		}

		private static void ColorWriteLine(ConsoleColor color, string s, params object[] args)
		{
			ColorWrite(color, s + Environment.NewLine, args);
		}

		private static void ColorWrite(ConsoleColor color, string s, params object[] args)
		{
			ConsoleColor oldColor = Console.ForegroundColor;
			Console.ForegroundColor = color;
			try
			{
				Console.WriteLine(s, args);
			}
			finally
			{
				Console.ForegroundColor = oldColor;
			}
		}

		private static Version GetAssemblyVersion(IEnumerable<Assembly> assemblies, AssemblyName assemblyName)
		{
			return (from a in assemblies
							where a.GetName().Name == assemblyName.Name
							select a.GetName().Version).First();
		}

		private static IEnumerable<Assembly> GetRootAssemblies(string path)
		{
			string[] exts = new string[] { "*.dll", "*.exe", "*.ocx" };
			foreach (string ext in exts)
			{
				foreach (string filename in Directory.GetFiles(path, ext))
				{
					Assembly ass = null;
					try
					{
						ass = Assembly.LoadFrom(filename);
					}
					catch
					{
						// Ignore junk files
					}
					if (ass != null)
					{
						yield return ass;
					}
				}
			}
		}

		private static bool IncludeAssembly(AssemblyName assembyName, string[] excludes)
		{
			if (assembyName != null)
			{
				string name = assembyName.Name;
				string dummy;
				if (!gac.IsSystemAssembly(name, out dummy, true))
				{
					if (!excludes.Contains(name))
					{
						return true;
					}
				}
			}
			return false;
		}
	}
}
