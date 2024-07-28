using System;
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
            var success = Run(args);
            if (success)
            {
                Log($"-=-=- Ending: {DateTime.Now:yyyyMMdd HHmmss} -=-=-");
            }

            if (Environment.UserInteractive)
            {
                Log($"{Environment.NewLine}Press Enter to continue...");
                Console.ReadLine();
            }
        }

        static bool Run(string[] args)
        {
            var parsedArgs = ParseOptions(args);

            if (parsedArgs == null || parsedArgs.Length != 4)
            {
                var usage =
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

            Log($"-=-=- Starting: {DateTime.Now:yyyyMMdd HHmmss} -=-=-");

            CopyFiles.SyncFiles(parsedArgs[0], parsedArgs[1], parsedArgs[2], parsedArgs[3]);

            return true;
        }

        static string[]? ParseOptions(string[] args)
        {
            var parsedArgs = args;

            if (parsedArgs.Contains("-d"))
            {
                CopyFiles.CompareMetadata = true;
                parsedArgs = [.. parsedArgs.Except(["-d"])];
            }
            else
            {
                CopyFiles.CompareMetadata = false;
            }

            string[] argsExcludes = [.. parsedArgs.Where(a => a.StartsWith("-e"))];
            CopyFiles.Excludes = [.. argsExcludes.Select(a => a[2..])];
            parsedArgs = [.. parsedArgs.Where(a => !a.StartsWith("-e"))];

            string[] argsIdentifierfile = [.. parsedArgs.Where(a => a.StartsWith("-i"))];
            if (argsIdentifierfile.Length > 1)
            {
                return null;
            }
            if (argsIdentifierfile.Length == 1)
            {
                string identifierfile = argsIdentifierfile[0][2..];
                try
                {
                    CopyFiles.Identifiers = [.. File.ReadAllLines(identifierfile).Where(l => l != string.Empty)];
                }
                catch (FileNotFoundException ex)
                {
                    LogWriter.WriteConsoleColor(ex.Message, ConsoleColor.Red);
                    return null;
                }

                parsedArgs = [.. parsedArgs.Where(a => !a.StartsWith("-i"))];
            }

            string[] argsLogpath = [.. parsedArgs.Where(a => a.StartsWith("-l"))];
            if (argsLogpath.Length > 1)
            {
                return null;
            }
            var logfile = $"SyncFiles_{DateTime.Now:yyyyMMdd}.txt";
            if (argsLogpath.Length == 1)
            {
                var logpath = argsLogpath[0][2..];
                if (!Directory.Exists(logpath))
                {
                    Console.WriteLine($"Creating log folder: '{logpath}'.{Environment.NewLine}");
                    Directory.CreateDirectory(logpath);
                }

                LogWriter.Logfile = Path.Combine(logpath, logfile);
                parsedArgs = [.. parsedArgs.Where(a => !a.StartsWith("-l"))];
            }
            else
            {
                LogWriter.Logfile = logfile;
            }

            string[] argsMaxsize = [.. parsedArgs.Where(a => a.StartsWith("-m"))];
            if (argsMaxsize.Length > 1)
            {
                return null;
            }
            if (argsMaxsize.Length == 1)
            {
                if (argsMaxsize[0].Length < 3 || !long.TryParse(argsMaxsize[0][2..], out long result))
                {
                    Console.WriteLine($"Invalid maxsize value specified: '{argsMaxsize[0]}'.{Environment.NewLine}");
                    return null;
                }

                CopyFiles.Maxsize = result;
                parsedArgs = [.. parsedArgs.Where(a => !a.StartsWith("-m"))];
            }

            if (parsedArgs.Contains("-s"))
            {
                CopyFiles.Simulate = true;
                parsedArgs = [.. parsedArgs.Except(["-s"])];
            }
            else
            {
                var envWhatIf = Environment.GetEnvironmentVariable("WhatIf");
                CopyFiles.Simulate = !string.IsNullOrWhiteSpace(envWhatIf) && envWhatIf != "false";
            }

            if (parsedArgs.Contains("-v"))
            {
                LogWriter.Verbose = true;
                parsedArgs = [.. parsedArgs.Except(["-v"])];
            }
            else
            {
                var envVerbose = Environment.GetEnvironmentVariable("Verbose");
                LogWriter.Verbose = !string.IsNullOrWhiteSpace(envVerbose) && envVerbose != "false";
            }

            return parsedArgs;
        }
    }
}
