using System;
using System.IO;

namespace CleanTCLog
{
    class Program
    {
        static void Main(string[] args)
        {
            if (args.Length != 2)
            {
                Console.WriteLine("Usage: CleanTCLog <infile> <outfile>");
                return;
            }

            var infile = args[0];
            var outfile = args[1];

            CleanFile(infile, outfile);
        }

        static void CleanFile(string infile, string outfile)
        {
            var rows = File.ReadAllLines(infile);

            for (var i = 0; i < rows.Length; i++)
            {
                var row = rows[i];

                if (row.Length > 9 && row[0] == '[' && row[9] == ']')
                {
                    rows[i] = row = row[10..];
                }

                // (1s)
                // (1m)
                // (11s)
                // (11m)
                // (1m:11s)
                // (11m:11s)
                var pos1 = row.LastIndexOf('(');
                var pos2 = row.LastIndexOf(')');
                if (pos1 > 0 && pos2 == row.Length - 1)
                {
                    var time = row.Substring(pos1 + 1, pos2 - pos1 - 1);
                    if (
                        (time.Length == 2 && char.IsDigit(time[0]) && time[1] == 's')
                        ||
                        (time.Length == 2 && char.IsDigit(time[0]) && time[1] == 'm')
                        ||
                        (time.Length == 3 && char.IsDigit(time[0]) && char.IsDigit(time[1]) && time[2] == 's')
                        ||
                        (time.Length == 3 && char.IsDigit(time[0]) && char.IsDigit(time[1]) && time[2] == 'm')
                        ||
                        (time.Length == 6 && char.IsDigit(time[0]) && time[1] == 'm' && time[2] == ':' && char.IsDigit(time[3]) && char.IsDigit(time[4]) && time[5] == 's')
                        ||
                        (time.Length == 7 && char.IsDigit(time[0]) && char.IsDigit(time[1]) && time[2] == 'm' && time[3] == ':' && char.IsDigit(time[3]) && char.IsDigit(time[4]) && time[5] == 's')
                        )
                    {
                        rows[i] = pos1 > 0 && row[pos1 - 1] == ' ' ? row[..(pos1 - 1)] : row[..pos1];
                    }
                    else
                    {
                        Console.WriteLine($">>>{row}<<<");
                    }
                }
            }

            File.WriteAllLines(outfile, rows);
        }
    }
}
