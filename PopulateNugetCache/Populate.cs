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
            string targetRootFolder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".nuget", "packages");

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
            string[] nugetFolders = Directory.GetDirectories(sourceRootFolder);

            var packagesFoldersList = new List<string>();
            foreach (var nugetFolder in nugetFolders)
            {
                string[] folders = Directory.GetDirectories(nugetFolder);
                packagesFoldersList.AddRange(folders);
            }
            string[] packagesFolders = packagesFoldersList.ToArray();

            Log($"Found {packagesFolders.Length} package folders.");

            var uniquePackagesFolders = packagesFolders.GroupBy(p => Path.GetFileName(p));

            Log($"Found {uniquePackagesFolders.Count()} unique package folders.");

            return packagesFolders;
        }

        IGrouping<string, string>[] GetVersionsFolders(string[] packagesFolders)
        {
            var versionsFoldersList = new List<string>();
            foreach (var packagesFolder in packagesFolders)
            {
                string[] folders = Directory.GetDirectories(packagesFolder);
                versionsFoldersList.AddRange(folders);
            }
            string[] versionsFolders = versionsFoldersList.ToArray();

            Log($"Found {versionsFolders.Length} version folders.");

            var uniqueVersionsFolders = versionsFolders.GroupBy(v => $"{Path.GetFileName(Path.GetDirectoryName(v))}{Path.DirectorySeparatorChar}{Path.GetFileName(v)}").OrderBy(f => f.Key).ToArray();

            Log($"Found {uniqueVersionsFolders.Length} unique version folders.");

            return uniqueVersionsFolders;
        }

        IGrouping<string, string>[] CalculateOperations(string targetRootFolder, IGrouping<string, string>[] uniqueVersionsFolders)
        {
            var copyFoldersList = new List<IGrouping<string, string>>();
            foreach (var uniqueVersionsFolder in uniqueVersionsFolders)
            {
                string targetFolder = Path.Combine(targetRootFolder, uniqueVersionsFolder.Key);
                if (!Directory.Exists(targetFolder))
                {
                    copyFoldersList.Add(uniqueVersionsFolder);
                }
            }
            var operationFolders = copyFoldersList.OrderBy(c => c.Key).ToArray();
            string opname = Operation == OperationMode.move ? "move" : "copy";

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
                string sourceFolder = operationFolder.OrderBy(p => p).First();
                string targetFolder = Path.Combine(targetRootFolder, operationFolder.Key);

                var parentFolder = Path.GetDirectoryName(targetFolder);
                if (!Directory.Exists(parentFolder))
                {
                    statFolders++;
                }

                CalculateStats(sourceFolder, targetFolder);
            }

            Log($"Folders: {statFolders}");
            string opname = Operation == OperationMode.move ? "move" : "copy";
            Log($"Files {opname}: {statFiles}");
            double kb = ((long)(statFilesSize / 10.24)) / 100.0;
            double mb = ((long)(statFilesSize / 10.24 / 1024)) / 100.0;
            double gb = ((long)(statFilesSize / 10.24 / 1024 / 1024)) / 100.0;
            Log($"Files size: {statFilesSize} ({kb} kb, {mb} mb, {gb} gb)");
        }

        void CalculateStats(string sourceDirName, string destDirName)
        {
            DirectoryInfo dir = new DirectoryInfo(sourceDirName);

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
                string sourceFolder = operationFolder.OrderBy(p => p).First();
                string targetFolder = Path.Combine(targetRootFolder, operationFolder.Key);

                var parentFolder = Path.GetDirectoryName(targetFolder);
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
                Log($"Creating folder: '{targetRootFolder }'", verbose: true);
                if (!Dryrun)
                {
                    Directory.CreateDirectory(targetRootFolder);
                }
            }
        }

        void CopyDirectory(string sourceDirName, string destDirName)
        {
            DirectoryInfo dir = new DirectoryInfo(sourceDirName);

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
                string temppath = Path.Combine(destDirName, file.Name);
                Log($"Copying file: '{file.FullName}' -> '{temppath}'", verbose: true);
                if (!Dryrun)
                {
                    file.CopyTo(temppath);
                }
            }

            foreach (var subdir in dir.GetDirectories())
            {
                string temppath = Path.Combine(destDirName, subdir.Name);
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
