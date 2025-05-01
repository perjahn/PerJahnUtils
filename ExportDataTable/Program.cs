using System;
using System.Collections.Generic;
using System.Data;
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
            using MySqlConnection cn = new(connstr);
            cn.Open();

            Export2(cn, tablename, filename, sortcol, batchsize);

            cn.Close();
        }

        static void Export2(MySqlConnection cn, string tablename, string filename, string sortcol, int batchsize)
        {
            var watch = Stopwatch.StartNew();

            for (var row = 0; ;)
            {
                if (row % batchsize != 0)
                {
                    Console.WriteLine($"{(int)watch.Elapsed.TotalSeconds}: Got {row} rows.");
                    return;
                }

                var sql = "select * from @tablename order by @sortcol limit @row,@batchsize";

                List<string> rows = [];

                using var cmd = cn.CreateCommand();

                cmd.Connection = cn;
                cmd.CommandType = CommandType.Text;
                cmd.CommandText = sql;
                cmd.CommandTimeout = 600;

                var pTableName = cmd.CreateParameter();
                pTableName.ParameterName = "tablename";
                pTableName.Value = tablename;
                _ = cmd.Parameters.Add(pTableName);

                var pSortCol = cmd.CreateParameter();
                pSortCol.ParameterName = "sortcol";
                pSortCol.Value = sortcol;
                _ = cmd.Parameters.Add(pSortCol);

                var pRow = cmd.CreateParameter();
                pRow.ParameterName = "row";
                pRow.Value = row;
                _ = cmd.Parameters.Add(pRow);

                var pBatchSize = cmd.CreateParameter();
                pBatchSize.ParameterName = "batchsize";
                pBatchSize.Value = batchsize;
                _ = cmd.Parameters.Add(pBatchSize);

                using MySqlDataReader reader = cmd.ExecuteReader();

                if (!reader.HasRows)
                {
                    Console.WriteLine($"{(int)watch.Elapsed.TotalSeconds}: Got {row} rows.");
                    return;
                }

                Console.WriteLine($"{(int)watch.Elapsed.TotalSeconds}: Got rows.");

                while (reader.Read())
                {
                    StringBuilder sb = new();

                    for (var col = 0; col < reader.FieldCount; col++)
                    {
                        if (reader.IsDBNull(col))
                        {
                            Console.WriteLine($"{(int)watch.Elapsed.TotalSeconds}: row {row}, column {col} is null");
                            if (col != 0)
                            {
                                _ = sb.Append('\t');
                            }
                        }
                        else
                        {
                            if (col == 0)
                            {
                                _ = sb.Append(reader[col]);
                            }
                            else
                            {
                                _ = sb.Append('\t');
                                _ = sb.Append(reader[col]);
                            }
                        }
                    }

                    rows.Add(sb.ToString());

                    row++;
                }

                File.AppendAllLines(filename, rows);
                Console.WriteLine($"{(int)watch.Elapsed.TotalSeconds}: Wrote {row} rows.");

                Thread.Sleep(1000);
            }
        }
    }
}
