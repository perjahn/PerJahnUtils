using Newtonsoft.Json.Linq;
using System;
using System.Globalization;
using System.Linq;
using System.Net;
using System.Text;

namespace GetElasticSize
{
    class Program
    {
        static int Main(string[] args)
        {
            if (args.Length != 3)
            {
                Log("Usage: GetElasticSize <serverurl> <username> <password>");
                return 1;
            }

            string serverurl = args[0];
            string username = args[1];
            string password = args[2];

            GetSize(serverurl, username, password);

            return 0;
        }

        private static void GetSize(string serverurl, string username, string password)
        {
            using (WebClient client = new WebClient())
            {
                string credentials = Convert.ToBase64String(Encoding.ASCII.GetBytes($"{username}:{password}"));
                client.Headers[HttpRequestHeader.Authorization] = $"Basic {credentials}";

                client.Headers["Accept"] = "application/json";
                client.Encoding = Encoding.UTF8;


                string address = $"{serverurl}/_cat/indices?pretty";

                Log($"'{address}'");

                string result = client.DownloadString(address);

                JArray indices = JArray.Parse(result);

                int maxlength = indices.Max(i => i["index"].ToString().Length);

                foreach (JToken index in indices.OrderBy(i => -GetSizeFromPrefix(i["store.size"].ToString())))
                {
                    string separator = $"  {new string(' ', maxlength - index["index"].ToString().Length)}";
                    Console.WriteLine($"{index["index"]}{separator}{index["store.size"]}");
                }
            }
        }

        private static long GetSizeFromPrefix(string prefixsize)
        {
            if (prefixsize.EndsWith("tb"))
            {
                return (long)(double.Parse(prefixsize.Substring(0, prefixsize.Length - 2), CultureInfo.InvariantCulture) * 1024 * 1024 * 1024 * 1024);
            }
            else if (prefixsize.EndsWith("gb"))
            {
                return (long)(double.Parse(prefixsize.Substring(0, prefixsize.Length - 2), CultureInfo.InvariantCulture) * 1024 * 1024 * 1024);
            }
            else if (prefixsize.EndsWith("mb"))
            {
                return (long)(double.Parse(prefixsize.Substring(0, prefixsize.Length - 2), CultureInfo.InvariantCulture) * 1024 * 1024);
            }
            else if (prefixsize.EndsWith("kb"))
            {
                return (long)(double.Parse(prefixsize.Substring(0, prefixsize.Length - 2), CultureInfo.InvariantCulture) * 1024);
            }
            else if (prefixsize.EndsWith("b"))
            {
                return (long)(double.Parse(prefixsize.Substring(0, prefixsize.Length - 1), CultureInfo.InvariantCulture));
            }
            else if (prefixsize == string.Empty)
            {
                return 0;
            }
            else
            {
                throw new NotImplementedException($"Unknown size: '{prefixsize}'");
            }
        }

        private static void Log(string message)
        {
            Console.WriteLine($"{message}");
        }
    }
}
