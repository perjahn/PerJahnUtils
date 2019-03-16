using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Runtime.InteropServices;
using System.Security.Cryptography;

namespace SyncNugetPackages
{
    class Program
    {
        public enum ResourceScope : uint
        {
            Connected = 0x1,
            GlobalNet = 0x2,
            Remembered = 0x3,
            Recent = 0x4,
            Context = 0x5
        }

        public enum ResourceType : uint
        {
            Any = 0x0,
            Disk = 0x1,
            Print = 0x2,
            Reserved = 0x8,
            Unknown = 0xffffffffu
        }

        public enum ResourceDisplayType : uint
        {
            Generic = 0x0,
            Domain = 0x1,
            Server = 0x2,
            Share = 0x3,
            File = 0x4,
            Group = 0x5,
            Network = 0x6,
            Root = 0x7,
            ShareAdmin = 0x8,
            Directory = 0x9,
            Tree = 0xa,
            NDSContainer = 0xb
        }

        public enum ResourceUsage : uint
        {
            Connectable = 0x1,
            Container = 0x2,
            NoLocalDevice = 0x4,
            Sibling = 0x8,
            Attached = 0x10,
            All = Connectable | Container | Attached,
            Reserved = 0x80000000u
        }


        [StructLayout(LayoutKind.Sequential)]
        public class NETRESOURCE
        {
            public ResourceScope dwScope;
            public ResourceType dwType;
            public ResourceDisplayType dwDisplayType;
            public ResourceUsage dwUsage;
            public string lpLocalName;
            public string lpRemoteName;
            public string lpComment;
            public string lpProvider;
        }

        [DllImport("mpr.dll")]
        public static extern int WNetAddConnection2(NETRESOURCE netResource, string password, string username, uint flags);

        [DllImport("mpr.dll")]
        public static extern int WNetCancelConnection2(string lpName, Int32 dwFlags, bool bForce);

        static MD5 md5 = MD5.Create();

        class Myfileinfo
        {
            public string filename;
            public FileInfo info;
            public byte[] hash;
        };

        static int Main(string[] args)
        {
            string[] parsedArgs = args;

            bool simulate = parsedArgs.Contains("-s");
            parsedArgs = parsedArgs.Where(a => a != "-s").ToArray();

            bool verbose = parsedArgs.Contains("-v");
            parsedArgs = parsedArgs.Where(a => a != "-v").ToArray();

            if (parsedArgs.Length != 3)
            {
                Console.WriteLine(
@"Usage: SyncNugetPackages [-s] [-v] <compressed folder> <extracted folder> <servers>

Program for syncing Nuget folders between build agents. Useful when internet is unreachable.

compressed folder: A custom, local folder where compressed nuget packages are cached.
extracted folder:  A custom, local folder where extracted nuget packages are cached.
servers:           Comma separated list of servers.
-s:                Simulate, dry run.
-v:                Verbose logging.

Notice: The Nuget infrastructure is using both a compressed folder and an extracted
        folder (dunno why), this app doesn't extract or compress anything, it only
        syncs all compressed folders with each other, and all extracted folders with
        each other.

I think this app needs to be run twice to make sure all servers are synced. ToDo.");

                return 1;
            }

            CopyPackages(parsedArgs[0], parsedArgs[1], parsedArgs[2].Split(','), simulate, verbose);

            return 0;
        }

        static void CopyPackages(string compressedFolder, string extractedFolder, string[] servers, bool simulate, bool verbose)
        {
            if (!Directory.Exists(compressedFolder))
            {
                Log("Compressed folder not found: '" + compressedFolder + "'");
                return;
            }
            if (!Directory.Exists(extractedFolder))
            {
                Log("Extracted folder not found: '" + extractedFolder + "'");
                return;
            }


            string[] cachePaths =
            {
                compressedFolder,
                extractedFolder
            };
            string[] serverPaths =
            {
                @"\C$\Windows\SysWOW64\config\systemprofile\AppData\Local\NuGet\Cache",
                @"\C$\Windows\SysWOW64\config\systemprofile\.nuget\packages"
            };

            var operations = cachePaths.Zip(serverPaths, (string cachePath, string serverPath) =>
                new { cachePath, serverPath });

            foreach (var operation in operations)
            {
                string cachePath = operation.cachePath;
                string serverPath = operation.serverPath;

                int cachePathOffset = cachePath.EndsWith(@"\") ? cachePath.Length : cachePath.Length + 1;


                Log("Gathering local files from " + cachePath + "...");
                var filesCache = Directory.GetFiles(cachePath, "*", SearchOption.AllDirectories)
                    .Select(f => new Myfileinfo
                    {
                        filename = f.Substring(cachePathOffset),
                        info = new FileInfo(f),
                        hash = ComputeHash(f)
                    })
                    .ToList();

                Log("Count: " + filesCache.Count());

                Dictionary<string, int> copies = new Dictionary<string, int>
                {
                    ["local"] = 0
                };
                foreach (string server in servers)
                {
                    copies[server] = 0;
                }

                long copiedsize = 0;
                foreach (string server in servers)
                {
                    string networkPath = @"\\" + server + serverPath;
                    string localDrive = "Y:";

                    MapDrive(networkPath, localDrive);

                    SyncFolders(cachePath, cachePathOffset, filesCache, localDrive + @"\", server, simulate, verbose, ref copies, out copiedsize);
                }


                Log("Copied files (" + copiedsize + " bytes):");
                Log("  local: " + copies["local"]);
                foreach (string server in servers)
                {
                    Log("  " + server + ": " + copies[server]);
                }


                Log("Unmapping Y:");
                int result = WNetCancelConnection2("Y:", 1, true);
                if (result != 0)
                {
                    throw new ApplicationException("Couldn't unmap Y:. Error result: " + result);
                }
            }
        }

        static void MapDrive(string uncPath, string localDrive)
        {
            int result;

            if (Directory.Exists(localDrive))
            {
                Log("Unmapping " + localDrive);
                result = WNetCancelConnection2(localDrive, 1, true);
                if (result != 0)
                {
                    throw new ApplicationException("Couldn't unmap " + localDrive + ". Error result: " + result);
                }
            }

            NETRESOURCE netResource = new NETRESOURCE
            {
                dwScope = ResourceScope.GlobalNet,
                dwType = ResourceType.Disk,
                dwDisplayType = ResourceDisplayType.Generic,
                dwUsage = ResourceUsage.All,

                lpComment = null,
                lpLocalName = localDrive,
                lpProvider = null,
                lpRemoteName = uncPath
            };

            Log("Mapping " + uncPath + " to " + localDrive);
            result = WNetAddConnection2(netResource, null, null, 6);
            if (result != 0)
            {
                throw new ApplicationException("Couldn't map " + uncPath + " to " + localDrive + ". Error result: " + result);
            }

            if (!Directory.Exists(localDrive))
            {
                throw new ApplicationException("Couldn't map " + uncPath + " to " + localDrive + ". Directory not found.");
            }
        }

        static void SyncFolders(string path1, int offset1, List<Myfileinfo> files1, string path2,
            string server, bool simulate, bool verbose, ref Dictionary<string, int> copies, out long copiedsize)
        {
            copiedsize = 0;

            int offset2 = path2.EndsWith(@"\") ? path2.Length : path2.Length + 1;

            Log("***** Gathering files from: " + path2 + " *****");

            var files2 = Directory.GetFiles(path2, "*", SearchOption.AllDirectories)
                .SelectMany(f =>
                {
                    Myfileinfo[] infos = new Myfileinfo[1];
                    try
                    {
                        infos[0] = new Myfileinfo
                        {
                            filename = f.Substring(offset2),
                            info = new FileInfo(f),
                            hash = ComputeHash(f)
                        };
                    }
                    catch (Exception ex) when (ex is PathTooLongException || ex is DirectoryNotFoundException || ex is NullReferenceException)
                    {
                        infos = new Myfileinfo[0];
                        Log(f + ": " + ex.Message);
                    }
                    return infos;
                })
                .ToList();

            Log("Server: " + server + ", count: " + files2.Count());

            foreach (var file2 in files2)
            {
                var files = files1.Where(f => f.filename == file2.filename);

                foreach (var file1 in files)
                {
                    if (file2.filename == file1.filename)
                    {
                        if (file2.info.Length != file1.info.Length || file2.info.LastWriteTime != file1.info.LastWriteTime)
                        {
                            if (!file1.hash.SequenceEqual(file2.hash))
                            {
                                if (verbose)
                                {
                                    Log("Diff file: '" + file2.filename +
                                        "', size1: " + file1.info.Length +
                                        ", size2: " + file2.info.Length +
                                        ", date1: " + file1.info.LastWriteTime.ToString("yyyyMMdd HHmmss") +
                                        ", date2: " + file2.info.LastWriteTime.ToString("yyyyMMdd HHmmss") +
                                        ", hash1: " + file1.hash +
                                        ", hash2: " + file2.hash);

                                    Log("Copying (diff): '" + file2.info.FullName + "' -> '" + file1.info.FullName + "'");
                                }
                                if (!simulate)
                                {
                                    File.Copy(file2.info.FullName, file1.info.FullName, true);
                                }
                                copiedsize += file2.info.Length;
                            }
                        }
                    }
                }

                if (files.Count() == 0)
                {
                    string targetfile = Path.Combine(path1, file2.filename);

                    try
                    {
                        if (verbose)
                        {
                            Log("Copying (new1): '" + file2.info.FullName + "' -> '" + targetfile + "'");
                        }
                        string dir = Path.GetDirectoryName(targetfile);
                        if (!Directory.Exists(dir))
                        {
                            Directory.CreateDirectory(dir);
                        }
                        if (!simulate)
                        {
                            File.Copy(file2.info.FullName, targetfile);
                        }
                        copiedsize += file2.info.Length;
                        files1.Add(new Myfileinfo
                        {
                            filename = targetfile.Substring(offset1),
                            info = new FileInfo(targetfile),
                            hash = ComputeHash(targetfile)
                        });
                        copies["local"]++;
                    }
                    catch (Exception ex) when (ex is PathTooLongException || ex is DirectoryNotFoundException || ex is NullReferenceException)
                    {
                        Log("Couldn't copy file: '" + file2.info.FullName + "' -> '" + targetfile + "'");
                        Log(ex.Message);
                    }
                }
            }

            foreach (var file1 in files1)
            {
                var files = files2.Where(f => f.filename == file1.filename);

                if (files.Count() == 0)
                {
                    string targetfile = Path.Combine(path2, file1.filename);

                    try
                    {
                        if (verbose)
                        {
                            Log("Copying (new2): '" + file1.info.FullName + "' -> '" + targetfile + "'");
                        }
                        string dir = Path.GetDirectoryName(targetfile);
                        if (!Directory.Exists(dir))
                        {
                            Directory.CreateDirectory(dir);
                        }
                        if (!simulate)
                        {
                            File.Copy(file1.info.FullName, targetfile);
                        }
                        copiedsize += file1.info.Length;
                        files2.Add(new Myfileinfo
                        {
                            filename = targetfile.Substring(offset2),
                            info = new FileInfo(targetfile),
                            hash = ComputeHash(targetfile)
                        });
                        copies[server]++;
                    }
                    catch (Exception ex) when (ex is PathTooLongException || ex is DirectoryNotFoundException || ex is NullReferenceException)
                    {
                        Log("Couldn't copy file: '" + file1.info.FullName + "' -> '" + targetfile + "'");
                        Log(ex.Message);
                    }
                }
            }
        }

        static byte[] ComputeHash(string filename)
        {
            using (var fs = File.OpenRead(filename))
            {
                return md5.ComputeHash(fs);
            }
        }

        static void Log(string message)
        {
            Console.WriteLine(Dns.GetHostName() + ": " + message);
        }
    }
}
