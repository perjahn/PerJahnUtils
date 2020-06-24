using System;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;

namespace sha512
{
    class Program
    {
        static int Main(string[] args)
        {
            bool useFile = args.Contains("-f");
            var parsedArgs = args.Where(a => a != "-f").ToArray();
            bool useBase64 = parsedArgs.Contains("-base64");
            parsedArgs = parsedArgs.Where(a => a != "-base64").ToArray();
            if (parsedArgs.Length != 1)
            {
                Console.WriteLine("Usage: sha512 [-f] [-base64] <filename or string>");
                return 1;
            }

            var hash = GetHashString(useFile ?
                File.ReadAllBytes(parsedArgs[0]) :
                Encoding.UTF8.GetBytes(parsedArgs[0]), useBase64);

            if (Console.IsOutputRedirected)
            {
                Console.Write(hash);
            }
            else
            {
                Console.WriteLine(hash);
            }

            return 0;
        }

        public static string GetHashString(byte[] buf, bool useBase64)
        {
            using var crypto = new SHA512Managed();
            return useBase64 ?
                Convert.ToBase64String(crypto.ComputeHash(buf)):
                string.Concat(crypto.ComputeHash(buf).Select(b => b.ToString("x2")));
        }
    }
}
