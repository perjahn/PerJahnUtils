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

            string path = args.Length == 1 ? args[0] : ".";

            string[] files = Directory.GetFiles(path, "*.cs", SearchOption.AllDirectories)
                .Select(f => f.StartsWith($".{Path.DirectorySeparatorChar}") || f.StartsWith($".{Path.AltDirectorySeparatorChar}") ? f.Substring(2) : f)
                .ToArray();

            Array.Sort(files);

            Console.WriteLine($"Found {files.Length} cs files.");
            long filecount = 0;

            foreach (var filename in files)
            {
                string content = File.ReadAllText(filename);
                int start = 0;

                for (int i = 0; i < content.Length; i++)
                {
                    if (content[i] == '\r' || content[i] == '\n')
                    {
                        if ((start <= i - 2 && content[start] == '/' && content[start + 1] == '/')
                            ||
                            (content[start..i].All(c => char.IsWhiteSpace(c))))
                        {
                            start = i + 1;
                        }
                    }
                }

                if (start > 0 && !content[0..start].Contains("auto-generated"))
                {
                    content = content.Substring(start);

                    Console.WriteLine($"Updating: '{filename}'");
                    File.WriteAllText(filename, content);
                    filecount++;
                }
            }

            Console.WriteLine($"Updated {filecount} files: {watch.Elapsed}");
        }
    }
}
