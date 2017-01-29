// This is a script that updates the syncips.txt file, and optional the local hosts file.

// This .cs file or any binary isn't executed, instead a generated .csx file is executed
// by the C# script interpreter csi.exe, therefore two rows must first be replaced, this
// is done by a post build event that uncomments any "//#r" and "//return" rows.

// run with: csi.exe syncips.csx syncips.txt

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Text;
using System.Threading;

public class Program
{
    static string _gitexe = @"C:\Program Files\git\bin\git.exe";

    public static int Main(string[] args)
    {
        int result = 0;

        if (args.Length != 1)
        {
            Log(
@"Poor mans dynamic dns - script for syncing ip between nodes in a farm.

Usage: csi.exe syncips.csx <syncfile>

The syncfile is silmilar to a hosts file, specifies what ip addresses should be kept in sync.

Mandatory environment variables:
  gitserver      git.example.com
  gitrepopath    /myorg/myrepo.git
  gitusername    someuser
  gitpassword    secret
  gitemail       noreply@example.com

Optional environment variables:
  Domain                .myorg.com
  UpdateExternalIPs     false/(true)
  UpdateLocalHostsFile  false/(true). This might require Administrator privileges.");

            result = 1;
        }
        else
        {
            try
            {
                SyncIPs(args[0]);
                UpdateLocalHostsFile(args[0]);
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
        }

        if (Environment.UserInteractive)
        {
            Log("Press any key to continue...");
            Console.ReadKey();
        }

        return result;
    }

    private static void SyncIPs(string syncfile)
    {
        if (!File.Exists(_gitexe))
        {
            throw new ApplicationException($"Git not found: '{_gitexe}'");
        }

        string localhostname = Dns.GetHostName();

        Log($"Local hostname: '{localhostname}'");

        string localipaddress = NetworkInterface
            .GetAllNetworkInterfaces()
            .Where(i => i.NetworkInterfaceType == NetworkInterfaceType.Ethernet && i.OperationalStatus == OperationalStatus.Up)
            .SelectMany(i => i.GetIPProperties().UnicastAddresses)
            .Where(a => a.Address.AddressFamily == AddressFamily.InterNetwork && a.Address.ToString().StartsWith("192.168."))
            .Select(a => a.Address.ToString())
            .FirstOrDefault();

        if (localipaddress == null)
        {
            throw new ApplicationException("Couldn't find any suitable ip address.");
        }

        Log($"Local IP Address: {localipaddress}");

        for (int tries = 0; tries < 5; tries++)
        {
            try
            {
                string filename = Path.Combine($"try_{tries + 1}", syncfile);

                UpdateIPsInGit(localhostname, localipaddress, filename);
                return;
            }
            catch (ApplicationException ex) when (tries < 4)
            {
                Log($"Couldn't push code to git: {ex.Message}");
            }
        }
    }

    private static void UpdateIPsInGit(string localhostname, string localipaddress, string filename)
    {
        string gitserver = Environment.GetEnvironmentVariable("gitserver");
        if (string.IsNullOrEmpty(gitserver))
        {
            throw new ApplicationException("Environment variable 'gitserver' not set.");
        }
        string gitrepopath = Environment.GetEnvironmentVariable("gitrepopath");
        if (string.IsNullOrEmpty(gitrepopath))
        {
            throw new ApplicationException("Environment variable 'gitrepopath' not set.");
        }
        string gitusername = Environment.GetEnvironmentVariable("gitusername");
        if (string.IsNullOrEmpty(gitusername))
        {
            throw new ApplicationException("Environment variable 'gitusername' not set.");
        }
        string gitpassword = Environment.GetEnvironmentVariable("gitpassword");
        if (string.IsNullOrEmpty(gitpassword))
        {
            throw new ApplicationException("Environment variable 'gitpassword' not set.");
        }
        string gitemail = Environment.GetEnvironmentVariable("gitemail");
        if (string.IsNullOrEmpty(gitemail))
        {
            throw new ApplicationException("Environment variable 'gitemail' not set.");
        }

        string sourcefolder = Path.GetFullPath(Path.GetDirectoryName(filename));

        char[] separators = { Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar };
        int index = filename.IndexOfAny(separators);
        string repofolder = index >= 0 ? filename.Substring(0, index) : filename;


        CloneFromGit(gitserver, gitrepopath, repofolder, gitusername, gitpassword);

        int modifiedRows = UpdateIPsFile(filename, localhostname, localipaddress);

        if (modifiedRows > 0)
        {
            string curdir = Directory.GetCurrentDirectory();
            try
            {
                Directory.SetCurrentDirectory(repofolder);
                PushToGit(localhostname, gitusername, gitemail);
            }
            finally
            {
                Directory.SetCurrentDirectory(curdir);
            }
        }
    }

    private static int UpdateIPsFile(string syncfile, string localhostname, string localipaddress)
    {
        if (!File.Exists(syncfile))
        {
            throw new ApplicationException($"Couldn't find file: '{syncfile}'");
        }

        string[] rows = File.ReadAllLines(syncfile);

        int modifiedRows = 0;

        string updateExternalIPs = Environment.GetEnvironmentVariable("UpdateExternalIPs");
        if (!string.IsNullOrEmpty(updateExternalIPs) && updateExternalIPs != "false")
        {
            modifiedRows += UpdateExternalIPs(rows);
        }

        for (int i = 0; i < rows.Length; i++)
        {
            string ipaddress, hostname;
            if (!TryParseRow(rows[i], out ipaddress, out hostname))
            {
                continue;
            }

            if ((hostname.Equals(localhostname, StringComparison.OrdinalIgnoreCase) ||
                hostname.StartsWith($"{localhostname}.", StringComparison.OrdinalIgnoreCase)) &&
                ipaddress != localipaddress)
            {
                string old = rows[i];
                rows[i] = $"{localipaddress,-15} {hostname}";
                Log($"Updating ip address: '{old}' -> '{rows[i]}'");
                modifiedRows++;
            }
        }

        if (modifiedRows > 0)
        {
            Log($"Updating {syncfile} due to {modifiedRows} changed rows.");
            File.WriteAllLines(syncfile, rows);
        }
        else
        {
            Log($"No rows in {syncfile} updated.");
        }

        return modifiedRows;
    }

    private static int UpdateExternalIPs(string[] rows)
    {
        string hostsfile = Path.Combine(Environment.SystemDirectory, @"drivers\etc\hosts");
        string bakfile = $"{hostsfile}.txt";
        int modifiedRows = 0;

        if (!File.Exists(hostsfile))
        {
            Log($"Couldn't find hosts file: '{hostsfile}'");
            return 0;
        }

        try
        {
            File.Move(hostsfile, bakfile);

            for (int i = 0; i < rows.Length; i++)
            {
                string ipaddress, hostname;
                if (!TryParseRow(rows[i], out ipaddress, out hostname))
                {
                    continue;
                }


                Log($"Resolving: '{hostname}'");
                IPHostEntry entry;
                try
                {
                    entry = Dns.GetHostEntry(hostname);
                }
                catch (SocketException ex)
                {
                    Log($"Couldn't resolve hostname: '{hostname}': {ex.Message}");
                    continue;
                }

                string[] ipaddresses = entry
                    .AddressList
                    .Select(a => a.ToString())
                    .ToArray();

                if (ipaddresses.Length > 1)
                {
                    ipaddresses = ipaddresses
                        .Where(a => !a.Contains(':'))
                        .ToArray();
                }

                if (ipaddresses.Length > 1)
                {
                    ipaddresses = ipaddresses
                        .Where(a => a.StartsWith("192.168."))
                        .ToArray();
                }

                if (ipaddresses.Length > 1)
                {
                    ipaddresses = ipaddresses
                        .Where(a => !a.EndsWith(".1"))
                        .ToArray();
                }

                string resolvedipaddress = ipaddresses.FirstOrDefault();

                Log($"Got ip: {resolvedipaddress}");

                if (!string.IsNullOrEmpty(resolvedipaddress) && resolvedipaddress != ipaddress)
                {
                    string old = rows[i];
                    rows[i] = $"{resolvedipaddress,-15} {hostname}";
                    Log($"Updating remote ip address: '{old}' -> '{rows[i]}'");
                    modifiedRows++;
                }
            }
        }
        finally
        {
            if (File.Exists(bakfile) && !File.Exists(hostsfile))
            {
                File.Move(bakfile, hostsfile);
            }
        }

        return modifiedRows;
    }

    private static void CloneFromGit(string server, string repopath, string repofolder, string username, string password)
    {
        Log($"Current directory: '{Directory.GetCurrentDirectory()}'");

        string url = $"http://{username}:{password}@{server}{repopath}";

        Log($"Cloning from git url: '{url}' -> '{repofolder}'");

        RunCommand(_gitexe, $"--no-pager clone {url} {repofolder}");
    }

    private static void PushToGit(string localhostname, string username, string email)
    {
        Log($"Current directory: '{Directory.GetCurrentDirectory()}'");

        Log("Adding/updating/deleting files...");
        RunCommand(_gitexe, "--no-pager add -A");

        Log("Setting config...");
        RunCommand(_gitexe, $"config user.email {email}");
        RunCommand(_gitexe, $"config user.name {username}");

        string commitmessage = $"{localhostname}: Automatic updating of ip addresses: {DateTime.Now.ToString("yyyyMMdd HHmm")}";

        Log("Committing...");
        RunCommand(_gitexe, $"--no-pager commit -m \"{commitmessage}\"");

        Log("Setting config...");
        RunCommand(_gitexe, "config push.default simple");

        Log("Pushing...");
        RunCommand(_gitexe, "--no-pager push");
    }

    private class entry
    {
        public string ipaddress { get; set; }
        public string hostname { get; set; }
        public bool used { get; set; }
    }

    private static void UpdateLocalHostsFile(string syncfile)
    {
        string updateLocalHostsFile = Environment.GetEnvironmentVariable("UpdateLocalHostsFile");
        if (string.IsNullOrEmpty(updateLocalHostsFile) || updateLocalHostsFile == "false")
        {
            Log("Won't update local hosts file.");
            return;
        }

        string domain = Environment.GetEnvironmentVariable("Domain");

        string hostsfile = Path.Combine(Environment.SystemDirectory, @"drivers\etc\hosts");

        List<string> rows = File.ReadAllLines(hostsfile).ToList();

        string[] rowsPatch = File.ReadAllLines(syncfile);

        Dictionary<string, entry> patchvalues = new Dictionary<string, entry>();
        if (!string.IsNullOrEmpty(domain))
        {
            foreach (string row in rowsPatch)
            {
                string ipaddress, hostname;
                if (TryParseRow(row, out ipaddress, out hostname))
                {
                    if (!hostname.EndsWith(domain))
                    {
                        patchvalues[hostname + domain] = new entry { ipaddress = ipaddress, used = false };
                    }
                }
            }
        }
        foreach (string row in rowsPatch)
        {
            string ipaddress, hostname;
            if (TryParseRow(row, out ipaddress, out hostname))
            {
                patchvalues[hostname] = new entry { ipaddress = ipaddress, used = false };
            }
        }


        int modifiedRows = 0;

        List<entry> excessiveEntries = new List<entry>();
        for (int i = 0; i < rows.Count; i++)
        {
            string ipaddress, hostname;
            if (TryParseRow(rows[i], out ipaddress, out hostname))
            {
                excessiveEntries.Add(new entry { hostname = hostname, ipaddress = ipaddress });
            }
        }

        for (int i = 0; i < rows.Count; i++)
        {
            string ipaddress, hostname;
            if (!TryParseRow(rows[i], out ipaddress, out hostname))
            {
                continue;
            }

            Log($"Got ip: {ipaddress}");

            if (patchvalues.ContainsKey(hostname) && patchvalues[hostname].ipaddress != ipaddress)
            {
                string old = rows[i];
                rows[i] = $"{patchvalues[hostname].ipaddress,-15} {hostname}";
                Log($"Updating local hosts file ip address: '{old}' -> '{rows[i]}'");
                modifiedRows++;
            }

            if (patchvalues.ContainsKey(hostname))
            {
                patchvalues[hostname].used = true;
                excessiveEntries.Remove(excessiveEntries.First(e => e.hostname == hostname && e.ipaddress == ipaddress));
            }
        }

        foreach (var entry in patchvalues.Where(e => !e.Value.used))
        {
            string row = $"{entry.Value.ipaddress,-15} {entry.Key}";
            Log($"Adding ip+host to local hosts file: '{row}'");
            rows.Add(row);
            modifiedRows++;
            entry.Value.used = true;
        }

        if (modifiedRows > 0)
        {
            Log($"Updating {hostsfile} due to {modifiedRows} changed rows.");
            File.WriteAllLines(hostsfile, rows);
        }
        else
        {
            Log($"No rows in {hostsfile} updated.");
        }

        Log($"Found {excessiveEntries.Count} excessive entries.");
        foreach (entry entry in excessiveEntries)
        {
            Log($"{entry.ipaddress,-15} {entry.hostname}");
        }
    }

    private static bool TryParseRow(string row, out string ipaddress, out string hostname)
    {
        row = row.Trim();

        if (row == string.Empty || row.StartsWith("#"))
        {
            ipaddress = hostname = null;
            return false;
        }

        char[] separators = { ' ', '\t' };

        string[] tokens = row.Split(separators, StringSplitOptions.RemoveEmptyEntries);
        if (tokens.Length != 2)
        {
            Log($"Ignoring corrupt row: '{row}'");
            ipaddress = hostname = null;
            return false;
        }

        ipaddress = tokens[0];
        hostname = tokens[1];

        return true;
    }

    private static void RunCommand(string exefile, string args)
    {
        Log($"Running: '{exefile}' '{args}'");

        Process process = new Process();
        process.StartInfo = new ProcessStartInfo(exefile, args)
        {
            UseShellExecute = false
        };

        process.Start();
        process.WaitForExit();

        if (process.ExitCode != 0)
        {
            throw new ApplicationException($"Failed to execute: '{exefile}', args: '{args}'");
        }
    }

    private static void LogColor(string message, ConsoleColor color)
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

    private static void Log(string message)
    {
        string hostname = Dns.GetHostName();
        Console.WriteLine($"{hostname}: {message}");
    }
}

//return Program.Main(Environment.GetCommandLineArgs().Skip(2).ToArray());
