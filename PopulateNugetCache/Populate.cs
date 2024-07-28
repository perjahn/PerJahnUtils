using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace PopulateNugetCache
{
    class Populate
    {
        public enum OperationMode { copy, move }

        public bool Dryrun { get; set; } = false;
        public OperationMode Operation { get; set; } = OperationMode.copy;
        public bool Verbose { get; set; } = false;

        long statFolders = 0;
        long statFiles = 0;
        long statFilesSize = 0;

        public void PopulateNugetCache(string sourceRootFolder)
        {
            var targetRootFolder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".nuget", "packages");

            var packagesFolders = GetPackagesFolders(sourceRootFolder);

            var uniqueVersionsFolders = GetVersionsFolders(packagesFolders);

            var operationFolders = CalculateOperations(targetRootFolder, uniqueVersionsFolders);

            foreach (var operationFolder in operationFolders)
            {
                Log($"'{operationFolder.Key}'", verbose: true);
            }

            Stat(targetRootFolder, operationFolders);

            Action(targetRootFolder, operationFolders);
        }

        string[] GetPackagesFolders(string sourceRootFolder)
        {
            var nugetFolders = Directory.GetDirectories(sourceRootFolder);

            List<string> packagesFoldersList = [];
            foreach (var nugetFolder in nugetFolders)
            {
                var folders = Directory.GetDirectories(nugetFolder);
                packagesFoldersList.AddRange(folders);
            }
            string[] packagesFolders = [.. packagesFoldersList];

            Log($"Found {packagesFolders.Length} package folders.");

            var uniquePackagesFolders = packagesFolders.GroupBy(p => Path.GetFileName(p));

            Log($"Found {uniquePackagesFolders.Count()} unique package folders.");

            return packagesFolders;
        }

        IGrouping<string, string>[] GetVersionsFolders(string[] packagesFolders)
        {
            List<string> versionsFoldersList = [];
            foreach (var packagesFolder in packagesFolders)
            {
                var folders = Directory.GetDirectories(packagesFolder);
                versionsFoldersList.AddRange(folders);
            }
            string[] versionsFolders = [.. versionsFoldersList];

            Log($"Found {versionsFolders.Length} version folders.");

            IGrouping<string, string>[] uniqueVersionsFolders = [.. versionsFolders.GroupBy(v => $"{Path.GetFileName(Path.GetDirectoryName(v))}{Path.DirectorySeparatorChar}{Path.GetFileName(v)}").OrderBy(f => f.Key)];

            Log($"Found {uniqueVersionsFolders.Length} unique version folders.");

            return uniqueVersionsFolders;
        }

        IGrouping<string, string>[] CalculateOperations(string targetRootFolder, IGrouping<string, string>[] uniqueVersionsFolders)
        {
            List<IGrouping<string, string>> copyFoldersList = [];
            foreach (var uniqueVersionsFolder in uniqueVersionsFolders)
            {
                var targetFolder = Path.Combine(targetRootFolder, uniqueVersionsFolder.Key);
                if (!Directory.Exists(targetFolder))
                {
                    copyFoldersList.Add(uniqueVersionsFolder);
                }
            }
            IGrouping<string, string>[] operationFolders = [.. copyFoldersList.OrderBy(c => c.Key)];
            var opname = Operation == OperationMode.move ? "move" : "copy";

            Log($"Got {operationFolders.Length} {opname} operations ({uniqueVersionsFolders.Length - operationFolders.Length} version folders already exists).");

            return operationFolders;
        }

        void Stat(string targetRootFolder, IGrouping<string, string>[] operationFolders)
        {
            if (!Directory.Exists(targetRootFolder))
            {
                statFolders++;
            }

            foreach (var operationFolder in operationFolders)
            {
                var sourceFolder = operationFolder.OrderBy(p => p).First();
                var targetFolder = Path.Combine(targetRootFolder, operationFolder.Key);

                var parentFolder = Path.GetDirectoryName(targetFolder);
                if (!Directory.Exists(parentFolder))
                {
                    statFolders++;
                }

                CalculateStats(sourceFolder, targetFolder);
            }

            Log($"Folders: {statFolders}");
            var opname = Operation == OperationMode.move ? "move" : "copy";
            Log($"Files {opname}: {statFiles}");
            double kb = ((long)(statFilesSize / 10.24)) / 100.0;
            double mb = ((long)(statFilesSize / 10.24 / 1024)) / 100.0;
            double gb = ((long)(statFilesSize / 10.24 / 1024 / 1024)) / 100.0;
            Log($"Files size: {statFilesSize} ({kb} kb, {mb} mb, {gb} gb)");
        }

        void CalculateStats(string sourceDirName, string destDirName)
        {
            DirectoryInfo dir = new(sourceDirName);

            if (!Directory.Exists(destDirName))
            {
                statFolders++;
            }

            foreach (var file in dir.GetFiles())
            {
                statFiles++;
                statFilesSize += file.Length;
            }

            foreach (var subdir in dir.GetDirectories())
            {
                CalculateStats(subdir.FullName, Path.Combine(destDirName, subdir.Name));
            }
        }

        void Action(string targetRootFolder, IGrouping<string, string>[] operationFolders)
        {
            CreateTargetRootFolder(targetRootFolder);

            foreach (var operationFolder in operationFolders)
            {
                var sourceFolder = operationFolder.OrderBy(p => p).First();
                var targetFolder = Path.Combine(targetRootFolder, operationFolder.Key);

                var parentFolder = Path.GetDirectoryName(targetFolder) ?? string.Empty;
                if (!Directory.Exists(parentFolder))
                {
                    Log($"Creating folder: '{parentFolder}'", verbose: true);
                    if (!Dryrun)
                    {
                        Directory.CreateDirectory(parentFolder);
                    }
                }

                if (Operation == OperationMode.move)
                {
                    Log($"Moving: '{sourceFolder}' -> '{targetFolder}'", verbose: true);
                    if (!Dryrun)
                    {
                        Directory.Move(sourceFolder, targetFolder);
                    }
                }
                else
                {
                    Log($"Copying: '{sourceFolder}' -> '{targetFolder}'", verbose: true);
                    CopyDirectory(sourceFolder, targetFolder);
                }
            }
        }

        void CreateTargetRootFolder(string targetRootFolder)
        {
            if (!Directory.Exists(targetRootFolder))
            {
                Log($"Creating folder: '{targetRootFolder}'", verbose: true);
                if (!Dryrun)
                {
                    Directory.CreateDirectory(targetRootFolder);
                }
            }
        }

        void CopyDirectory(string sourceDirName, string destDirName)
        {
            DirectoryInfo dir = new(sourceDirName);

            if (!Directory.Exists(destDirName))
            {
                Log($"Creating folder: '{destDirName}'", verbose: true);
                if (!Dryrun)
                {
                    Directory.CreateDirectory(destDirName);
                }
            }

            foreach (var file in dir.GetFiles())
            {
                var temppath = Path.Combine(destDirName, file.Name);
                Log($"Copying file: '{file.FullName}' -> '{temppath}'", verbose: true);
                if (!Dryrun)
                {
                    file.CopyTo(temppath);
                }
            }

            foreach (var subdir in dir.GetDirectories())
            {
                var temppath = Path.Combine(destDirName, subdir.Name);
                CopyDirectory(subdir.FullName, temppath);
            }
        }

        void Log(string message, bool verbose = false)
        {
            if (verbose && !Verbose)
            {
                return;
            }
            Console.WriteLine(message);
        }
    }
}
