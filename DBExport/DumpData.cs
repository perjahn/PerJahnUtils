using Microsoft.Data.SqlClient;
using System;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;

namespace DBExport
{
    class DumpData
    {
        public static async Task Export(string connstr, string folder)
        {
            var watch = Stopwatch.StartNew();

            if (!Directory.Exists(folder))
            {
                Console.WriteLine($"Creating folder: '{folder}'");
                Directory.CreateDirectory(folder);
            }

            var sql = "select name from sys.tables order by name";

            var connection = new SqlConnection(connstr);
            connection.Open();

            using var cmd = connection.CreateCommand();

            cmd.Connection = connection;
            cmd.CommandText = sql;

            long rows = 0;
            long tables = 0;

            var reader = await cmd.ExecuteReaderAsync();
            while (reader.Read())
            {
                string tablename = reader.GetString(0);
                string filename = Path.Combine(folder, $"{tablename}.txt");

                rows += await DumpTable(connstr, tablename, filename);
                //rows += await Task.Run(() => DumpTable(connstr, tablename, filename));
                tables++;
            }

            reader.Close();
            connection.Close();

            Console.WriteLine($"Tables: {tables}");
            Console.WriteLine($"Total rows: {rows}");
            Console.WriteLine($"Total time: {watch.Elapsed}");
        }

        public static async Task<long> DumpTable(string connstr, string tablename, string filename)
        {
            var watch = Stopwatch.StartNew();

            Console.WriteLine($"Exporting: '{tablename}' -> '{filename}'");

            var connection = new SqlConnection(connstr);
            connection.Open();

            var sql = $"select * from [{tablename}]";

            using var cmd = connection.CreateCommand();

            cmd.Connection = connection;
            cmd.CommandText = sql;

            using var reader = await cmd.ExecuteReaderAsync();
            using var writer = new StreamWriter(filename);

            long row = 0;

            while (reader.Read())
            {
                if (row == 0)
                {
                    for (int i = 0; i < reader.FieldCount; i++)
                    {
                        if (i > 0)
                        {
                            writer.Write('\t');
                        }
                        writer.Write(reader.GetName(i));
                    }
                }

                for (int i = 0; i < reader.FieldCount; i++)
                {
                    if (i > 0)
                    {
                        writer.Write('\t');
                    }

                    if (reader.GetFieldType(i) == typeof(System.Byte[]))
                    {
                        if (row == 0)
                        {
                            Console.WriteLine($"Excluding column: '{reader.GetName(i)}' '{reader.GetDataTypeName(i)}'");
                        }
                    }
                    else if (reader.GetFieldType(i) == typeof(DateTimeOffset))
                    {
                        DateTimeOffset? date = reader.GetValue(i) as DateTimeOffset?;
                        if (date != null)
                        {
                            writer.Write(date.Value.ToString("s"));
                        }
                    }
                    else
                    {
                        writer.Write(reader.GetValue(i));
                    }
                }
                writer.WriteLine();

                row++;
                if (row % 1000000 == 0)
                {
                    Console.WriteLine($"Row: {row}...");
                }
            }

            Console.WriteLine($"{tablename}: {row}");

            writer.Close();
            reader.Close();
            connection.Close();

            Console.WriteLine($"Time: {watch.Elapsed}");

            return row;
        }
    }
}
