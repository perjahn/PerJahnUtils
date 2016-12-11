using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using WUApiLib;

namespace Patcher
{
    class Patch
    {
        private struct LUID
        {
            public int LowPart;
            public int HighPart;
        }

        private struct LUID_AND_ATTRIBUTES
        {
            public LUID pLuid;
            public int Attributes;
        }

        private struct TOKEN_PRIVILEGES
        {
            public int PrivilegeCount;
            public LUID_AND_ATTRIBUTES Privileges;
        }

        [DllImport("advapi32.dll")]
        static extern int OpenProcessToken(IntPtr ProcessHandle, int DesiredAccess, out IntPtr TokenHandle);

        [DllImport("advapi32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        static extern bool AdjustTokenPrivileges(IntPtr TokenHandle,
            [MarshalAs(UnmanagedType.Bool)]bool DisableAllPrivileges,
            ref TOKEN_PRIVILEGES NewState,
            UInt32 BufferLength,
            IntPtr PreviousState,
            IntPtr ReturnLength);

        [DllImport("advapi32.dll")]
        static extern int LookupPrivilegeValue(string lpSystemName, string lpName, out LUID lpLuid);

        [DllImport("user32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        static extern bool ExitWindowsEx(int uFlags, int dwReason);

        const string SE_SHUTDOWN_NAME = "SeShutdownPrivilege";
        const short SE_PRIVILEGE_ENABLED = 2;
        const short TOKEN_ADJUST_PRIVILEGES = 32;
        const short TOKEN_QUERY = 8;

        public void InstallPatches()
        {
            Log("Searching for updates...");

            List<IUpdate5> updates;

            do
            {
                UpdateSession session = new UpdateSession();

                updates = GetPatches(session);

                PrintStats(updates);

                DownloadPatches(session, updates);

                updates = GetPatches(session);

                InstallPatches(session, updates);

                updates = GetPatches(session);
            }
            while (updates.Count() > 0);

            Log("Done!");

            return;
        }

        private void Log(string message)
        {
            File.AppendAllText(Path.Combine(Path.GetTempPath(), "Patcher.log"),
                DateTime.UtcNow.ToString() + ": " + message + Environment.NewLine);
        }

        private List<IUpdate5> GetPatches(UpdateSession session)
        {
            UpdateServiceManager manager = new UpdateServiceManager();

            Log("Found " + manager.Services.Count + " update services.");

            List<IUpdate5> updates = new List<IUpdate5>();
            foreach (IUpdateService2 service in manager.Services)
            {
                Log("Retrieving patches from: " + service.Name);

                try
                {
                    var searcher = session.CreateUpdateSearcher();
                    searcher.ServerSelection = ServerSelection.ssWindowsUpdate;
                    searcher.ServiceID = service.ServiceID;

                    ISearchResult searchresult = searcher.Search("");

                    UpdateCollection updatecollection = searchresult.Updates;

                    Log("Found " + updatecollection.Count + " updates.");

                    foreach (IUpdate5 update in updatecollection)
                    {
                        if (!updates.Any(u => u.Title == update.Title))
                        {
                            updates.Add(update);
                        }
                    }
                }
                catch (COMException ex)
                {
                    Log("Couldn't retrive patches: 0x" + ex.HResult.ToString("X"));
                    Log(ex.ToString());
                }
            }

            return updates;
        }

        private void PrintStats(List<IUpdate5> updates)
        {
            string printsize = GetPrintSize(updates.Sum(u => u.MaxDownloadSize));

            Log("Total unique updates: " + updates.Count + ": " + printsize + " MB.");
        }

        private void ListPatches(List<IUpdate5> updates)
        {
            foreach (IUpdate5 update in updates.OrderBy(u => u.Title))
            {
                Log(update.Title + ": " + GetPrintSize(update.MaxDownloadSize) + " MB.");
            }
        }

        private void DownloadPatches(UpdateSession session, List<IUpdate5> updates)
        {
            Log("Downloading " + updates.Count + " patches...");

            foreach (IUpdate5 update in updates.OrderBy(u => u.Title))
            {
                if (update.IsDownloaded)
                {
                    Log("Patch is already downloaded: " + update.Title);
                    continue;
                }


                UpdateCollection updateCollection = new UpdateCollection();
                updateCollection.Add(update);

                UpdateDownloader downloader = session.CreateUpdateDownloader();
                downloader.Updates = updateCollection;

                bool downloaded = false;

                for (int tries = 0; tries < 3 && !downloaded; tries++)
                {
                    try
                    {
                        string printtry = tries > 0 ? " (try " + (tries + 1) + ")" : string.Empty;

                        Log("Downloading" + printtry + ": " + update.Title + ": " + GetPrintSize(update.MaxDownloadSize) + " MB.");

                        IDownloadResult downloadresult = downloader.Download();
                        if (downloadresult.ResultCode == OperationResultCode.orcSucceeded)
                        {
                            downloaded = true;
                        }
                        else
                        {
                            Log("Couldn't download patch: " + downloadresult.ResultCode + ": 0x" + downloadresult.HResult.ToString("X"));
                        }
                    }
                    catch (COMException ex)
                    {
                        Log("Couldn't download patch: 0x" + ex.HResult.ToString("X"));
                    }
                }
            }
        }

        private void InstallPatches(UpdateSession session, List<IUpdate5> updates)
        {
            Log("Installing " + updates.Count + " patches...");

            bool reboot = false;

            foreach (IUpdate5 update in updates.OrderBy(u => u.Title))
            {
                if (update.IsInstalled)
                {
                    Log("Patch is already installed: " + update.Title);
                    continue;
                }
                else if (!update.IsDownloaded)
                {
                    Log("Patch isn't downloaded yet: " + update.Title);
                }
                else
                {
                    try
                    {
                        Log("Installing: " + update.Title);

                        UpdateCollection updateCollection = new UpdateCollection();
                        updateCollection.Add(update);

                        IUpdateInstaller installer = session.CreateUpdateInstaller();
                        installer.Updates = updateCollection;

                        IInstallationResult installresult = installer.Install();
                        if (installresult.ResultCode == OperationResultCode.orcSucceeded)
                        {
                            if (installresult.RebootRequired)
                            {
                                reboot = true;
                            }
                        }
                        else
                        {
                            Log("Couldn't install patch: " + installresult.ResultCode + ": 0x" + installresult.HResult.ToString("X"));
                        }
                    }
                    catch (COMException ex)
                    {
                        Log("Couldn't download patch: 0x" + ex.HResult.ToString("X"));
                    }
                }
            }

            string regpath = @"SOFTWARE\Microsoft\Windows\CurrentVersion\WindowsUpdate\Auto Update\RebootRequired";
            if (reboot || CheckIfLocalMachineKeyExists(regpath))
            {
                Log("Rebooting");

                IntPtr hToken;
                TOKEN_PRIVILEGES tkp;

                OpenProcessToken(Process.GetCurrentProcess().Handle, TOKEN_ADJUST_PRIVILEGES | TOKEN_QUERY, out hToken);
                tkp.PrivilegeCount = 1;
                tkp.Privileges.Attributes = SE_PRIVILEGE_ENABLED;
                LookupPrivilegeValue("", SE_SHUTDOWN_NAME, out tkp.Privileges.pLuid);
                AdjustTokenPrivileges(hToken, false, ref tkp, 0U, IntPtr.Zero, IntPtr.Zero);

                if (!ExitWindowsEx(6, 0))
                {
                    Log("Couldn't reboot.");
                }
            }
        }

        private string GetPrintSize(decimal size)
        {
            return size > 0 && (int)(size / 1024 / 1024) == 0 ? "<1" : ((int)(size / 1024 / 1024)).ToString();
        }

        private bool CheckIfLocalMachineKeyExists(string regpath)
        {
            RegistryKey key = Registry.LocalMachine.OpenSubKey(regpath);
            if (key != null)
            {
                key.Close();
                return true;
            }
            else
            {
                return false;
            }
        }
    }
}
