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
            var parsedArgs = args;
            var useFile = parsedArgs.Contains("-f");
            parsedArgs = [.. parsedArgs.Where(a => a != "-f")];
            var useBase64 = parsedArgs.Contains("-base64");
            parsedArgs = [.. parsedArgs.Where(a => a != "-base64")];
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
            return useBase64 ?
                Convert.ToBase64String(SHA512.HashData(buf)) :
                string.Concat(SHA512.HashData(buf).Select(b => b.ToString("x2")));
        }
    }
}
