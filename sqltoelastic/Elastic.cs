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
        public static StreamWriter _logfile;

        public void PutIntoIndex(string serverurl, string username, string password, string indexname,
            string typename, string timestampfield, string idprefix, string idfield, JObject[] jsonrows)
        {
            string bulkdata;

            using (WebClient client = new WebClient())
            {
                int rownum = 0;

                string address = $"{serverurl}/_bulk";

                StringBuilder sb = new StringBuilder();

                foreach (JObject jsonrow in jsonrows)
                {
                    jsonrow["@timestamp"] = jsonrow[timestampfield];
                    DateTime created = jsonrow[timestampfield].Value<DateTime>();

                    string datestring = created.ToString("yyyy.MM");
                    string dateindexname = $"{indexname}-{datestring}";
                    string id = $"{idprefix}{jsonrow[idfield].Value<string>()}";

                    string metadata = "{ \"index\": { \"_index\": \"" + dateindexname + "\", \"_type\": \"" + typename + "\", \"_id\": \"" + id + "\" } }";
                    sb.AppendLine(metadata);

                    string rowdata = jsonrow.ToString().Replace("\r", string.Empty).Replace("\n", string.Empty);
                    sb.AppendLine(rowdata);

                    rownum++;

                    if (rownum % 1000 == 0)
                    {
                        Log($"Importing rows: {rownum}");

                        bulkdata = sb.ToString();
                        ImportRows(client, address, username, password, bulkdata);
                        sb = new StringBuilder();
                    }
                }

                bulkdata = sb.ToString();
                if (bulkdata.Length > 0)
                {
                    Log($"Importing rows: {rownum}");
                    ImportRows(client, address, username, password, bulkdata);
                }

                Log("Done!");
            }
        }

        private void ImportRows(WebClient client, string address, string username, string password, string bulkdata)
        {
            if (username != null && password != null)
            {
                string credentials = Convert.ToBase64String(Encoding.ASCII.GetBytes($"{username}:{password}"));
                client.Headers[HttpRequestHeader.Authorization] = $"Basic {credentials}";
            }
            client.Headers["Content-Type"] = "application/x-ndjson";
            client.Headers["Accept"] = "application/json";
            client.Encoding = Encoding.UTF8;

            string result = string.Empty;
            try
            {
                result = client.UploadString(address, bulkdata);
            }
            catch (WebException ex)
            {
                Log($"Put '{address}': >>>{bulkdata}<<<");
                Log($"Result: >>>{result}<<<");
                Log($"Exception: >>>{ex.ToString()}<<<");
            }
        }

        private void Log(string message)
        {
            string date = DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss");
            _logfile.WriteLine($"{date}: {message}");
            _logfile.Flush();
        }
    }
}
