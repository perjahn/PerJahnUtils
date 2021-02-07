using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace sortini
{
    class Program
    {
        static int Main(string[] args)
        {
            if (args.Length != 2)
            {
                Console.WriteLine("Usage: sortini <infile> <outfile>");
                return 1;
            }

            string infile = args[0];
            string outfile = args[1];

            if (!File.Exists(infile))
            {
                Console.WriteLine($"File not found: '{infile}'");
                return 1;
            }

            string[] rows = File.ReadAllLines(infile).Where(l => l != string.Empty).ToArray();

            int sectionstart = -1;
            for (int row = 0; row < rows.Length; row++)
            {
                if (rows[row].Trim().StartsWith('['))
                {
                    if (row - sectionstart > 1)
                    {
                        Array.Sort(rows, sectionstart + 1, row - sectionstart - 1);
                        sectionstart = row;
                    }
                }
            }

            if (rows.Length - sectionstart > 1)
            {
                Array.Sort(rows, sectionstart + 1, rows.Length - sectionstart - 1);
            }

            rows = FormatRows(rows);

            foreach (var r in rows)
            {
                Console.WriteLine(r);
            }

            File.WriteAllLines(outfile, rows);

            return 0;
        }

        static string[] FormatRows(string[] rows)
        {
            var newRows = rows.ToList();
            for (int row = 0; row < newRows.Count; row++)
            {
                if (row != 0 && newRows[row].StartsWith('['))
                {
                    newRows.Insert(row, string.Empty);
                    row++;
                }
            }

            return newRows.ToArray();
        }
    }
}
