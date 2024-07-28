using System;
using System.Diagnostics;
using System.IO;
using System.Linq;

namespace RemoveCSJunk
{
    class Program
    {
        static void Main(string[] args)
        {
            var watch = Stopwatch.StartNew();

            var path = args.Length == 1 ? args[0] : ".";

            string[] files = [.. Directory.GetFiles(path, "*.cs", SearchOption.AllDirectories)
                .Select(f => f.StartsWith($".{Path.DirectorySeparatorChar}") || f.StartsWith($".{Path.AltDirectorySeparatorChar}") ? f[2..] : f)];

            Array.Sort(files);

            Console.WriteLine($"Found {files.Length} cs files.");
            long filecount = 0;

            foreach (var filename in files)
            {
                var content = File.ReadAllText(filename);
                var start = 0;

                for (var i = 0; i < content.Length; i++)
                {
                    if (content[i] is '\r' or '\n')
                    {
                        if ((start <= i - 2 && content[start] == '/' && content[start + 1] == '/')
                            ||
                            content[start..i].All(char.IsWhiteSpace))
                        {
                            start = i + 1;
                        }
                    }
                }

                if (start > 0 && !content[0..start].Contains("auto-generated"))
                {
                    content = content[start..];

                    Console.WriteLine($"Updating: '{filename}'");
                    File.WriteAllText(filename, content);
                    filecount++;
                }
            }

            Console.WriteLine($"Updated {filecount} files: {watch.Elapsed}");
        }
    }
}
