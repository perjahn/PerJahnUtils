using System;
using System.IO;
using System.Linq;

namespace base64decode
{
    class Program
    {
        static int Main(string[] args)
        {
            var usefile = args.Contains("-f");
            string[] parsedArgs = [.. args.Where(a => a != "-f")];
            if (parsedArgs.Length is not 1 and not 2)
            {
                Console.WriteLine("Usage: base64decode [-f] <filename or string> [outfile]");
                return 1;
            }

            var bytes = usefile ?
                File.ReadAllBytes(parsedArgs[0]) :
                Convert.FromBase64String(parsedArgs[0]);

            if (parsedArgs.Length == 2)
            {
                File.WriteAllBytes(parsedArgs[1], bytes);
            }
            else
            {
                foreach (var b in bytes)
                {
                    char c = (char)b;
                    Console.Write(c);
                }
                Console.WriteLine();
            }

            return 0;
        }
    }
}
