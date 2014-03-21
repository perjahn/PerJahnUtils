using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Runtime.InteropServices;

namespace copysync
{
    class CopyOperation
    {
        public string SourcePath { get; set; }
        public string DestinationPath { get; set; }

        public CopyOperation(string SourcePath, string DestinationPath)
        {
            this.SourcePath = SourcePath;
            this.DestinationPath = DestinationPath;
        }
    }

    class Program
    {
        static bool _onlynewer = false;
        static bool _simulate = false;
        static bool _tfs = false;
        static bool _verbose = false;

        static int Main(string[] args)
        {
            int result = CopyNonInteractive(args);

            if (Environment.UserInteractive)
            {
                Console.WriteLine(Environment.NewLine + "Press any key to continue...");
                Console.ReadKey();
            }

            return result;
        }

        static int CopyNonInteractive(string[] args)
        {
            string usage = @"CopySync 1.3

Usage: copysync [-n] [-s] [-v] -[t] <source folder> <target folder> <exclude files>

Example: copysync C:\Projects\PlatformCode\*.dll C:\Projects\CustomerApp -*.resources.dll

-n:  Only copy files which are newer in source folder.
-s:  Perform a simulated copy without any side effects.
-t:  Generate TFS checkout script
-v:  Verbose logging";


            if (args.Any(a => a == "-n"))
            {
                _onlynewer = true;
            }
            if (args.Any(a => a == "-s"))
            {
                _simulate = true;
            }
            if (args.Any(a => a == "-t"))
            {
                _tfs = true;
            }
            if (args.Any(a => a == "-v"))
            {
                _verbose = true;
            }

            args = args.Except(new string[] { "-n", "-s", "-t", "-v" }).ToArray();

            if (args.Length < 2)
            {
                Console.WriteLine(usage);
                return 0;
            }

            List<CopyOperation> copyOperations;

            List<string> ExcludeFilePatterns = new List<string>();
            for (int i = 2; i < args.Length; i++)
            {
                if (!args[i].StartsWith("-"))
                {
                    Console.WriteLine(usage);
                    return 0;
                }

                ExcludeFilePatterns.Add(args[i].Substring(1));
            }

            try
            {
                copyOperations = GetCopyOperations(args[0], args[1], ExcludeFilePatterns);
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

            PerformCopy(copyOperations, args[0], args[1]);

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
        static string[] GetFiles(string path, List<string> ExcludeFilePatterns)
        {
            Console.WriteLine("Parsing path: '" + path + "'");

            string folder = Path.GetDirectoryName(path);
            string pattern = Path.GetFileName(path);

            if (pattern.Contains('*') || pattern.Contains('?'))
            {
                if (!Directory.Exists(folder))
                {
                    throw new DirectoryNotFoundException("Folder does not exist: '" + folder + "'");
                }
            }
            else
            {
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
                    throw new DirectoryNotFoundException("Path does not exist: '" + path + "'");
                }
            }

            string[] files;
            List<Exception> errors = new List<Exception>();
            //files = Directory.GetFiles(folder, pattern, SearchOption.AllDirectories);
            files = GetFiles2(new DirectoryInfo(folder), pattern, out errors).ToArray();

            foreach (string ExcludeFilePattern in ExcludeFilePatterns)
            {
                List<string> ExcludeFiles = //Directory.GetFiles(folder, ExcludeFilePattern, SearchOption.AllDirectories)
                    GetFiles2(new DirectoryInfo(folder), pattern, out errors)
                        .Select(f => Path.GetFileName(f))
                        .Distinct()
                        .ToList();

                files = files.Where(f => !ExcludeFiles.Contains(Path.GetFileName(f))).ToArray();
            }

            foreach (Exception ex in errors)
            {
                Console.WriteLine(ex.Message);
            }


            return files;
        }

        // A working version of GetFiles, not the sucky one in .net framework.
        static List<string> GetFiles2(DirectoryInfo folder, string pattern, out List<Exception> errors)
        {
            List<string> files = new List<string>();
            errors = new List<Exception>();

            try
            {
                files = folder.GetFiles(pattern).Select(f => f.FullName).ToList();
            }
            catch (UnauthorizedAccessException ex)
            {
                errors.Add(ex);
            }


            try
            {
                DirectoryInfo[] subdirs = folder.GetDirectories(pattern);

                foreach (DirectoryInfo di in subdirs)
                {
                    List<Exception> subdirErrors;
                    files.AddRange(GetFiles2(di, pattern, out subdirErrors));
                    errors.AddRange(subdirErrors);
                }
            }
            catch (UnauthorizedAccessException ex)
            {
                errors.Add(ex);
            }

            return files;
        }

        static List<CopyOperation> GetCopyOperations(string sourcePath, string destinationPath, List<string> ExcludeFilePatterns)
        {
            List<string> errors = new List<string>();

            string[] sourcePaths = GetFiles(sourcePath, ExcludeFilePatterns);
            Console.WriteLine("Total source files: " + sourcePaths.Length + Environment.NewLine);

            string[] targetPaths = GetFiles(destinationPath, ExcludeFilePatterns);
            Console.WriteLine("Total destination files: " + targetPaths.Length + Environment.NewLine);

            List<CopyOperation> copyOperations = new List<CopyOperation>();


            var sourceFiles = sourcePaths.GroupBy(f => Path.GetFileName(f).ToLower()).ToList();
            Console.WriteLine("Source: Unique file names: " + sourceFiles.Count);

            var destinationFiles = targetPaths.ToLookup(f => Path.GetFileName(f).ToLower(), f => f);
            Console.WriteLine("Destination: Unique file names: " + destinationFiles.Count + Environment.NewLine);


            foreach (var fileNameGroup in sourceFiles)
            {
                string filename = fileNameGroup.Key;
                if (destinationFiles.Contains(filename) && AreFilesNewer(fileNameGroup.ToList(), destinationFiles[filename].ToList()))
                {
                    if (AreFilesIdentical(fileNameGroup.ToList(), ref errors))
                    {
                        foreach (string destination in destinationFiles[filename])
                        {
                            copyOperations.Add(new CopyOperation(fileNameGroup.First(), destination));
                        }
                    }
                }
            }

            if (errors.Count > 0)
            {
                foreach (string error in errors)
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
                DateTime oldestSource = SourceFileNames
                    .Select(f => File.GetLastWriteTime(f))
                    .OrderBy(f => f)
                    .First();

                DateTime newestDestination = DestinationFileNames
                    .Select(f => File.GetLastWriteTime(f))
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

            for (int i = 0; i < FileNames.Count; i++)
            {
                Log("Reading: '" + FileNames[i] + "'");

                if (content1 == null)
                {
                    content1 = File.ReadAllBytes(FileNames[i]);
                }
                else
                {
                    byte[] content2 = File.ReadAllBytes(FileNames[i]);

                    if (content1.Length != content2.Length || memcmp(content1, content1, content1.Length) != 0)
                    {
                        errors.Add("Two files in the source folder is different: '" +
                            FileNames[0] + "' <-> '" + FileNames[i] +
                            "': Please fix your broken source folder.");
                    }
                }
            }

            return true;
        }

        static void PerformCopy(List<CopyOperation> copyOperations, string sourcePath, string destinationPath)
        {
            ConsoleColor oldcolor = Console.ForegroundColor;
            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.WriteLine("Copying " + copyOperations.Count + " files: '" + sourcePath + "' -> '" + destinationPath + "'");
            Console.ForegroundColor = oldcolor;


            foreach (CopyOperation op in copyOperations)
            {
                Log("Copying: '" + op.SourcePath + "' -> '" + op.DestinationPath + "'");

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

            string batfile = "checkout.bat";
            using (StreamWriter sw = new StreamWriter(batfile))
            {
                foreach (CopyOperation op in copyOperations)
                {
                    string command = "tf checkout \"" + op.DestinationPath + "\"";
                    sw.WriteLine(command);
                }
            }

            Console.WriteLine("Wrote tfs commands to: " + batfile);
        }
    }
}
