using System;
using System.Collections.Generic;
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
            parsedArgs = [.. parsedArgs.Where(a => a != "-dryrun")];

            if (parsedArgs.Length is < 1 or > 2)
            {
                Console.WriteLine("Usage: wsfix <path> [tabsize] [-dryrun]");
                return 1;
            }

            if (parsedArgs.Length == 2 && !int.TryParse(parsedArgs[1], out int tabsize))
            {
                Console.WriteLine($"Couldn't parse tabsize as int: '{parsedArgs[1]}'");
                return 1;
            }

            var filename = args[0];

            return Fix(filename, dryrun) ? 0 : 1;
        }

        static bool Fix(string path, bool dryrun)
        {
            if (!Directory.Exists(path))
            {
                Console.WriteLine($"Directory not found: '{path}'");
                return false;
            }

            string[] files = [.. Directory.GetFiles(path, "*.cs", SearchOption.AllDirectories)
                .Select(f => f.StartsWith($".{Path.DirectorySeparatorChar}") || f.StartsWith($".{Path.AltDirectorySeparatorChar}") ? f[2..] : f)];

            foreach (var filename in files)
            {
                FixCsFile(filename, dryrun);
            }

            return true;
        }

        static void FixCsFile(string filename, bool dryrun)
        {
            Console.WriteLine($"Reading: '{filename}'");

            var buf = File.ReadAllBytes(filename);
            var writeutf8bom = buf.Length >= 3 && buf[0] == 239 && buf[1] == 187 && buf[2] == 191;

            var content = File.ReadAllText(filename);

            var newcontent = FixCsCode(content);

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
        }

        static string FixCsCode(string content)
        {
            List<char> chars = [.. content];

            var startofline = 0;
            for (var i = 0; i < chars.Count; i++)
            {
                var c = chars[i];
                if (c is '\r' or '\n')
                {
                    var firsttrail = i;
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
