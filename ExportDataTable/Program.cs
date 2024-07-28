using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Threading;
using MySql.Data.MySqlClient;

namespace ExportDataTable
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
            var watch = Stopwatch.StartNew();

            using Db mydb = new(connstr);

            for (var row = 0; ;)
            {
                if (row % batchsize != 0)
                {
                    Console.WriteLine(string.Empty + (int)watch.Elapsed.TotalSeconds + $": Got {row} rows.");
                    return;
                }

                var sql = $"select * from `{tablename}` order by `{sortcol}` limit {row},{batchsize}";

                List<string> rows = [];

                using MySqlDataReader reader = mydb.ExecuteReaderSQL(sql);

                if (!reader.HasRows)
                {
                    Console.WriteLine(string.Empty + (int)watch.Elapsed.TotalSeconds + $": Got {row} rows.");
                    return;
                }

                Console.WriteLine(string.Empty + (int)watch.Elapsed.TotalSeconds + ": Got rows.");

                while (reader.Read())
                {
                    StringBuilder sb = new();

                    for (var col = 0; col < reader.FieldCount; col++)
                    {
                        if (reader.IsDBNull(col))
                        {
                            Console.WriteLine(string.Empty + (int)watch.Elapsed.TotalSeconds + $": row {row}, column {col} is null");
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

                File.AppendAllLines(filename, rows);
                Console.WriteLine(string.Empty + (int)watch.Elapsed.TotalSeconds + $": Wrote {row} rows.");

                Thread.Sleep(1000);
            }
        }
    }
}
