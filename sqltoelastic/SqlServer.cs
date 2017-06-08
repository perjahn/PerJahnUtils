using System;
using System.IO;
using System.Linq;
using System.Data.Common;
using System.Text;
using Newtonsoft.Json.Linq;
using System.Collections.Generic;

namespace sqltoelastic
{
    class SqlServer
    {
        public JObject[] DumpTable(string dbprovider, string connstr, string sql)
        {
            List<JObject> jsonrows = new List<JObject>();

            using (db mydb = new db(dbprovider, connstr))
            {
                List<string> columns = new List<string>();

                using (DbDataReader reader = mydb.ExecuteReaderSQL(sql))
                {
                    for (int i = 0; i < reader.FieldCount; i++)
                    {
                        columns.Add(reader.GetName(i));
                    }

                    while (reader.Read())
                    {
                        StringBuilder rowdata = new StringBuilder();

                        rowdata.AppendLine("{");

                        for (int i = 0; i < reader.FieldCount; i++)
                        {
                            string colname = columns[i];

                            if (reader.IsDBNull(i))
                            {
                                continue;
                            }

                            if (reader.GetFieldType(i) == typeof(DateTimeOffset))
                            {
                                DateTimeOffset? data = reader.GetValue(i) as DateTimeOffset?;
                                if (data == null)
                                {
                                    rowdata.Append($"  \"{colname}\": null");
                                }
                                else
                                {
                                    rowdata.Append($"  \"{colname}\": \"{data.Value.ToString("s")}\"");
                                }
                            }
                            else
                            {
                                string data = reader.GetValue(i) as string;
                                if (data == null)
                                {
                                    rowdata.Append($"  \"{colname}\": null");
                                }
                                else
                                {
                                    bool parsablejson;
                                    try
                                    {
                                        JObject jsondata = JObject.Parse(data);
                                        parsablejson = true;
                                    }
                                    catch (Newtonsoft.Json.JsonReaderException)
                                    {
                                        // Parse. Or Parse not. There is no TryParse. :(
                                        parsablejson = false;
                                    }

                                    if (parsablejson)
                                    {
                                        rowdata.Append($"  \"{colname}\": {data}");
                                    }
                                    else
                                    {
                                        rowdata.Append($"  \"{colname}\": \"{data}\"");
                                    }
                                }
                            }

                            rowdata.AppendLine(i < reader.FieldCount - 1 ? "," : string.Empty);
                        }

                        rowdata.AppendLine("}");

                        JObject jsonrow = JObject.Parse(rowdata.ToString());
                        jsonrows.Add(jsonrow);
                    }

                    reader.Close();
                }
            }

            return jsonrows.ToArray();
        }
    }
}
