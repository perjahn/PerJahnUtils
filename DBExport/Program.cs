using System;
using System.Threading.Tasks;

namespace DBExport
{
    class Program
    {
        static async Task<int> Main(string[] args)
        {
            if (args.Length != 2)
            {
                Console.WriteLine("Usage: <ConnectionString> <folder>");
                return 1;
            }

            var connstr = args[0];
            var folder = args[1];

            await DumpData.Export(connstr, folder);

            return 0;
        }
    }
}
