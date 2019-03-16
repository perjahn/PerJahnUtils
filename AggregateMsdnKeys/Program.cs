using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml;
using System.Xml.Linq;

namespace AggregateMsdnKeys
{
    class Program
    {
        static long _progressFiles = 0;
        static long _progressDirectories = 0;

        static void Main(string[] args)
        {
            DriveInfo[] drives;
            try
            {
                drives = DriveInfo.GetDrives();
            }
            catch (Exception ex) when (ex is IOException)
            {
                Console.WriteLine($"Couldn't get drives: {ex.Message}");
                return;
            }

            List<string> files = new List<string>();

            foreach (DriveInfo drive in drives)
            {
                if (drive.DriveType == DriveType.Fixed)
                {
                    RecurseDirectory(drive.RootDirectory.FullName, files);
                    //RecurseDirectory(@"D:\c\msdn", files);
                }
            }

            Console.WriteLine($"Found {files.Count} files.");

            Dictionary<string, List<string>> allkeys = new Dictionary<string, List<string>>();

            foreach (string filename in files)
            {
                Console.WriteLine($"Reading: '{filename}'");
                XDocument xdoc;
                try
                {
                    xdoc = XDocument.Load(filename);
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.ToString());
                    continue;
                }

                var keys = xdoc
                    .Descendants("Key")
                    .Select(el => new { name = el.Parent.Attribute("Name").Value, key = el.FirstNode.NodeType == XmlNodeType.Text ? el.Value.Trim() : null })
                    .Where(k => k.key != null);

                foreach (var key in keys)
                {
                    if (allkeys.ContainsKey(key.name))
                    {
                        if (!allkeys[key.name].Contains(key.key))
                        {
                            allkeys[key.name].Add(key.key);
                        }
                    }
                    else
                    {
                        allkeys[key.name] = new List<string>
                        {
                            key.key
                        };
                    }
                }
            }

            Console.WriteLine($"Found {allkeys.Count} products.");
            Console.WriteLine($"Found {allkeys.Sum(p => p.Value.Count)} keys.");

            List<string> rows = new List<string>();

            foreach (var product in allkeys.Keys.OrderBy(p => p))
            {
                foreach (var key in allkeys[product].OrderBy(k => k))
                {
                    Console.WriteLine($"{product}\t{key}");
                    rows.Add($"{product}\t{key}");
                }
            }

            if (args.Length == 1)
            {
                string filename = args[0];
                File.WriteAllLines(filename, rows);
            }
        }

        static void RecurseDirectory(string path, List<string> files)
        {
            try
            {
                string[] localfiles = Directory.GetFiles(path, "*.xml");
                foreach (string filename in localfiles)
                {
                    _progressFiles++;
                    if (_progressFiles % 10000 == 0)
                    {
                        Console.WriteLine($"Files: {_progressFiles}, found: {files.Count}");
                    }

                    if (new FileInfo(filename).Length < 1024 * 1024)
                    {
                        string[] rows = File.ReadAllLines(filename);
                        if (rows.Length > 0 && rows[0] == "<YourKey>")
                        {
                            files.Add(filename);
                        }
                    }
                }
                string[] folders = Directory.GetDirectories(path, "*");
                foreach (string folder in folders)
                {
                    _progressDirectories++;
                    if (_progressDirectories % 10000 == 0)
                    {
                        Console.WriteLine($"Directories: {_progressDirectories}, found: {files.Count}");
                    }

                    RecurseDirectory(folder, files);
                }
            }
            catch (Exception ex) when (ex is IOException || ex is UnauthorizedAccessException)
            {
                Console.WriteLine(ex.Message);
            }
        }
    }
}
