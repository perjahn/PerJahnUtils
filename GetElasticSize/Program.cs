using Newtonsoft.Json.Linq;
using System;
using System.Globalization;
using System.Linq;
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

            var serverurl = args[0];
            var username = args[1];
            var password = args[2];

            await GetSize(serverurl, username, password);

            return 0;
        }

        private static async Task GetSize(string serverurl, string username, string password)
        {
            using HttpClient client = new();

            var credentials = Convert.ToBase64String(Encoding.ASCII.GetBytes($"{username}:{password}"));
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", credentials);

            client.DefaultRequestHeaders.Add("Accept", "application/json");

            var address = $"{serverurl}/_cat/indices?pretty";

            Log($"'{address}'");

            var result = await client.GetStringAsync(address);

            var indices = JArray.Parse(result);

            var maxlength = indices.Max(i => i["index"]?.Value<string>()?.Length ?? 0);

            foreach (var index in indices.OrderBy(i => -GetSizeFromPrefix(i["store.size"]?.Value<string>() ?? string.Empty)))
            {
                var separator = $"  {new string(' ', maxlength - index["index"]?.Value<string>()?.Length ?? 0)}";
                Console.WriteLine($"{index["index"]}{separator}{index["store.size"]}");
            }
        }

        private static long GetSizeFromPrefix(string prefixsize)
        {
            if (prefixsize.EndsWith("tb"))
            {
                return (long)(double.Parse(prefixsize[..^2], CultureInfo.InvariantCulture) * 1024 * 1024 * 1024 * 1024);
            }
            else if (prefixsize.EndsWith("gb"))
            {
                return (long)(double.Parse(prefixsize[..^2], CultureInfo.InvariantCulture) * 1024 * 1024 * 1024);
            }
            else if (prefixsize.EndsWith("mb"))
            {
                return (long)(double.Parse(prefixsize[..^2], CultureInfo.InvariantCulture) * 1024 * 1024);
            }
            else if (prefixsize.EndsWith("kb"))
            {
                return (long)(double.Parse(prefixsize[..^2], CultureInfo.InvariantCulture) * 1024);
            }
            else if (prefixsize.EndsWith('b'))
            {
                return (long)double.Parse(prefixsize[..^1], CultureInfo.InvariantCulture);
            }
            return prefixsize == string.Empty ? 0 : throw new NotImplementedException($"Unknown size: '{prefixsize}'");
        }

        private static void Log(string message)
        {
            Console.WriteLine($"{message}");
        }
    }
}
