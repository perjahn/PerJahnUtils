using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace CheckMissingFiles
{
    class ConsoleHelper
    {
        public static string DeferredLine { get; set; }
        public static bool HasWritten { get; set; }

        public static void WriteLine(string s)
        {
            HasWritten = true;

            if (DeferredLine != null)
            {
                Console.WriteLine(DeferredLine);
                DeferredLine = null;
            }

            Console.WriteLine(s);
        }

        public static void WriteLineColor(string s, ConsoleColor color)
        {
            ConsoleColor oldColor = Console.ForegroundColor;

            try
            {
                HasWritten = true;

                if (DeferredLine != null)
                {
                    Console.ForegroundColor = ConsoleColor.White;
                    Console.WriteLine(DeferredLine);
                    DeferredLine = null;
                }

                Console.ForegroundColor = color;
                Console.WriteLine(s);
            }
            finally
            {
                Console.ForegroundColor = oldColor;
            }
        }
    }
}
