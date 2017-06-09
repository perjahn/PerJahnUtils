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
        public JObject[] DumpTable(string dbprovider, string connstr, string sql, string[] toupperfields, string[] tolowerfields)
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
                                rowdata.Append($"  \"{colname}\": \"{(data == null ? "null" : data.Value.ToString("s"))}\"");
                            }
                            else if (reader.GetFieldType(i) == typeof(short))
                            {
                                short? data = reader.GetValue(i) as short?;
                                rowdata.Append($"  \"{colname}\": {(data == null ? "\"null\"" : data.Value.ToString())}");
                            }
                            else if (reader.GetFieldType(i) == typeof(int))
                            {
                                int? data = reader.GetValue(i) as int?;
                                rowdata.Append($"  \"{colname}\": {(data == null ? "\"null\"" : data.Value.ToString())}");
                            }
                            else if (reader.GetFieldType(i) == typeof(long))
                            {
                                long? data = reader.GetValue(i) as long?;
                                rowdata.Append($"  \"{colname}\": {(data == null ? "\"null\"" : data.Value.ToString())}");
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
                                    if (toupperfields.Contains(colname))
                                    {
                                        data = data.ToUpper();
                                    }
                                    if (tolowerfields.Contains(colname))
                                    {
                                        data = data.ToLower();
                                    }

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
