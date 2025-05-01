// This is an app that updates the syncips.txt file, and optional the local hosts file.
// run with: dotnet run -- syncips.txt

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;

namespace syncips
{
    class Program
    {
        const string _gitexe = @"C:\Program Files\git\bin\git.exe";

        public static int Main(string[] args)
        {
            if (args == null)
            {
                return 1;
            }

            var result = 0;

            if (args.Length != 1)
            {
                Log(
    @"Poor man's dynamic dns - script for syncing ip between nodes in a farm.

Usage: dotnet run -- <syncfile>

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
                _ = Console.ReadKey();
            }

            return result;
        }

        private static void SyncIPs(string syncfile)
        {
            if (!File.Exists(_gitexe))
            {
                throw new ApplicationException($"Git not found: '{_gitexe}'");
            }

            var localhostname = Dns.GetHostName();

            Log($"Local hostname: '{localhostname}'");

            var localipaddress = NetworkInterface
                .GetAllNetworkInterfaces()
                .Where(i => i.NetworkInterfaceType == NetworkInterfaceType.Ethernet && i.OperationalStatus == OperationalStatus.Up)
                .SelectMany(i => i.GetIPProperties().UnicastAddresses)
                .Where(a => a.Address.AddressFamily == AddressFamily.InterNetwork && a.Address.ToString().StartsWith("192.168."))
                .Select(a => a.Address.ToString())
                .FirstOrDefault() ?? throw new ApplicationException("Couldn't find any suitable ip address.");

            Log($"Local IP Address: {localipaddress}");

            for (var tries = 0; tries < 5; tries++)
            {
                try
                {
                    var filename = Path.Combine($"try_{tries + 1}", syncfile);

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
            var gitserver = Environment.GetEnvironmentVariable("gitserver");
            if (string.IsNullOrEmpty(gitserver))
            {
                throw new ApplicationException("Environment variable 'gitserver' not set.");
            }
            var gitrepopath = Environment.GetEnvironmentVariable("gitrepopath");
            if (string.IsNullOrEmpty(gitrepopath))
            {
                throw new ApplicationException("Environment variable 'gitrepopath' not set.");
            }
            var gitusername = Environment.GetEnvironmentVariable("gitusername");
            if (string.IsNullOrEmpty(gitusername))
            {
                throw new ApplicationException("Environment variable 'gitusername' not set.");
            }
            var gitpassword = Environment.GetEnvironmentVariable("gitpassword");
            if (string.IsNullOrEmpty(gitpassword))
            {
                throw new ApplicationException("Environment variable 'gitpassword' not set.");
            }
            var gitemail = Environment.GetEnvironmentVariable("gitemail");
            if (string.IsNullOrEmpty(gitemail))
            {
                throw new ApplicationException("Environment variable 'gitemail' not set.");
            }

            char[] separators = [Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar];
            var index = filename.IndexOfAny(separators);
            var repofolder = index >= 0 ? filename[..index] : filename;

            CloneFromGit(gitserver, gitrepopath, repofolder, gitusername, gitpassword);

            var modifiedRows = UpdateIPsFile(filename, localhostname, localipaddress);

            if (modifiedRows > 0)
            {
                var curdir = Directory.GetCurrentDirectory();
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

            var rows = File.ReadAllLines(syncfile);

            var modifiedRows = 0;

            var updateExternalIPs = Environment.GetEnvironmentVariable("UpdateExternalIPs");
            if (!string.IsNullOrEmpty(updateExternalIPs) && updateExternalIPs != "false")
            {
                modifiedRows += UpdateExternalIPs(rows);
            }

            for (var i = 0; i < rows.Length; i++)
            {
                if (!TryParseRow(rows[i], out string ipaddress, out string hostname))
                {
                    continue;
                }

                if ((hostname.Equals(localhostname, StringComparison.OrdinalIgnoreCase) ||
                    hostname.StartsWith($"{localhostname}.", StringComparison.OrdinalIgnoreCase)) &&
                    ipaddress != localipaddress)
                {
                    var old = rows[i];
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
            var hostsfile = Path.Combine(Environment.SystemDirectory, @"drivers\etc\hosts");
            var bakfile = $"{hostsfile}.txt";
            var modifiedRows = 0;

            if (!File.Exists(hostsfile))
            {
                Log($"Couldn't find hosts file: '{hostsfile}'");
                return 0;
            }

            try
            {
                File.Move(hostsfile, bakfile);

                for (var i = 0; i < rows.Length; i++)
                {
                    if (!TryParseRow(rows[i], out string ipaddress, out string hostname))
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

                    string[] ipaddresses = [.. entry.AddressList.Select(a => a.ToString())];

                    if (ipaddresses.Length > 1)
                    {
                        ipaddresses = [.. ipaddresses.Where(a => !a.Contains(':'))];
                    }

                    if (ipaddresses.Length > 1)
                    {
                        ipaddresses = [.. ipaddresses.Where(a => a.StartsWith("192.168."))];
                    }

                    if (ipaddresses.Length > 1)
                    {
                        ipaddresses = [.. ipaddresses.Where(a => !a.EndsWith(".1"))];
                    }

                    var resolvedipaddress = ipaddresses.FirstOrDefault();

                    Log($"Got ip: {resolvedipaddress}");

                    if (!string.IsNullOrEmpty(resolvedipaddress) && resolvedipaddress != ipaddress)
                    {
                        var old = rows[i];
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

            var url = $"http://{username}:{password}@{server}{repopath}";

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

            var commitmessage = $"{localhostname}: Automatic updating of ip addresses: {DateTime.Now:yyyyMMdd HHmm}";

            Log("Committing...");
            RunCommand(_gitexe, $"--no-pager commit -m \"{commitmessage}\"");

            Log("Setting config...");
            RunCommand(_gitexe, "config push.default simple");

            Log("Pushing...");
            RunCommand(_gitexe, "--no-pager push");
        }

        private class Entry
        {
            public string Ipaddress { get; set; }
            public string Hostname { get; set; }
            public bool Used { get; set; }
        }

        private static void UpdateLocalHostsFile(string syncfile)
        {
            var updateLocalHostsFile = Environment.GetEnvironmentVariable("UpdateLocalHostsFile");
            if (string.IsNullOrEmpty(updateLocalHostsFile) || updateLocalHostsFile == "false")
            {
                Log("Wont update local hosts file.");
                return;
            }

            var domain = Environment.GetEnvironmentVariable("Domain");

            var hostsfile = Path.Combine(Environment.SystemDirectory, @"drivers\etc\hosts");

            List<string> rows = [.. File.ReadAllLines(hostsfile)];

            var rowsPatch = File.ReadAllLines(syncfile);

            Dictionary<string, Entry> patchvalues = [];
            if (!string.IsNullOrEmpty(domain))
            {
                foreach (var row in rowsPatch)
                {
                    if (TryParseRow(row, out string ipaddress, out string hostname))
                    {
                        if (!hostname.EndsWith(domain))
                        {
                            patchvalues[hostname + domain] = new Entry { Ipaddress = ipaddress, Used = false };
                        }
                    }
                }
            }

            foreach (var row in rowsPatch)
            {
                if (TryParseRow(row, out string ipaddress, out string hostname))
                {
                    patchvalues[hostname] = new Entry { Ipaddress = ipaddress, Used = false };
                }
            }

            var modifiedRows = 0;

            List<Entry> excessiveEntries = [];
            for (var i = 0; i < rows.Count; i++)
            {
                if (TryParseRow(rows[i], out string ipaddress, out string hostname))
                {
                    excessiveEntries.Add(new Entry { Hostname = hostname, Ipaddress = ipaddress });
                }
            }

            for (var i = 0; i < rows.Count; i++)
            {
                if (!TryParseRow(rows[i], out string ipaddress, out string hostname))
                {
                    continue;
                }

                Log($"Got ip: {ipaddress}");

                if (patchvalues.TryGetValue(hostname, out Entry patchvalue1) && patchvalue1.Ipaddress != ipaddress)
                {
                    var old = rows[i];
                    rows[i] = $"{patchvalue1.Ipaddress,-15} {hostname}";
                    Log($"Updating local hosts file ip address: '{old}' -> '{rows[i]}'");
                    modifiedRows++;
                }

                if (patchvalues.TryGetValue(hostname, out Entry patchvalue2))
                {
                    patchvalue2.Used = true;
                    _ = excessiveEntries.Remove(excessiveEntries.First(e => e.Hostname == hostname && e.Ipaddress == ipaddress));
                }
            }

            foreach (var entry in patchvalues.Where(e => !e.Value.Used))
            {
                var row = $"{entry.Value.Ipaddress,-15} {entry.Key}";
                Log($"Adding ip+host to local hosts file: '{row}'");
                rows.Add(row);
                modifiedRows++;
                entry.Value.Used = true;
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
            foreach (var entry in excessiveEntries)
            {
                Log($"{entry.Ipaddress,-15} {entry.Hostname}");
            }
        }

        private static bool TryParseRow(string row, out string ipaddress, out string hostname)
        {
            row = row.Trim();

            if (row == string.Empty || row.StartsWith('#'))
            {
                ipaddress = hostname = null;
                return false;
            }

            char[] separators = [' ', '\t'];

            var tokens = row.Split(separators, StringSplitOptions.RemoveEmptyEntries);
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

            using Process process = new()
            {
                StartInfo = new ProcessStartInfo(exefile, args)
                {
                    UseShellExecute = false
                }
            };

            if (!process.Start())
            {
                throw new ApplicationException($"Failed to execute: '{exefile}', args: '{args}'");
            }
            process.WaitForExit();

            if (process.ExitCode != 0)
            {
                throw new ApplicationException($"Failed to execute: '{exefile}', args: '{args}'");
            }
        }

        private static void LogColor(string message, ConsoleColor color)
        {
            var oldColor = Console.ForegroundColor;
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
            var hostname = Dns.GetHostName();
            Console.WriteLine($"{hostname}: {message}");
        }
    }
}
