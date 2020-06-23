using System;
using System.IO;
using System.Linq;

namespace base64decode
{
    class Program
    {
        static int Main(string[] args)
        {
            bool usefile = args.Contains("-f");
            var parsedArgs = args.Where(a => a != "-f").ToArray();
            if (parsedArgs.Length != 1 && parsedArgs.Length != 2)
            {
                Console.WriteLine("Usage: base64decode [-f] <filename or string> [outfile]");
                return 1;
            }

            byte[] bytes = usefile ?
                File.ReadAllBytes(parsedArgs[0]) :
                Convert.FromBase64String(parsedArgs[0]);

            if (parsedArgs.Length == 2)
            {
                File.WriteAllBytes(parsedArgs[1], bytes);
            }
            else
            {
                foreach (byte b in bytes)
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
