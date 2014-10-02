using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;

namespace DllDep
{
	class Program
	{
		// CharSet = CharSet.Auto,
		[DllImport("kernel32.dll", SetLastError = true)]
		static extern IntPtr CreateFile(
			string filename,
			uint access,
			uint share,
			IntPtr securityAttributes,
			uint creationDisposition,
			uint flagsAndAttributes,
			IntPtr templateFile);

		[DllImport("kernel32.dll", SetLastError = true)]
		static extern bool CloseHandle(IntPtr hObject);

		const uint GENERIC_READ = 0x80000000;
		const uint FILE_SHARE_READ = 1;
		const uint OPEN_EXISTING = 3;

		static bool _verbose = false;

		private static int Main(string[] args)
		{
			string usage =
@"DllDep 1.9

Usage: DllDep [-v] [path] [-rExcludeReferences] [-aExcludeAssemblies]
path:         Path to directory of assemblies.
exclude references: Comma separated list of references to exclude (without "".dll"").
exclude assemblies: Comma separated list of assemblies to exclude (without "".dll"").

-v:           Verbose logging.

Return values:
 3 - Fatal errors occured.
 2 - Missing dll files.
 1 - Version mismatch (no missing files).
 0 - Have a nice day!";

			string path = null;
			string[] excludeReferences = null, excludeAssemblies = null;

			for (int arg = 0; arg < args.Length; arg++)
			{
				if (args[arg].StartsWith("-v"))
				{
					_verbose = true;
				}
				else if (args[arg].StartsWith("-r") && excludeReferences == null)
				{
					excludeReferences = args[arg].Substring(2).Split(',');
				}
				else if (args[arg].StartsWith("-a") && excludeAssemblies == null)
				{
					excludeAssemblies = args[arg].Substring(2).Split(',');
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
				path = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
			}


			int result;

			if (!Directory.Exists(path))
			{
				ColorWriteLine(ConsoleColor.Red, "ERROR: Couldn't find directory: '" + path + "'");
				return 3;
			}

			if (args.Length < 1)
			{
				Console.WriteLine(usage);
			}

			if (excludeReferences == null)
			{
				excludeReferences = new string[] { };
			}
			if (excludeAssemblies == null)
			{
				excludeAssemblies = new string[] { };
			}

			result = ParseDirectory(path, excludeReferences, excludeAssemblies);

			if (args.Length < 1)
			{
				Console.WriteLine("Press eny key to exit!");
				Console.ReadKey();
			}

			return result;
		}

		private static int ParseDirectory(string path, string[] excludeReferences, string[] excludeAssemblies)
		{
			bool mismatch = false;
			bool missing = false;


			ColorWriteLine(ConsoleColor.Cyan, "Parsing directory: '" + path + "'");

			var rootAssemblies = GetRootAssemblies(path);


			List<string> excludedAssemblies = excludeAssemblies.ToList();
			List<string> excludedReferences = excludeReferences.ToList();

			foreach (Assembly assembly in rootAssemblies.OrderBy(a => a.FullName))
			{
				AssemblyName ass1 = assembly.GetName();

				if (IncludeAssembly(ass1))
				{
					if (ass1.Name != "vshost" && ass1.Name != "vshost32")
					{
						if (!excludeAssemblies.Contains(ass1.Name))
						{
							Console.WriteLine("{0} - {1}", ass1.Name, ass1.Version.ToString());
						}

						foreach (AssemblyName ass2 in assembly.GetReferencedAssemblies().OrderBy(a => a.FullName))
						{
							if (IncludeAssembly(ass2))
							{
								if (rootAssemblies.Any(a => a.GetName().FullName == ass2.FullName))
								{
									if (excludeAssemblies.Contains(ass1.Name) || excludeReferences.Contains(ass2.Name))
									{
										continue;
									}

									Console.WriteLine("\t{0} - {1}", ass2.Name, ass2.Version.ToString());
								}
								else if (rootAssemblies.Any(a => a.GetName().Name == ass2.Name))
								{
									if (excludedAssemblies.Contains(ass1.Name))
									{
										excludedAssemblies.Remove(ass1.Name);
									}
									else if (excludedReferences.Contains(ass2.Name))
									{
										excludedReferences.Remove(ass2.Name);
									}

									if (excludeAssemblies.Contains(ass1.Name) || excludeReferences.Contains(ass2.Name))
									{
										continue;
									}

									Version existingfilever = GetAssemblyVersion(rootAssemblies, ass2);
									mismatch = true;
									ColorWrite(ConsoleColor.Yellow, "ERROR:\t{0} - {1} ({2} found on disk)",
										ass2.Name, ass2.Version.ToString(), existingfilever.ToString());
								}
								else
								{
									if (excludedAssemblies.Contains(ass1.Name))
									{
										excludedAssemblies.Remove(ass1.Name);
									}
									else if (excludedReferences.Contains(ass2.Name))
									{
										excludedReferences.Remove(ass2.Name);
									}

									if (excludeAssemblies.Contains(ass1.Name) || excludeReferences.Contains(ass2.Name))
									{
										continue;
									}

									missing = true;
									ColorWrite(ConsoleColor.Red, "ERROR:\t{0} - {1}",
										ass2.Name, ass2.Version.ToString());
								}
							}
						}

						Console.WriteLine();
					}
				}
			}

			CheckExcessive("Excessive excluded Assemblies:", excludedAssemblies);
			CheckExcessive("Excessive excluded References:", excludedReferences);

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
					catch (System.Exception ex)
					{
						// Ignore junk files
						if (_verbose)
						{
							ColorWriteLine(ConsoleColor.Red, ex.ToString());
						}

						if (IsFileBlocked(filename))
						{
							ColorWriteLine(ConsoleColor.Red, "File is blocked: '" + filename + "'");
						}
					}
					if (ass != null)
					{
						yield return ass;
					}
				}
			}
		}

		private static bool IsFileBlocked(string filename)
		{
			PlatformID p = Environment.OSVersion.Platform;
			if (p == PlatformID.Win32NT || p == PlatformID.Win32S || p == PlatformID.Win32Windows || p == PlatformID.WinCE)
			{
				IntPtr hStream = CreateFile(filename + ":Zone.Identifier", GENERIC_READ, FILE_SHARE_READ, IntPtr.Zero, OPEN_EXISTING, 0, IntPtr.Zero);
				IntPtr INVALID_HANDLE_VALUE = new IntPtr(-1);
				if (hStream != INVALID_HANDLE_VALUE)
				{
					CloseHandle(hStream);
					return true;
				}
				CloseHandle(hStream);
			}
			return false;
		}

		private static bool IncludeAssembly(AssemblyName assembyName)
		{
			if (assembyName != null)
			{
				string name = assembyName.Name;
				string dummy;
				if (!gac.IsSystemAssembly(name, out dummy, true))
				{
					return true;
				}
			}
			return false;
		}

		private static void CheckExcessive(string msg, List<string> excludedAssemblies)
		{
			if (excludedAssemblies.Count() > 0)
			{
				ColorWrite(ConsoleColor.Red, msg);
			}

			foreach (string ass in excludedAssemblies)
			{
				Console.WriteLine(ass);
			}
		}
	}
}
