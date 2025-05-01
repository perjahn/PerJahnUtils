using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using System.Runtime.InteropServices;

namespace copysync
{
    class CopyOperation(string SourcePath, string DestinationPath)
    {
        public string SourcePath { get; set; } = SourcePath;
        public string DestinationPath { get; set; } = DestinationPath;
    }

    class Program
    {
        static bool _onlynewer;
        static bool _simulate;
        static bool _tfs;
        static bool _verbose;

        static int Main(string[] args)
        {
            var result = CopyNonInteractive(args);

            if (Environment.UserInteractive)
            {
                Console.WriteLine($"{Environment.NewLine}Press any key to continue...");
                _ = Console.ReadKey();
            }

            return result;
        }

        static int CopyNonInteractive(string[] args)
        {
            var usage = @"CopySync 1.3

Usage: copysync [-n] [-s] [-v] -[t] <source folder> <target folder> <exclude files>

Example: copysync C:\Projects\PlatformCode\*.dll C:\Projects\CustomerApp -*.resources.dll

-n:  Only copy files which are newer in source folder.
-s:  Perform a simulated copy without any side effects.
-t:  Generate TFS checkout script
-v:  Verbose logging";

            var parsedArgs = args;

            if (parsedArgs.Any(a => a == "-n"))
            {
                _onlynewer = true;
            }
            if (parsedArgs.Any(a => a == "-s"))
            {
                _simulate = true;
            }
            if (parsedArgs.Any(a => a == "-t"))
            {
                _tfs = true;
            }
            if (parsedArgs.Any(a => a == "-v"))
            {
                _verbose = true;
            }

            parsedArgs = [.. parsedArgs.Except(["-n", "-s", "-t", "-v"])];

            if (parsedArgs.Length < 2)
            {
                Console.WriteLine(usage);
                return 0;
            }

            List<CopyOperation> copyOperations;

            List<string> ExcludeFilePatterns = [];
            for (var i = 2; i < parsedArgs.Length; i++)
            {
                if (!parsedArgs[i].StartsWith('-'))
                {
                    Console.WriteLine(usage);
                    return 0;
                }

                ExcludeFilePatterns.Add(parsedArgs[i][1..]);
            }

            try
            {
                copyOperations = GetCopyOperations(parsedArgs[0], parsedArgs[1], ExcludeFilePatterns);
            }
            catch (IOException ex)
            {
                WriteError(ex.Message);
                return 1;
            }

            if (copyOperations == null)
            {
                return 2;
            }

            PerformCopy(copyOperations, parsedArgs[0], parsedArgs[1]);

            GenerateCheckoutScript(copyOperations);

            return 0;
        }

        static void WriteError(string s)
        {
            var oldcolor = Console.ForegroundColor;
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine(s);
            Console.ForegroundColor = oldcolor;
        }

        static void Log(string s)
        {
            if (_verbose)
            {
                Console.WriteLine(s);
            }
        }

        // path can be of pattern: 1. folder, 2. file, 3. pattern.
        // 1. c:\windows
        // 2. c:\windows\notepad.exe
        // 3. c:\windows\*.exe
        static string[] GetFiles(string path, List<string> ExcludeFilePatterns, out int excludedFiles)
        {
            Log($"Parsing path: '{path}'");

            string folder, pattern;

            if (path.Contains('*') || path.Contains('?'))
            {
                folder = Path.GetDirectoryName(path);
                pattern = Path.GetFileName(path);
            }
            else
            {
                folder = path;
                pattern = "*";

                if (Directory.Exists(path))
                {
                    folder = path;
                    pattern = "*";
                }
                else if (File.Exists(path))
                {
                    // ok - do nothing
                }
                else
                {
                    throw new DirectoryNotFoundException($"Path does not exist: '{path}'");
                }
            }

            if (!Directory.Exists(folder))
            {
                throw new DirectoryNotFoundException($"Folder does not exist: '{folder}'");
            }

            Console.WriteLine($"Path: '{folder}', Pattern: '{pattern}'");

            string[] files;
            List<Exception> errors = [];
            files = Directory.GetFiles(folder, pattern, SearchOption.AllDirectories);

            excludedFiles = 0;
            foreach (var ExcludeFilePattern in ExcludeFilePatterns)
            {
                Log($"Excluding pattern: '{ExcludeFilePattern}'");

                string[] ExcludeFiles = [.. Directory.GetFiles(folder, ExcludeFilePattern, SearchOption.AllDirectories)
                    .Select(Path.GetFileName)
                    .Distinct()];

                var oldcount = files.Length;
                files = [.. files.Where(f => !ExcludeFiles.Contains(Path.GetFileName(f)))];
                excludedFiles += oldcount - files.Length;
            }

            foreach (var ex in errors)
            {
                Console.WriteLine(ex.Message);
            }

            return files;
        }

        static List<CopyOperation> GetCopyOperations(string sourcePath, string destinationPath, List<string> ExcludeFilePatterns)
        {
            List<string> errors = [];

            var sourcePaths = GetFiles(sourcePath, ExcludeFilePatterns, out int excludedFiles);
            Console.WriteLine($"Total source files: {sourcePaths.Length} (excluded {excludedFiles}){Environment.NewLine}");

            var targetPaths = GetFiles(destinationPath, ExcludeFilePatterns, out excludedFiles);
            Console.WriteLine($"Total destination files: {targetPaths.Length} (excluded {excludedFiles}){Environment.NewLine}");

            List<CopyOperation> copyOperations = [];

            IGrouping<string, string>[] sourceFiles = [.. sourcePaths.GroupBy(f => Path.GetFileName(f).ToLower())];
            Console.WriteLine($"Source: Unique file names: {sourceFiles.Length}");

            var destinationFiles = targetPaths.ToLookup(f => Path.GetFileName(f).ToLower(), f => f);
            Console.WriteLine($"Destination: Unique file names: {destinationFiles.Count}{Environment.NewLine}");

            foreach (var fileNameGroup in sourceFiles)
            {
                var filename = fileNameGroup.Key;
                if (destinationFiles.Contains(filename) && AreFilesNewer([.. fileNameGroup], [.. destinationFiles[filename]]))
                {
                    if (AreFilesIdentical([.. fileNameGroup], ref errors))
                    {
                        foreach (var destination in destinationFiles[filename])
                        {
                            copyOperations.Add(new CopyOperation(fileNameGroup.First(), destination));
                        }
                    }
                }
            }

            if (errors.Count > 0)
            {
                foreach (var error in errors)
                {
                    WriteError(error);
                }
                return null;
            }

            return copyOperations;
        }

        static bool AreFilesNewer(List<string> SourceFileNames, List<string> DestinationFileNames)
        {
            if (_onlynewer)
            {
                var oldestSource = SourceFileNames
                    .Select(File.GetLastWriteTime)
                    .OrderBy(f => f)
                    .First();

                var newestDestination = DestinationFileNames
                    .Select(File.GetLastWriteTime)
                    .OrderBy(f => f)
                    .Last();

                if (oldestSource < newestDestination)
                {
                    return false;
                }
            }

            return true;
        }

        [DllImport("msvcrt.dll", CallingConvention = CallingConvention.Cdecl)]
        static extern int memcmp(byte[] b1, byte[] b2, long count);

        static bool AreFilesIdentical(List<string> FileNames, ref List<string> errors)
        {
            if (FileNames == null || FileNames.Count < 2)
            {
                return true;
            }

            byte[] content1 = null;

            for (var i = 0; i < FileNames.Count; i++)
            {
                Log($"Reading: '{FileNames[i]}'");

                if (content1 == null)
                {
                    content1 = File.ReadAllBytes(FileNames[i]);
                }
                else
                {
                    var content2 = File.ReadAllBytes(FileNames[i]);

                    if (content1.Length != content2.Length || memcmp(content1, content1, content1.Length) != 0)
                    {
                        errors.Add($"Two files in the source folder is different: '{FileNames[0]}' <-> '{FileNames[i]}': Please fix your broken source folder.");
                    }
                }
            }

            return true;
        }

        static void PerformCopy(List<CopyOperation> copyOperations, string sourcePath, string destinationPath)
        {
            var oldcolor = Console.ForegroundColor;
            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.WriteLine($"Copying {copyOperations.Count} files: '{sourcePath}' -> '{destinationPath}'");
            Console.ForegroundColor = oldcolor;

            foreach (var op in copyOperations)
            {
                Log($"Copying: '{op.SourcePath}' -> '{op.DestinationPath}'");

                if (!_simulate)
                {
                    if ((File.GetAttributes(op.DestinationPath) & FileAttributes.ReadOnly) != 0)
                    {
                        File.SetAttributes(op.DestinationPath, File.GetAttributes(op.DestinationPath) & ~FileAttributes.ReadOnly);
                    }
                    File.Copy(op.SourcePath, op.DestinationPath, true);
                }
            }
        }

        static void GenerateCheckoutScript(List<CopyOperation> copyOperations)
        {
            if (!_tfs)
            {
                return;
            }

            var batfile = "checkout.bat";
            using StreamWriter sw = new(batfile);
            foreach (var op in copyOperations)
            {
                var command = $"tf checkout \"{op.DestinationPath}\"";
                sw.WriteLine(command);
            }

            Console.WriteLine($"Wrote tfs commands to: {batfile}");
        }
    }
}
