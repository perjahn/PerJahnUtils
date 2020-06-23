using Newtonsoft.Json.Linq;
using System;
using System.IO;
using System.IO.Enumeration;

namespace jsonformatter
{
    class Program
    {
        static int Main(string[] args)
        {
            if (args.Length != 1)
            {
                Console.WriteLine("Usage: jsonformatter <filename>");
                return 1;
            }

            string filename = args[0];

            if (!File.Exists(filename))
            {
                Console.WriteLine($"Couldn't find file: '{filename}'");
                return 1;
            }

            string json = File.ReadAllText(filename);

            var jobject = JObject.Parse(json);

            File.WriteAllText(filename, jobject.ToString());

            return 0;
        }
    }
}
