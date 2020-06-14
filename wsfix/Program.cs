using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;

namespace wsfix
{
    class Program
    {
        static int Main(string[] args)
        {
            var parsedArgs = args;

            var dryrun = parsedArgs.Contains("-dryrun");
            parsedArgs = parsedArgs.Where(a => a != "-dryrun").ToArray();

            if (parsedArgs.Length < 1 || parsedArgs.Length > 2)
            {
                Console.WriteLine("Usage: wsfix <path> [tabsize] [-dryrun]");
                return 1;
            }

            var tabsize = 4;
            if (parsedArgs.Length == 2 && !int.TryParse(parsedArgs[1], out tabsize))
            {
                Console.WriteLine($"Couldn't parse tabsize as int: '{parsedArgs[1]}'");
                return 1;
            }

            var filename = args[0];

            if (!Fix(filename, tabsize, dryrun))
            {
                return 1;
            }

            return 0;
        }

        static bool Fix(string path, int tabsize, bool dryrun)
        {
            if (!Directory.Exists(path))
            {
                Console.WriteLine($"Directory not found: '{path}'");
                return false;
            }

            var files = Directory.GetFiles(path, "*.cs", SearchOption.AllDirectories)
                .Select(f => f.StartsWith($".{Path.DirectorySeparatorChar}") || f.StartsWith($".{Path.AltDirectorySeparatorChar}") ? f.Substring(2) : f)
                .ToArray();

            foreach (var filename in files)
            {
                FixCsFile(filename, tabsize, dryrun);
            }

            return true;
        }

        static void FixCsFile(string filename, int tabsize, bool dryrun)
        {
            Console.WriteLine($"Reading: '{filename}'");

            byte[] buf = File.ReadAllBytes(filename);
            bool writeutf8bom = buf.Length >= 3 && buf[0] == 239 && buf[1] == 187 && buf[2] == 191;

            string content = File.ReadAllText(filename);

            string newcontent = FixCsCode(content);

            if (content != newcontent && !dryrun)
            {
                if (writeutf8bom)
                {
                    File.WriteAllText(filename, newcontent, Encoding.UTF8);
                }
                else
                {
                    File.WriteAllText(filename, newcontent);
                }
            }

            static string FixCsCode(string content)
            {
                var chars = content.ToList();

                int startofline = 0;
                for (int i = 0; i < chars.Count; i++)
                {
                    char c = chars[i];
                    if (c == '\r' || c == '\n')
                    {
                        int firsttrail = i;
                        while (firsttrail > startofline && (chars[firsttrail - 1] == ' ' || chars[firsttrail - 1] == '\t'))
                        {
                            firsttrail--;
                        }

                        if (firsttrail < i)
                        {
                            chars.RemoveRange(firsttrail, i - firsttrail);
                        }
                    }
                }

                return string.Join(string.Empty, chars);
            }
        }
    }
}
