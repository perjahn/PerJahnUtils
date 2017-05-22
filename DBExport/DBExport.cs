using System;
using System.Linq;

namespace DBExport
{
    class Program
    {
        static int Main(string[] args)
        {
            if (args.Length != 4)
            {
                Console.WriteLine("Usage: DBExport <dbprovider> <connstr> <tablename> <filename>");
                return 1;
            }

            string dbprovider = args[0];
            string connstr = args[1];
            string tablename = args[2];
            string filename = args[3];

            if (tablename.ToArray().Any(c => !char.IsLetterOrDigit(c)))
            {
                Console.WriteLine($"Invalid table name: '{tablename}'");
                return 1;
            }

            DumpData dumper = new DumpData();
            dumper.DumpTable(dbprovider, connstr, tablename, filename);

            return 0;
        }
    }
}
