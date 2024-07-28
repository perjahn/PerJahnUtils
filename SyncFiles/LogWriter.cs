using System;
using System.IO;

namespace SyncFiles
{
    class LogWriter
    {
        public static bool Verbose { get; set; }
        public static string Logfile { get; set; } = string.Empty;

        public static void WriteLine(string message, bool verbose = false)
        {
            if (Verbose || !verbose)
            {
                Console.WriteLine(message);
                if (Logfile != string.Empty)
                {
                    using StreamWriter sw = new(Logfile, true);
                    sw.WriteLine(message);
                }
            }
        }

        public static void WriteConsoleColor(string message, ConsoleColor color)
        {
            var c = Console.ForegroundColor;
            Console.ForegroundColor = color;
            Console.WriteLine(message);
            Console.ForegroundColor = c;
        }
    }
}
