using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace RemoveMissingFiles
{
    class ConsoleHelper
    {
        public static string deferredLine { get; set; }

        public static void WriteLineColor(string s, ConsoleColor color)
        {
            ConsoleColor oldColor = Console.ForegroundColor;

            try
            {
                if (deferredLine != null)
                {
                    Console.ForegroundColor = ConsoleColor.White;
                    Console.WriteLine(deferredLine);
                    deferredLine = null;
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
