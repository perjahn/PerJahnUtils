using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MySql.Data.MySqlClient;

namespace ExportDateTable
{
    class Program
    {
        static int Main(string[] args)
        {
            if (args.Length != 5)
            {
                Console.WriteLine("Usage: ExportDataTable <connstr> <tablename> <filename> <sortcol> <batchsize>");
                return 1;
            }

            Export(args[0], args[1], args[2], args[3], int.Parse(args[4]));

            return 0;
        }

        static void Export(string connstr, string tablename, string filename, string sortcol, int batchsize)
        {
            Stopwatch sw = Stopwatch.StartNew();

            using (var mydb = new Db(connstr))
            {
                for (int row = 0; ;)
                {
                    if (row % batchsize != 0)
                    {
                        Console.WriteLine("" + (int)sw.Elapsed.TotalSeconds + $": Got {row} rows.");
                        return;
                    }

                    string sql = $"select * from `{tablename}` order by `{sortcol}` limit {row},{batchsize}";

                    List<string> rows = new List<string>();

                    using (MySqlDataReader reader = mydb.ExecuteReaderSQL(sql))
                    {
                        if (!reader.HasRows)
                        {
                            Console.WriteLine("" + (int)sw.Elapsed.TotalSeconds + $": Got {row} rows.");
                            return;
                        }

                        Console.WriteLine("" + (int)sw.Elapsed.TotalSeconds + ": Got rows.");

                        while (reader.Read())
                        {
                            StringBuilder sb = new StringBuilder();

                            for (int col = 0; col < reader.FieldCount; col++)
                            {
                                if (reader.IsDBNull(col))
                                {
                                    Console.WriteLine("" + (int)sw.Elapsed.TotalSeconds + $": row {row}, column {col} is null");
                                    if (col != 0)
                                    {
                                        sb.Append('\t');
                                    }
                                }
                                else
                                {
                                    if (col == 0)
                                    {
                                        sb.Append(reader[col]);
                                    }
                                    else
                                    {
                                        sb.Append('\t');
                                        sb.Append(reader[col]);
                                    }
                                }
                            }

                            rows.Add(sb.ToString());

                            row++;
                        }
                    }

                    File.AppendAllLines(filename, rows);
                    Console.WriteLine("" + (int)sw.Elapsed.TotalSeconds + $": Wrote {row} rows.");

                    System.Threading.Thread.Sleep(1000);
                }
            }
        }
    }
}
