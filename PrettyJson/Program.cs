using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.IO;

namespace PrettyJson
{
    class Program
    {
        static int Main(string[] args)
        {
            if (args.Length != 2)
            {
                Console.WriteLine("Usage: <infile> <outfile>");
                return 1;
            }


            string content = File.ReadAllText(args[0]);

            string pretty = JToken.Parse(content).ToString(Formatting.Indented);

            File.WriteAllText(args[1], pretty);

            return 0;
        }
    }
}
