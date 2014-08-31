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
		public static extern IntPtr CreateFile(
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


		private static int Main(string[] args)
		{
			string usage =
@"DllDep 1.7

Usage: DllDep [path] [-rExcludeReferences] [-aExcludeAssemblies]
path:         Path to directory of assemblies.
exclude references: Comma separated list of references to exclude (without "".dll"").
exclude assemblies: Comma separated list of assemblies to exclude (without "".dll"").

Return values:
 3 - Fatal errors occured.
 2 - Missing dll files.
 1 - Version mismatch (no missing files).
 0 - Have a nice day!";

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
				Console.WriteLine("Press eny key to exit!");
				Console.ReadKey();
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
