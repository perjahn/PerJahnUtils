using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SyncFiles
{
	class Program
	{
		static void Log(string message, bool verbose = false)
		{
			LogWriter.WriteLine(message, verbose);
		}

		static void Main(string[] args)
		{
			Run(args);

			if (Environment.UserInteractive)
			{
				Log(Environment.NewLine + "Press Enter to continue...");
				Console.ReadLine();
			}
		}

		static void Run(string[] args)
		{
			LogWriter.logfile = "logfile_" + DateTime.Now.ToString("HHmmss") + ".txt";

			args = ParseOptions(args);

			if (args == null || args.Length != 4)
			{
				string usage =
@"SyncFiles 1.2

Usage: SyncFiles [-d] [-eEXCLUDE] [-iIDENTIFERFILE] [-mMAXSIZE] [-s] <sourcefile> <targetfile> <sourcepath> <targetpath>

-d  Also compare actual metadata (filetime+filesize) from file systems before copying.
-e  Exclude file pattern.
-i  File including an identifer per line.
-m  Max file size in bytes.
-s  Simulate.";

				Log(usage);
				return;
			}

			CopyFiles.SyncFiles(args[0], args[1], args[2], args[3]);
		}

		static string[] ParseOptions(string[] args)
		{
			if (args.Contains("-d"))
			{
				CopyFiles.compareMetadata = true;
				args = args.Except(new string[] { "-d" }).ToArray();
			}
			else
			{
				CopyFiles.compareMetadata = false;
			}


			string[] argsExclude = args.Where(a => a.ToLower().StartsWith("-e")).ToArray();
			if (argsExclude.Length > 1)
			{
				return null;
			}
			if (argsExclude.Length == 1)
			{
				CopyFiles.exclude = argsExclude[0].Substring(2);
				args = args.Where(a => !a.ToLower().StartsWith("-e")).ToArray();
			}


			string[] argsIdentifierfile = args.Where(a => a.ToLower().StartsWith("-i")).ToArray();
			if (argsIdentifierfile.Length > 1)
			{
				return null;
			}
			if (argsIdentifierfile.Length == 1)
			{
				string identifierfile = argsIdentifierfile[0].Substring(2);
				try
				{
					CopyFiles.identifiers = File.ReadAllLines(identifierfile).Where(l => l != string.Empty).ToArray();
				}
				catch (FileNotFoundException ex)
				{
					LogWriter.WriteConsoleColor(ex.Message, ConsoleColor.Red);
					return null;
				}

				args = args.Where(a => !a.ToLower().StartsWith("-i")).ToArray();
			}


			string[] argsMaxsize = args.Where(a => a.ToLower().StartsWith("-m")).ToArray();
			if (argsMaxsize.Length > 1)
			{
				return null;
			}
			if (argsMaxsize.Length == 1)
			{
				long result;
				if (argsMaxsize[0].Length < 3 || !long.TryParse(argsMaxsize[0].Substring(2), out result))
				{
					Console.WriteLine("Invalid maxsize value specified: '" + argsMaxsize[0] + "'." + Environment.NewLine);
					return null;
				}

				CopyFiles.maxsize = result;
				args = args.Where(a => !a.ToLower().StartsWith("-m")).ToArray();
			}


			if (args.Contains("-s"))
			{
				CopyFiles.simulate = true;
				args = args.Except(new string[] { "-s" }).ToArray();
			}
			else
			{
				string envWhatIf = Environment.GetEnvironmentVariable("WhatIf");
				CopyFiles.simulate = string.IsNullOrWhiteSpace(envWhatIf) || envWhatIf == "false" ? false : true;
			}


			if (args.Contains("-v"))
			{
				LogWriter.verbose = true;
				args = args.Except(new string[] { "-v" }).ToArray();
			}
			else
			{
				string envVerbose = Environment.GetEnvironmentVariable("Verbose");
				LogWriter.verbose = string.IsNullOrWhiteSpace(envVerbose) || envVerbose == "false" ? false : true;
			}


			return args;
		}

	}
}
