using Newtonsoft.Json.Linq;
using System;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;

namespace sqltoelastic
{
    class Elastic
    {
        public static string _logfile;

        public void PutIntoIndex(string serverurl, string username, string password, string indexname,
            string typename, string timestampfield, string idprefix, string idfield, JObject[] jsonrows)
        {
            using (WebClient client = new WebClient())
            {
                if (username != null && password != null)
                {
                    string credentials = Convert.ToBase64String(Encoding.ASCII.GetBytes($"{username}:{password}"));
                    client.Headers[HttpRequestHeader.Authorization] = $"Basic {credentials}";
                }

                foreach (JObject jsonrow in jsonrows)
                {
                    client.Headers["Content-Type"] = "application/json";
                    client.Headers["Accept"] = "application/json";

                    jsonrow["@timestamp"] = jsonrow[timestampfield];
                    DateTime created = jsonrow[timestampfield].Value<DateTime>();

                    string datestring = created.ToString("yyyy.MM");
                    string dateindexname = $"{indexname}-{datestring}";

                    string id = $"{idprefix}{jsonrow[idfield].Value<string>()}";

                    string address = $"{serverurl}/{dateindexname}/{typename}/{id}";

                    string data = jsonrow.ToString();
                    string result = string.Empty;

                    try
                    {
                        client.Encoding = Encoding.UTF8;
                        result = client.UploadString(address, "PUT", data);
                    }
                    catch (WebException ex)
                    {
                        Log($"Put '{address}': >>>{data}<<<");
                        Log($"Result: >>>{result}<<<");
                        Log($"Exception: >>>{ex.ToString()}<<<");
                    }
                }

                Log("Done!");
            }
        }

        private void Log(string message)
        {
            string date = DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss");
            File.AppendAllText(_logfile, $"{date}: {message}{Environment.NewLine}");
        }
    }
}
