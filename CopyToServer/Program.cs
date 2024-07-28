using System;
using System.IO;
using System.Linq;
using System.Net;

namespace CopyToServer
{
    class Program
    {
        static int Main(string[] args)
        {
            if (args.Length != 4)
            {
                Console.WriteLine(
@"CopyToServer 0.001 gamma

Usage: CopyToServer <source> <target> <username> <password>");
                return 1;
            }

            return Copy(args[0], args[1], args[2], args[3]);
        }

        static int Copy(string source, string target, string username, string password)
        {
            if (!source.StartsWith(@"\\") && !target.StartsWith(@"\\"))
            {
                Console.WriteLine(@"Source or target path must be an unc path (\\server\share\...).");
                return 1;
            }

            var share = source.StartsWith(@"\\") ? GetShare(source) : GetShare(target);

            NetworkCredential credentials = new(username, password);

            using (new NetworkConnection(share, credentials))
            {
                var dir = source.Contains(Path.DirectorySeparatorChar) ? Path.GetDirectoryName(source) : ".";
                var pattern = Path.GetFileName(source);
                if (dir == null)
                {
                    dir = source;
                    pattern = "*";
                }

                string[] files = [.. Directory.GetFiles(dir, pattern).Select(f => f.StartsWith(@".\") ? f[2..] : f)];

                if (!Directory.Exists(target))
                {
                    Console.WriteLine($"Creating folder: '{target}'");
                    Directory.CreateDirectory(target);
                }

                foreach (var filename in files)
                {
                    var targetfile = Path.Combine(target, Path.GetFileName(filename));
                    Console.WriteLine($"'{filename}' -> '{targetfile}'");
                    try
                    {
                        File.Copy(filename, targetfile);
                    }
                    catch (IOException ex)
                    {
                        Console.WriteLine(ex.Message);
                    }
                }
            }

            return 0;
        }

        static string GetShare(string path)
        {
            var index = path.IndexOf(Path.DirectorySeparatorChar, 2);
            if (index == -1)
            {
                throw new ApplicationException($"Malformed unc path: '{path}'");
            }

            index = path[(index + 1)..].Contains(Path.DirectorySeparatorChar) ?
                path.IndexOf(Path.DirectorySeparatorChar, index + 1) :
                path.Length;
            if (index == -1)
            {
                throw new ApplicationException($"Malformed unc path: '{path}'");
            }

            var share = path[..index];

            Console.WriteLine($"Share: '{path}' -> '{share}'");

            return share;
        }
    }
}
