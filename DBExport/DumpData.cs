using System;
using System.IO;
using System.Linq;
using System.Data.Common;

namespace DBExport
{
    class DumpData
    {
        public void DumpTable(string dbprovider, string connstr, string tablename, string filename)
        {
            using (Db mydb = new Db(dbprovider, connstr))
            {
                string sql = $"select * from {tablename}";

                using (StreamWriter sw = new StreamWriter(filename))
                {
                    using (DbDataReader reader = mydb.ExecuteReaderSQL(sql))
                    {
                        while (reader.Read())
                        {
                            for (int i = 0; i < reader.FieldCount; i++)
                            {
                                //Console.WriteLine($"'{reader.GetDataTypeName(i)}'");
                            }

                            for (int i = 0; i < reader.FieldCount; i++)
                            {
                                if (i > 0)
                                {
                                    sw.Write('\t');
                                }

                                if (reader.GetFieldType(i) == typeof(DateTimeOffset))
                                {
                                    DateTimeOffset? date = reader.GetValue(i) as DateTimeOffset?;
                                    if (date != null)
                                    {
                                        sw.Write(date.Value.ToString("s"));
                                    }
                                }
                                else
                                {
                                    sw.Write(reader.GetValue(i));
                                }
                            }
                            sw.WriteLine();
                        }

                        reader.Close();
                    }
                }
            }
        }
    }
}
