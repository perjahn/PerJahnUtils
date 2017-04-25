using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Security;
using System.Security.Cryptography;
using System.Text;
using System.Threading;

public class Program
{
    public static int Main(string[] args)
    {
        int result = 0;
        try
        {
            if (args.Length != 2)
            {
                Console.WriteLine(
@"BackupTCProjects 1.1

This is a backup program that retrieves all important configuration files on
a Teamcity build server. These files can be backuped and later imported on
any other build server. Junk files are excluded.

Reason for not using Teamcity's own backup feature is that it will make too
many commits, one for each change. This tool can instead be scheduled once
a day.

Usage: BackupTCProjects <source> <target>

Example: D:\TeamCity\.BuildServer\config\projects _Artifacts\projects

Optional environment variables (default):
includebuildnumberfiles  - true/(false)

Optional environment variables, used for pushing code (with examples):
gitserver                - gitserver.organization.com
gitrepopath              - /organization/tcconfig.git
gitrepofolder            - tcconfig\projects
gitusername              - luser
gitpassword              - abc123
gitemail                 - noreply@example.com
gitsimulatepush          - true/(false)");

                return 1;
            }

            BackupTCProjects(args[0], args[1]);
        }
        catch (ApplicationException ex)
        {
            LogColor(ex.Message, ConsoleColor.Red);
            result = 1;
        }
        catch (Exception ex)
        {
            LogColor(ex.ToString(), ConsoleColor.Red);
            result = 1;
        }

        if (Environment.UserInteractive)
        {
            Log("Press any key to continue...");
            Console.ReadKey();
        }

        return result;
    }

    static void BackupTCProjects(string sourcefolder, string targetfolder)
    {
        string curdir = Environment.CurrentDirectory;

        Log($"Current Directory: '{curdir}'");

        if (!Directory.Exists(sourcefolder))
        {
            string message = $"Couldn't find source folder: '{sourcefolder}'";
            throw new ApplicationException(message);
        }

        if (Directory.Exists(targetfolder))
        {
            Log($"Deleting target folder: '{targetfolder}'");
            RobustDelete(targetfolder);
        }

        Log($"Creating target folder: '{targetfolder}'");
        Directory.CreateDirectory(targetfolder);


        string shortSourcefolder = sourcefolder;
        if (sourcefolder.StartsWith(curdir))
        {
            shortSourcefolder = sourcefolder.Substring(curdir.Length);
        }


        bool includebuildnumberfiles = ParseBooleanEnvironmentVariable("includebuildnumberfiles", false);

        string shortTargetfolder = targetfolder;
        if (targetfolder.StartsWith(curdir))
        {
            shortTargetfolder = targetfolder.Substring(curdir.Length);
        }

        CopyFiles(shortSourcefolder, shortTargetfolder, includebuildnumberfiles);

        string gitserver = Environment.GetEnvironmentVariable("gitserver");
        string gitrepopath = Environment.GetEnvironmentVariable("gitrepopath");
        string gitrepofolder = Environment.GetEnvironmentVariable("gitrepofolder");
        string gitusername = Environment.GetEnvironmentVariable("gitusername");
        string gitpassword = Environment.GetEnvironmentVariable("gitpassword");
        string gitemail = Environment.GetEnvironmentVariable("gitemail");

        bool gitsimulatepush = ParseBooleanEnvironmentVariable("gitsimulatepush", false);

        if (string.IsNullOrEmpty(gitserver) || string.IsNullOrEmpty(gitrepopath) || string.IsNullOrEmpty(gitrepofolder) ||
            string.IsNullOrEmpty(gitusername) || string.IsNullOrEmpty(gitpassword) || string.IsNullOrEmpty(gitemail))
        {
            StringBuilder missing = new StringBuilder();
            if (string.IsNullOrEmpty(gitserver))
                missing.AppendLine("Missing gitserver.");
            if (string.IsNullOrEmpty(gitrepopath))
                missing.AppendLine("Missing gitrepopath.");
            if (string.IsNullOrEmpty(gitrepofolder))
                missing.AppendLine("Missing gitrepofolder.");
            if (string.IsNullOrEmpty(gitusername))
                missing.AppendLine("Missing gitusername.");
            if (string.IsNullOrEmpty(gitpassword))
                missing.AppendLine("Missing gitpassword.");
            if (string.IsNullOrEmpty(gitemail))
                missing.AppendLine("Missing gitemail.");

            Log("Missing git environment variables, will not push Teamcity config files to Git." + Environment.NewLine + missing.ToString());
        }
        else
        {
            PushToGit(shortTargetfolder, gitserver, gitrepopath, gitrepofolder, gitusername, gitpassword, gitemail, gitsimulatepush);
        }
    }

    static bool ParseBooleanEnvironmentVariable(string variableName, bool defaultValue)
    {
        string stringValue = Environment.GetEnvironmentVariable(variableName);
        if (stringValue == null)
        {
            return defaultValue;
        }
        else
        {
            bool boolValue;
            if (!bool.TryParse(stringValue, out boolValue))
            {
                return defaultValue;
            }
            return boolValue;
        }
    }

    static void CopyFiles(string sourcefolder, string targetfolder, bool includebuildnumberfiles)
    {
        string[] files = Directory.GetFiles(sourcefolder, "*", SearchOption.AllDirectories)
            .Select(f => f.StartsWith(@".\") ? f.Substring(2) : f)
            .ToArray();

        Log($"Found {files.Length} files.");


        string[] ignorefiles = files
            .Where(f => Path.GetExtension(f).Length == 2 && char.IsDigit(Path.GetExtension(f)[1]))
            .ToArray();
        files = files
            .Where(f => !ignorefiles.Contains(f))
            .ToArray();
        LogTCSection($"Ignoring {ignorefiles.Length} backup files.", ignorefiles);


        ignorefiles = files
            .Where(f => f.EndsWith(".buildNumbers.properties"))
            .ToArray();
        if (includebuildnumberfiles)
        {
            Log($"Including {ignorefiles.Length} build number files.");
        }
        else
        {
            files = files
                .Where(f => !ignorefiles.Contains(f))
                .ToArray();
            LogTCSection($"Ignoring {ignorefiles.Length} build number files.", ignorefiles);
        }

        ignorefiles = files
            .Where(f => Path.GetFileName(f) == "plugin-settings.xml" && new FileInfo(f).Length == 56)
            .ToArray();
        files = files
            .Where(f => !ignorefiles.Contains(f))
            .ToArray();
        LogTCSection($"Ignoring {ignorefiles.Length} default plugin settings files.", ignorefiles);


        Log($"Backuping {files.Length} files to: '{targetfolder}'");

        int count = 0;

        foreach (string sourcefile in files)
        {
            string targetfile = Path.Combine(targetfolder, sourcefile.Substring(sourcefolder.Length + 1));

            CopyFile(sourcefile, targetfile);
            count++;
        }

        Log($"Copied {files.Length} files.");
    }

    static void CopyFile(string sourcefile, string targetfile)
    {
        string folder = Path.GetDirectoryName(targetfile);

        if (!Directory.Exists(folder))
        {
            Log($"Creating target folder: '{folder}'");
            Directory.CreateDirectory(folder);
        }

        Log($"Copying: '{sourcefile}' -> '{targetfile}'");

        // Also normalize lf to crlf, else later commit/push might fail.
        string[] rows = File.ReadAllLines(sourcefile);
        File.WriteAllLines(targetfile, rows);
    }

    static void PushToGit(string sourcefolder, string server, string repopath, string repofolder, string username, string password, string email, bool gitsimulatepush)
    {
        string gitexe = @"C:\Program Files\git\bin\git.exe";
        if (!File.Exists(gitexe))
        {
            throw new ApplicationException($"Git not found: '{gitexe}'");
        }

        string rootfolder, subfolder;
        int offset = repofolder.IndexOfAny(new[] { Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar });
        if (offset >= 0)
        {
            rootfolder = repofolder.Substring(0, offset);
            subfolder = repofolder.Substring(offset + 1);
        }
        else
        {
            rootfolder = repofolder;
            subfolder = ".";
        }

        RobustDelete(rootfolder);


        string url = $"http://{username}:{password}@{server}{repopath}";

        Log($"Using git url: '{url}'");

        RunCommand(gitexe, $"--no-pager clone {url}");
        Directory.SetCurrentDirectory(rootfolder);
        Log($"Current directory: '{Directory.GetCurrentDirectory()}'");


        string relativesourcefolder = Path.Combine("..", sourcefolder);
        string targetfolder = subfolder;

        if (LogTCSection("Comparing folders", () => CompareFolders(relativesourcefolder, targetfolder)))
        {
            Log($"No changes found: '{relativesourcefolder}' '{targetfolder}'");
            return;
        }


        if (subfolder != ".")
        {
            if (Directory.Exists(subfolder))
            {
                Log($"Deleting folder: '{subfolder}'");
                Directory.Delete(subfolder, true);
            }
        }

        Log($"Copying files into git folder: '{relativesourcefolder}' -> '{targetfolder}'");
        Microsoft.VisualBasic.FileIO.FileSystem.CopyDirectory(relativesourcefolder, targetfolder, true);


        //string gitchanges = RunCommand(gitexe, "status --porcelain", true);
        //if (gitchanges != null)

        Log("Adding/updating/deleting files...");
        RunCommand(gitexe, "--no-pager add -A");

        Log("Setting config...");
        RunCommand(gitexe, $"config user.email {email}");
        RunCommand(gitexe, $"config user.name {username}");

        string commitmessage = "Automatic gathering of Teamcity config files: " + DateTime.Now.ToString("yyyyMMdd HHmm");

        Log("Committing...");
        RunCommand(gitexe, $"--no-pager commit -m \"{commitmessage}\"");

        Log("Setting config...");
        RunCommand(gitexe, "config push.default simple");

        Log("Pushing...");
        if (gitsimulatepush)
        {
            Log("...not!");
        }
        else
        {
            RunCommand(gitexe, "--no-pager push");
        }
    }

    private static void RobustDelete(string folder)
    {
        if (Directory.Exists(folder))
        {
            string[] files = Directory.GetFiles(folder, "*", SearchOption.AllDirectories);
            foreach (string filename in files)
            {
                try
                {
                    File.SetAttributes(filename, File.GetAttributes(filename) & ~FileAttributes.ReadOnly);
                }
                catch (Exception ex) when (ex is UnauthorizedAccessException || ex is IOException)
                {
                    // Will be dealt with deleting whole folder.
                }
            }

            for (int tries = 1; tries <= 10; tries++)
            {
                Log($"Try {tries} to delete folder: '{folder}'");
                try
                {
                    Directory.Delete(folder, true);
                    return;
                }
                catch (Exception ex) when (tries < 10 && (ex is UnauthorizedAccessException || ex is IOException))
                {
                    Thread.Sleep(1000);
                }
            }
        }
    }

    static bool CompareFolders(string folder1, string folder2)
    {
        Log($"Retrieving files: '{folder1}'");
        string[] files1 = Directory.GetFiles(folder1, "*", SearchOption.AllDirectories);
        Log($"Retrieving files: '{folder2}'");
        string[] files2 = Directory.GetFiles(folder2, "*", SearchOption.AllDirectories);

        if (files1.Length != files2.Length)
        {
            Log($"File count diff: {files1.Length} {files2.Length}");
            return false;
        }

        Array.Sort(files1);
        Array.Sort(files2);

        for (int i = 0; i < files1.Length; i++)
        {
            string file1 = files1[i];
            string file2 = files2[i];

            Log($"Comparing: '{file1}' '{file2}'");
            string f1 = file1.Substring(folder1.Length);
            string f2 = file2.Substring(folder2.Length);

            if (f1 != f2)
            {
                Log($"Filename diff: '{f1}' '{f2}'");
                return false;
            }

            string hash1 = GetFileHash(file1);
            string hash2 = GetFileHash(file2);
            if (hash1 != hash2)
            {
                Log($"Hash diff: '{file1}' '{file2}' {hash1} {hash2}");
                return false;
            }
        }

        return true;
    }

    static string GetFileHash(string filename)
    {
        using (FileStream fs = new FileStream(filename, FileMode.Open))
        {
            using (BufferedStream bs = new BufferedStream(fs))
            {
                using (SHA1Managed sha1 = new SHA1Managed())
                {
                    byte[] hash = sha1.ComputeHash(bs);
                    StringBuilder formatted = new StringBuilder(2 * hash.Length);
                    foreach (byte b in hash)
                    {
                        formatted.AppendFormat("{0:X2}", b);
                    }
                    return formatted.ToString();
                }
            }
        }
    }

    static string RunCommand(string exefile, string args, bool redirect = false)
    {
        Log($"Running: '{exefile}' '{args}'");

        Process process = new Process();
        if (redirect)
        {
            process.StartInfo = new ProcessStartInfo(exefile, args)
            {
                UseShellExecute = false,
                RedirectStandardOutput = true
            };
        }
        else
        {
            process.StartInfo = new ProcessStartInfo(exefile, args)
            {
                UseShellExecute = false
            };
        }

        process.Start();
        process.WaitForExit();

        if (process.ExitCode != 0)
        {
            throw new ApplicationException($"Failed to execute: '{exefile}', args: '{args}'");
        }

        return redirect ? process.StandardOutput.ReadToEnd() : null;
    }

    static void LogColor(string message, ConsoleColor color)
    {
        ConsoleColor oldColor = Console.ForegroundColor;
        try
        {
            Console.ForegroundColor = color;
            Log(message);
        }
        finally
        {
            Console.ForegroundColor = oldColor;
        }
    }

    static void Log(string message)
    {
        string hostname = Dns.GetHostName();
        Console.WriteLine($"{hostname}: {DateTime.Now}: {message}");
    }

    private static T LogTCSection<T>(string message, Func<T> func)
    {
        ConsoleColor oldColor = Console.ForegroundColor;
        try
        {
            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.WriteLine($"##teamcity[blockOpened name='{message}']");
        }
        finally
        {
            Console.ForegroundColor = oldColor;
        }

        T result = func.Invoke();

        oldColor = Console.ForegroundColor;
        try
        {
            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.WriteLine($"##teamcity[blockClosed name='{message}']");
        }
        finally
        {
            Console.ForegroundColor = oldColor;
        }

        return result;
    }

    static void LogTCSection(string message, IEnumerable<string> collection)
    {
        string hostname = Dns.GetHostName();

        ConsoleColor oldColor = Console.ForegroundColor;
        try
        {
            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.WriteLine($"##teamcity[blockOpened name='{message}']");
        }
        finally
        {
            Console.ForegroundColor = oldColor;
        }

        Console.WriteLine($"{hostname}: {DateTime.Now}: " + string.Join(Environment.NewLine + $"{hostname}: {DateTime.Now}: ", collection));

        try
        {
            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.WriteLine($"##teamcity[blockClosed name='{message}']");
        }
        finally
        {
            Console.ForegroundColor = oldColor;
        }
    }
}
