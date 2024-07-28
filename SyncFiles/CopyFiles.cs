using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;

namespace SyncFiles
{
    class CopyFiles
    {
        public static bool CompareMetadata { get; set; }
        public static bool Simulate { get; set; }
        public static string[]? Identifiers { get; set; } = null;
        public static long Maxsize { get; set; }
        public static string[]? Excludes { get; set; } = null;

        static void Log(string message, bool verbose = false, ConsoleColor? color = null)
        {
            if (color.HasValue)
            {
                Console.ForegroundColor = color.Value;
            }
            LogWriter.WriteLine(message, verbose);
            if (color.HasValue)
            {
                Console.ResetColor();
            }
        }

        public static void SyncFiles(string sourcefile, string targetfile, string sourcepath, string targetpath)
        {
            var sourcefiles = GetLines(sourcefile);
            var targetfiles = GetLines(targetfile);

            if (sourcefiles == null || targetfiles == null)
            {
                return;
            }

            Log($"Syncing: {sourcepath} -> {targetpath}", false, ConsoleColor.Magenta);

            Log($"Source files: {sourcefiles.Length}");
            Log($"Target files: {targetfiles.Length}");

            if (Excludes != null && Excludes.Length > 0)
            {
                Log($"Exclude file pattern: '{string.Join("', '", Excludes)}'");
            }
            if (Identifiers != null)
            {
                Log($"Identifiers: {Identifiers.Length}");
            }
            if (Maxsize > 0)
            {
                Log($"Max file size: {Maxsize}");
            }
            if (Simulate)
            {
                Log("Running in simulation mode.");
            }

            Log("Calculating files to copy...");
            string[] filestocopy = ExcludeFiles(sourcefiles, targetfiles);
            Log($"Files to copy: {filestocopy.Length}");

            ShowFastStatistics(targetfiles, filestocopy);

            string[] missingfolders = [.. filestocopy.Select(f => Path.GetDirectoryName(f.Split('\t')[2]) ?? string.Empty).Distinct()];
            Log($"Potentially missing target folders: {missingfolders.Length}");

            //ShowSlowStatistics(targetpath, filestocopy);

            if (Environment.UserInteractive)
            {
                Log($"{Environment.NewLine}Press Enter to start copying...");
                Console.ReadLine();
            }

            CreateFolders(targetpath, missingfolders);

            Copy(sourcepath, targetpath, filestocopy);
        }

        static string[]? GetLines(string filename)
        {
            try
            {
                return [.. File.ReadAllLines(filename).Where(l => l != string.Empty)];
            }
            catch (Exception ex) when (ex is FileNotFoundException or DirectoryNotFoundException or UnauthorizedAccessException or IOException)
            {
                LogWriter.WriteConsoleColor(ex.Message, ConsoleColor.Red);
                return null;
            }
        }

        static string[] ExcludeFiles(string[] filerows, string[] targetfiles)
        {
            var count1 = filerows.Length;
            string[] filestocopy = [.. filerows.Except(targetfiles)];
            var count2 = filestocopy.Length;
            Log($"Excluded files (exists in target): {count1 - count2}");

            count1 = filestocopy.Length;
            filestocopy = [.. filestocopy.Where(f => !Regex.IsMatch(Path.GetFileName(f.Split('\t')[2]), ".*DS_Store", RegexOptions.IgnoreCase))];
            count2 = filestocopy.Length;
            Log($"Excluded files (DS_Store): {count1 - count2}");

            count1 = filestocopy.Length;
            filestocopy = [.. filestocopy.Where(f => !Regex.IsMatch(Path.GetFileName(f.Split('\t')[2]), "thumbs.db", RegexOptions.IgnoreCase))];
            count2 = filestocopy.Length;
            Log($"Excluded files (thumbs.db): {count1 - count2}");

            if (Excludes != null && Excludes.Length > 0)
            {
                foreach (var exclude in Excludes)
                {
                    if (LogWriter.Verbose)
                    {
                        string[] junk = [.. filestocopy.Where(f => Regex.IsMatch(f.Split('\t')[2], exclude, RegexOptions.IgnoreCase))];
                        Log($"Excluded files (file pattern: '{exclude}'): {junk.Length}", true);
                        foreach (var filerow in junk)
                        {
                            Log($"Exclude file: '{filerow}'", true);
                        }
                    }

                    count1 = filestocopy.Length;
                    filestocopy = [.. filestocopy.Where(f => !Regex.IsMatch(f.Split('\t')[2], exclude, RegexOptions.IgnoreCase))];
                    count2 = filestocopy.Length;
                    Log($"Excluded files (file pattern: '{exclude}'): {count1 - count2}");
                }
            }

            if (Identifiers != null)
            {
                if (LogWriter.Verbose)
                {
                    string[] junk = [.. filestocopy.Where(f => f.Split('\t')[2].StartsWith("ProductContent") && !Identifiers.Any(i => f.Split('\t')[2].StartsWith($"ProductContent\\{i}")))];
                    Log($"Excluded files (identifiers): {junk.Length}", true);
                    foreach (var filerow in junk)
                    {
                        Log($"Exclude file: '{filerow}'", true);
                    }
                }

                count1 = filestocopy.Length;
                var t1 = DateTime.Now;
                filestocopy = [.. filestocopy.Where(f => !f.Split('\t')[2].StartsWith("ProductContent") || Identifiers.Any(i => f.Split('\t')[2].StartsWith($"ProductContent\\{i}")))];
                var t2 = DateTime.Now;
                count2 = filestocopy.Length;
                Log($"Excluded files (identifiers): {count1 - count2} (Calc time: {t2 - t1})");
            }

            if (Maxsize > 0)
            {
                if (LogWriter.Verbose)
                {
                    string[] junk = [.. filestocopy.Where(f => long.Parse(f.Split('\t')[1]) > Maxsize)];
                    Log($"Excluded files (max file size): {junk.Length}", true);
                    foreach (var filerow in junk)
                    {
                        Log($"Exclude file: '{filerow}'", true);
                    }
                }

                count1 = filestocopy.Length;
                filestocopy = [.. filestocopy.Where(f => long.Parse(f.Split('\t')[1]) <= Maxsize)];
                count2 = filestocopy.Length;
                Log($"Excluded files (max file size): {count1 - count2}");
            }

            return filestocopy;
        }

        static void ShowFastStatistics(string[] targetfiles, string[] filestocopy)
        {
            var copysize = filestocopy.Sum(s => long.Parse(s.Split('\t')[1]));
            Log($"Files to copy size: {copysize / 1024 / 1024} mb");

            var overwritecount = 0;
            var newcopycount = 0;
            long additionalsize = 0;

            Dictionary<string, long> dic = [];

            foreach (var filerow in targetfiles)
            {
                var tokens = filerow.Split('\t');
                dic.Add(tokens[2], long.Parse(tokens[1]));
            }

            foreach (var filerow in filestocopy)
            {
                var tokens = filerow.Split('\t');
                var path = tokens[2];
                var size = long.Parse(tokens[1]);
                if (dic.TryGetValue(path, out long value))
                {
                    overwritecount++;
                    additionalsize += size - value;
                }
                else
                {
                    newcopycount++;
                    additionalsize += size;
                }
            }

            Log($"Free space required: {additionalsize / 1024 / 1024} mb");

            Log($"Missing files in target, will be copied: {newcopycount}");
            Log($"Different, will be overwritten: {overwritecount}");

            return;
        }

        static void ShowSlowStatistics(string targetpath, string[] missingfiles)
        {
            var existcopycount = 0;
            var copycount = 0;

            foreach (var row in missingfiles)
            {
                var filename = row.Split('\t')[2];
                var targetpath2 = Path.Combine(targetpath, filename);

                if (File.Exists(targetpath2))
                {
                    // file already exist, probably different
                    existcopycount++;
                }
                else
                {
                    // file didn't exist in target
                    copycount++;
                }
            }

            Log($"Missing files in target, will be copied: {copycount}");
            Log($"Different, will be overwritten: {existcopycount}");
        }

        static void CreateFolders(string targetpath, string[] targetfolders)
        {
            foreach (var targetfolder in targetfolders)
            {
                var targetfolderfullpath = Path.Combine(targetpath, targetfolder);

                if (!Directory.Exists(targetfolderfullpath))
                {
                    Log($"Creating folder: '{targetfolderfullpath}'");
                    if (!Simulate)
                    {
                        Directory.CreateDirectory(targetfolderfullpath);
                    }
                }
            }
        }

        static void Copy(string sourcepath, string targetpath, string[] filestocopy)
        {
            var t1 = DateTime.Now;
            var copiedfiles = 0;
            long copiedsize = 0;
            var errors = 0;
            var equalfilecount = 0;

            foreach (var row in filestocopy)
            {
                var filesize = long.Parse(row.Split('\t')[1]);
                var filename = row.Split('\t')[2];
                var sourcepath2 = Path.Combine(sourcepath, filename);
                var targetpath2 = Path.Combine(targetpath, filename);

                FileInfo? fiSource = null;
                FileInfo? fiTarget = null;

                try
                {
                    if (CompareMetadata && File.Exists(targetpath2))
                    {
                        fiSource = new(sourcepath2);
                        fiTarget = new(targetpath2);
                    }
                }
                catch (Exception ex) when (ex is FileNotFoundException or DirectoryNotFoundException or UnauthorizedAccessException or IOException)
                {
                    Log($"Copying: '{sourcepath2}' -> '{targetpath2}'");
                    Log(ex.Message);
                    errors++;
                    continue;
                }

                try
                {
                    if (fiSource != null && fiTarget != null)
                    {
                        if (fiSource.LastWriteTime == fiTarget.LastWriteTime && fiSource.Length == fiTarget.Length)
                        {
                            Log($"Copying: '{sourcepath2}' -> '{targetpath2}': Files appears equal after all.", true, ConsoleColor.Yellow);
                            equalfilecount++;
                            continue;
                        }

                        RemoveRO(targetpath2);
                    }
                    else
                    {
                        if (File.Exists(targetpath2))
                        {
                            RemoveRO(targetpath2);
                        }
                    }

                    Log($"Copying: '{sourcepath2}' -> '{targetpath2}'");
                    if (!Simulate)
                    {
                        File.Copy(sourcepath2, targetpath2, true);
                    }
                    copiedfiles++;
                    copiedsize += filesize;
                }
                catch (Exception ex) when (ex is FileNotFoundException or DirectoryNotFoundException or UnauthorizedAccessException or IOException)
                {
                    Log(ex.Message);
                    errors++;
                }
            }

            var ts = DateTime.Now - t1;

            var copysize = filestocopy.Sum(s => long.Parse(s.Split('\t')[1]));

            Log($"Copied files: {copiedfiles} (of {filestocopy.Length})");
            Log($"Copied size: {copiedsize / 1024 / 1024} mb (of {copysize / 1024 / 1024} mb)");
            Log($"Unexpected identical files: {equalfilecount}");
            Log($"Time: {ts}");
            Log($"Speed: {(ts == TimeSpan.Zero ? 0 : (copysize / 1024 / 1024 / ts.TotalSeconds))} mb/s");
            Log($"Errors: {errors}");
        }

        static void RemoveRO(string filename)
        {
            var fa = File.GetAttributes(filename);
            if ((fa & FileAttributes.ReadOnly) == FileAttributes.ReadOnly)
            {
                if (!Simulate)
                {
                    File.SetAttributes(filename, fa & ~FileAttributes.ReadOnly);
                }
            }
        }
    }
}
