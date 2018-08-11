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
        public static StreamWriter _logfile;

        public JObject[] DumpTable(string dbprovider, string connstr, string sql, string[] toupperfields, string[] tolowerfields, string addconstantfield, string[] escapefields)
        {
            string addfieldname = null;
            string addfieldvalue = null;
            if (!string.IsNullOrEmpty(addconstantfield) && addconstantfield.Contains('='))
            {
                addfieldname = addconstantfield.Split('=')[0];
                addfieldvalue = addconstantfield.Split('=')[1];
            }

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
                            bool lastcol = i == reader.FieldCount - 1 && string.IsNullOrEmpty(addconstantfield);

                            if (reader.IsDBNull(i))
                            {
                                continue;
                            }

                            if (reader.GetFieldType(i) == typeof(DateTimeOffset))
                            {
                                DateTimeOffset? data = reader.GetValue(i) as DateTimeOffset?;
                                rowdata.Append($"  \"{colname}\": \"{(data == null ? "null" : data.Value.ToString("s"))}\"");
                            }
                            else if (reader.GetFieldType(i) == typeof(DateTime))
                            {
                                DateTime? data = reader.GetValue(i) as DateTime?;
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
                                if (!(reader.GetValue(i) is string data))
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
                                    if (escapefields.Contains(colname))
                                    {
                                        data = data.Replace(@"\", @"\\").Replace("\"", "\\\"");
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

                            rowdata.AppendLine(lastcol ? string.Empty : ",");
                        }

                        if (addfieldname != null && addfieldvalue != null)
                        {
                            rowdata.AppendLine($"  \"{addfieldname}\": \"{addfieldvalue}\"");
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

        private void Log(string message)
        {
            string date = DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss");
            _logfile.WriteLine($"{date}: {message}");
            _logfile.Flush();
        }
    }
}
