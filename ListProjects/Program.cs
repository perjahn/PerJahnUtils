using JsonUtils;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Dynamic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace ListProjects
{
    class Program
    {
        static void Main(string[] args)
        {
            if (args.Length != 3)
            {
                Console.WriteLine("Usage: ListProjects <hostname> <username> <password>");
                return;
            }

            GitList(args[0], args[1], args[2]);
        }

        public static void GitList(string hostname, string username, string password)
        {
            string sessionurl = $"https://{hostname}/api/v3/session";

            NameValueCollection values = new NameValueCollection
            {
                { "login", username },
                { "password", password }
            };

            string private_token = GetData(sessionurl, values).private_token;

            int page = 1;
            bool found;
            do
            {
                string projectsurl = $"https://{hostname}/api/v3/projects?private_token={private_token}&page={page}";
                found = false;
                var results = GetData(projectsurl);
                foreach (var result in results)
                {
                    Console.WriteLine(result.http_url_to_repo);
                    found = true;
                }
                page++;
            } while (found);
        }

        public static dynamic GetData(string url, NameValueCollection values = null)
        {
            return new Uri(url).GetDynamicJsonObject(values);
        }

        private static async Task<dynamic> GetDataDynamic(Uri uri)
        {
            using (var client = new HttpClient())
            {
                var content = await client.GetStringAsync(uri);
                return await Task.Run(() => JsonObject.GetDynamicJsonObject(content));
            }
        }
    }
}

namespace JsonUtils
{
    public class JsonObject : DynamicObject, IEnumerable, IEnumerator
    {
        object _object;

        JsonObject(object jObject)
        {
            _object = jObject;
        }

        public static dynamic GetDynamicJsonObject(byte[] buf)
        {
            return GetDynamicJsonObject(buf, Encoding.UTF8);
        }

        public static dynamic GetDynamicJsonObject(byte[] buf, Encoding encoding)
        {
            return GetDynamicJsonObject(encoding.GetString(buf));
        }

        public static dynamic GetDynamicJsonObject(string json)
        {
            object o = JsonConvert.DeserializeObject(json);
            return new JsonObject(o);
        }

        internal static dynamic GetDynamicJsonObject(JObject jObj)
        {
            return new JsonUtils.JsonObject(jObj);
        }

        public object this[string s]
        {
            get
            {
                JObject jObject = _object as JObject;
                object obj = jObject.SelectToken(s);
                if (obj == null) return true;

                if (obj is JValue)
                    return GetValue(obj);
                else
                    return new JsonObject(obj);
            }
        }

        public object this[int i]
        {
            get
            {
                if (!(_object is JArray)) return null;

                object obj = (_object as JArray)[i];
                if (obj is JValue)
                {
                    return GetValue(obj);
                }
                return new JsonObject(obj);
            }
        }

        public override bool TryGetMember(GetMemberBinder binder, out object result)
        {
            result = null;

            if (_object is JArray)
            {
                JArray jArray = _object as JArray;
                switch (binder.Name)
                {
                    case "Length":
                    case "Count": result = jArray.Count; break;
                    case "ToList": result = (Func<List<string>>)(() => jArray.Values().Select(x => x.ToString()).ToList()); break;
                    case "ToArray": result = (Func<string[]>)(() => jArray.Values().Select(x => x.ToString()).ToArray()); break;
                }

                return true;
            }

            JObject jObject = _object as JObject;
            object obj = jObject.SelectToken(binder.Name);
            if (obj == null) return true;

            if (obj is JValue)
                result = GetValue(obj);
            else
                result = new JsonObject(obj);

            return true;
        }

        object GetValue(object obj)
        {
            string val = ((JValue)obj).ToString();

            if (int.TryParse(val, out int resInt)) return resInt;
            if (DateTime.TryParse(val, out DateTime resDateTime)) return resDateTime;
            if (double.TryParse(val, out double resDouble)) return resDouble;

            return val;
        }

        public override string ToString()
        {
            return _object.ToString();
        }

        int _index = -1;

        public IEnumerator GetEnumerator()
        {
            _index = -1;
            return this;
        }

        public object Current
        {
            get
            {
                if (!(_object is JArray)) return null;
                object obj = (_object as JArray)[_index];
                if (obj is JValue) return GetValue(obj);
                return new JsonObject(obj);
            }
        }

        public bool MoveNext()
        {
            if (!(_object is JArray)) return false;
            _index++;
            return _index < (_object as JArray).Count;
        }

        public void Reset()
        {
            throw new NotImplementedException();
        }
    }

    public class XmlObject
    {
        public static dynamic GetDynamicJsonObject(string xmlString)
        {
            var xmlDoc = XDocument.Load(new StringReader(xmlString));
            return JsonObject.GetDynamicJsonObject(XmlToJObject(xmlDoc.Root));
        }

        public static dynamic GetDynamicJsonObject(Stream xmlStream)
        {
            var xmlDoc = XDocument.Load(xmlStream);
            return JsonObject.GetDynamicJsonObject(XmlToJObject(xmlDoc.Root));
        }

        static JObject XmlToJObject(XElement node)
        {
            JObject jObj = new JObject();
            foreach (var attr in node.Attributes())
            {
                jObj.Add(attr.Name.LocalName, attr.Value);
            }

            foreach (var childs in node.Elements().GroupBy(x => x.Name.LocalName))
            {
                string name = childs.ElementAt(0).Name.LocalName;
                if (childs.Count() > 1)
                {
                    JArray jArray = new JArray();
                    foreach (var child in childs)
                    {
                        jArray.Add(XmlToJObject(child));
                    }
                    jObj.Add(name, jArray);
                }
                else
                {
                    jObj.Add(name, XmlToJObject(childs.ElementAt(0)));
                }
            }

            node.Elements().Remove();
            if (!string.IsNullOrEmpty(node.Value))
            {
                string name = "Value";
                while (jObj[name] != null) name = "_" + name;
                jObj.Add(name, node.Value);
            }

            return jObj;
        }
    }
}

namespace System
{
    public static class JsonExtensions
    {
        public static JsonObject GetDynamicJsonObject(this Uri uri, NameValueCollection values = null)
        {
            using (Net.WebClient wc = new Net.WebClient())
            {
                wc.Encoding = Encoding.UTF8;
                wc.Headers["User-Agent"] = "Mozilla/4.0 (compatible; MSIE 7.0; Windows NT 5.1; .NET CLR 2.0.50727; .NET4.0C; .NET4.0E)";

                if (values == null)  // GET
                {
                    return JsonUtils.JsonObject.GetDynamicJsonObject(wc.DownloadString(uri));
                }
                else  // POST
                {
                    wc.Headers[Net.HttpRequestHeader.ContentType] = "application/x-www-form-urlencoded";
                    return JsonUtils.JsonObject.GetDynamicJsonObject(wc.UploadValues(uri, values));
                }
            }
        }
    }
}