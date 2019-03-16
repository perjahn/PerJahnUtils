using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Base64Decode
{
    class Program
    {
        static int Main(string[] args)
        {
            if (args.Length != 1 && args.Length != 2)
            {
                Console.WriteLine("Usage: Base64Decode <string> [outfile]");
                return 1;
            }

            //string text = File.ReadAllText(args[0]);
            byte[] bytes = Convert.FromBase64String(args[0]);
            if (args.Length == 2)
            {
                string filename = args[1];
                using (var fs = new FileStream(filename, FileMode.Create))
                {
                    fs.Write(bytes, 0, bytes.Length);
                }
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
