using System;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;

namespace sha256
{
    class Program
    {
        static int Main(string[] args)
        {
            bool usefile = args.Contains("-f");
            var parsedArgs = args.Where(a => a != "-f").ToArray();
            if (parsedArgs.Length != 1)
            {
                Console.WriteLine("Usage: sha256 [-f] <filename or string>");
                return 1;
            }

            Console.WriteLine(GetHashString(usefile ?
                File.ReadAllBytes(parsedArgs[0]) :
                Encoding.UTF8.GetBytes(parsedArgs[0])));

            return 0;
        }

        public static string GetHashString(byte[] buf)
        {
            using var crypto = new SHA256Managed();
            return string.Concat(crypto.ComputeHash(buf).Select(b => b.ToString("x2")));
        }
    }
}
