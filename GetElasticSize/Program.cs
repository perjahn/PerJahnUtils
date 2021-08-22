using Newtonsoft.Json.Linq;
using System;
using System.Globalization;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;

namespace GetElasticSize
{
    class Program
    {
        static async Task<int> Main(string[] args)
        {
            if (args.Length != 3)
            {
                Log("Usage: GetElasticSize <serverurl> <username> <password>");
                return 1;
            }

            string serverurl = args[0];
            string username = args[1];
            string password = args[2];

            await GetSize(serverurl, username, password);

            return 0;
        }

        private static async Task GetSize(string serverurl, string username, string password)
        {
            using var client = new HttpClient();

            string credentials = Convert.ToBase64String(Encoding.ASCII.GetBytes($"{username}:{password}"));
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", credentials);

            client.DefaultRequestHeaders.Add("Accept", "application/json");


            string address = $"{serverurl}/_cat/indices?pretty";

            Log($"'{address}'");

            string result = await client.GetStringAsync(address);

            JArray indices = JArray.Parse(result);

            int maxlength = indices.Max(i => i["index"]?.Value<string>()?.Length ?? 0);

            foreach (JToken index in indices.OrderBy(i => -GetSizeFromPrefix(i["store.size"]?.Value<string>() ?? string.Empty)))
            {
                string separator = $"  {new string(' ', maxlength - index["index"]?.Value<string>()?.Length ?? 0)}";
                Console.WriteLine($"{index["index"]}{separator}{index["store.size"]}");
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
