using System;
using System.Net;

namespace urldecode
{
    class Program
    {
        static int Main(string[] args)
        {
            if (args.Length != 1)
            {
                Console.WriteLine("Usage: urlencode <string>");
                return 1;
            }

            Console.WriteLine(WebUtility.UrlEncode(args[0]));

            return 0;
        }
    }
}
