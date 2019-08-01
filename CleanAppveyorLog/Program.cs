using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;

namespace CleanAppveyorLog
{
    class Program
    {
        static void Main(string[] args)
        {
            if (args.Length != 2)
            {
                Console.WriteLine("Usage: CleanAppveyorLog <infile> <outfile>");
                return;
            }

            string infile = args[0];
            string outfile = args[1];

            CleanFile(infile, outfile);
        }

        static void CleanFile(string infile, string outfile)
        {
            string[] rows = File.ReadAllLines(infile);

            for (int i = 0; i < rows.Length; i++)
            {
                string row = rows[i];

                // <1ms
                // <11ms
                // <111ms
                // < 1ms
                // < 11ms
                // < 111ms
                // 1ms
                // 11ms
                // 111ms
                // [1ms]
                // [11ms]
                // [111ms]
                // [<1ms]
                // [<11ms]
                // [<111ms]
                // [< 1ms]
                // [< 11ms]
                // [< 111ms]
                // [<1s 111ms]
                // [<11s 111ms]
                // [< 111s 111ms]
                var m = Regex.Match(row, @"\s+\[?<?(\s*\ds)*\s*\d+ms\]?$");
                if (m.Index > 0)
                {
                    rows[i] = row.Substring(0, m.Index);
                }
            }

            File.WriteAllLines(outfile, rows);
        }
    }
}
