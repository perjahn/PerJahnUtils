using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Base64Decode
{
    class Program
    {
        static int Main(string[] args)
        {
            if (args.Length != 1)
            {
                Console.WriteLine("Usage: Base64Decode <string>");
                return 1;
            }

            //string text = File.ReadAllText(args[0]);
            byte[] bytes = Convert.FromBase64String(args[0]);
            foreach (byte b in bytes)
            {
                char c = (char)b;
                Console.Write(c);
            }
            Console.WriteLine();

            return 0;
        }
    }
}
