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
            if (args.Length != 1)
            {
                Console.WriteLine("sha256 <filename or string>");
                return 1;
            }

            string content;
            byte[] buf;
            if (File.Exists(args[0]))
            {
                buf = File.ReadAllBytes(args[0]);
                Console.WriteLine(GetHashString(buf));
            }
            else
            {
                content = args[0];
                Console.WriteLine(GetHashString(Encoding.UTF8.GetBytes(content)));
            }

            return 0;
        }

        public static string GetHashString(byte[] buf)
        {
            using var crypto = new SHA256Managed();
            return string.Concat(crypto.ComputeHash(buf).Select(b => b.ToString("x2")));
        }
    }
}
