using System;
using System.IO;
using System.Linq;
using System.Text;

namespace base64encode
{
    class Program
    {
        static int Main(string[] args)
        {
            var parsedArgs = args;
            var usefile = parsedArgs.Contains("-f");
            parsedArgs = [.. parsedArgs.Where(a => a != "-f")];
            if (parsedArgs.Length != 1)
            {
                Console.WriteLine("Usage: base64encode [-f] <filename or string>");
                return 1;
            }

            Console.WriteLine(Convert.ToBase64String(usefile ?
                File.ReadAllBytes(parsedArgs[0]) :
                Encoding.UTF8.GetBytes(parsedArgs[0])));

            return 0;
        }
    }
}
