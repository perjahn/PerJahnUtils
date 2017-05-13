using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Base64Encode
{
    class Program
    {
        static int Main(string[] args)
        {
            if (args.Length != 1)
            {
                Console.WriteLine("Usage: Base64Encode <filename>");
                return 1;
            }

            byte[] bytes = File.ReadAllBytes(args[0]);
            string encodedstring = Convert.ToBase64String(bytes);
            Console.WriteLine(encodedstring);

            return 0;
        }
    }
}
