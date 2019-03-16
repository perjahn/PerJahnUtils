using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace SyncFiles
{
    class LogWriter
    {
        public static bool Verbose { get; set; }
        public static string Logfile { get; set; }

        public static void WriteLine(string message, bool verbose = false)
        {
            if (LogWriter.Verbose || !verbose)
            {
                Console.WriteLine(message);

                using (var sw = new StreamWriter(Logfile, true))
                {
                    sw.WriteLine(message);
                }
            }
        }

        public static void WriteConsoleColor(string message, ConsoleColor color)
        {
            ConsoleColor c = Console.ForegroundColor;
            Console.ForegroundColor = color;
            Console.WriteLine(message);
            Console.ForegroundColor = c;
        }
    }
}
