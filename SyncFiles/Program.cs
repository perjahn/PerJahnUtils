using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

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
            bool result = Run(args);
            if (result)
            {
                Log($"-=-=- Ending: {DateTime.Now.ToString("yyyyMMdd HHmmss")} -=-=-");
            }

            if (Environment.UserInteractive)
            {
                Log($"{Environment.NewLine}Press Enter to continue...");
                Console.ReadLine();
            }
        }

        static bool Run(string[] args)
        {
            args = ParseOptions(args);

            if (args == null || args.Length != 4)
            {
                string usage =
@"SyncFiles 1.2

Usage: SyncFiles [-d] [-eEXCLUDE] [-iIDENTIFERFILE] [-lLOGPATH] [-mMAXSIZE] [-s] <sourcefile> <targetfile> <sourcepath> <targetpath>

-d  Also compare actual metadata (filetime+filesize) from file systems before copying.
-e  Exclude file regex pattern (multiple -e can be specified).
-i  File including an identifer per line.
-l  Log folder.
-m  Max file size in bytes.
-s  Simulate.";

                Log(usage);
                return false;
            }

            Log($"-=-=- Starting: {DateTime.Now.ToString("yyyyMMdd HHmmss")} -=-=-");

            CopyFiles.SyncFiles(args[0], args[1], args[2], args[3]);

            return true;
        }

        static string[] ParseOptions(string[] args)
        {
            if (args.Contains("-d"))
            {
                CopyFiles.CompareMetadata = true;
                args = args.Except(new string[] { "-d" }).ToArray();
            }
            else
            {
                CopyFiles.CompareMetadata = false;
            }


            string[] argsExcludes = args.Where(a => a.ToLower().StartsWith("-e")).ToArray();
            CopyFiles.Excludes = argsExcludes.Select(a => a.Substring(2)).ToArray();
            args = args.Where(a => !a.ToLower().StartsWith("-e")).ToArray();


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
                    CopyFiles.Identifiers = File.ReadAllLines(identifierfile).Where(l => l != string.Empty).ToArray();
                }
                catch (FileNotFoundException ex)
                {
                    LogWriter.WriteConsoleColor(ex.Message, ConsoleColor.Red);
                    return null;
                }

                args = args.Where(a => !a.ToLower().StartsWith("-i")).ToArray();
            }


            string[] argsLogpath = args.Where(a => a.ToLower().StartsWith("-l")).ToArray();
            if (argsLogpath.Length > 1)
            {
                return null;
            }
            string logfile = $"SyncFiles_{DateTime.Now:yyyyMMdd}.txt";
            if (argsLogpath.Length == 1)
            {
                string logpath = argsLogpath[0].Substring(2);
                if (!Directory.Exists(logpath))
                {
                    Console.WriteLine($"Creating log folder: '{logpath}'.{Environment.NewLine}");
                    Directory.CreateDirectory(logpath);
                }

                LogWriter.Logfile = Path.Combine(logpath, logfile);
                args = args.Where(a => !a.ToLower().StartsWith("-l")).ToArray();
            }
            else
            {
                LogWriter.Logfile = logfile;
            }


            string[] argsMaxsize = args.Where(a => a.ToLower().StartsWith("-m")).ToArray();
            if (argsMaxsize.Length > 1)
            {
                return null;
            }
            if (argsMaxsize.Length == 1)
            {
                if (argsMaxsize[0].Length < 3 || !long.TryParse(argsMaxsize[0].Substring(2), out long result))
                {
                    Console.WriteLine($"Invalid maxsize value specified: '{argsMaxsize[0]}'.{Environment.NewLine}");
                    return null;
                }

                CopyFiles.Maxsize = result;
                args = args.Where(a => !a.ToLower().StartsWith("-m")).ToArray();
            }


            if (args.Contains("-s"))
            {
                CopyFiles.Simulate = true;
                args = args.Except(new string[] { "-s" }).ToArray();
            }
            else
            {
                string envWhatIf = Environment.GetEnvironmentVariable("WhatIf");
                CopyFiles.Simulate = string.IsNullOrWhiteSpace(envWhatIf) || envWhatIf == "false" ? false : true;
            }


            if (args.Contains("-v"))
            {
                LogWriter.Verbose = true;
                args = args.Except(new string[] { "-v" }).ToArray();
            }
            else
            {
                string envVerbose = Environment.GetEnvironmentVariable("Verbose");
                LogWriter.Verbose = string.IsNullOrWhiteSpace(envVerbose) || envVerbose == "false" ? false : true;
            }


            return args;
        }
    }
}
