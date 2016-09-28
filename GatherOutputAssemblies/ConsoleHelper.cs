using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace GatherOutputAssemblies
{
    class ConsoleHelper
    {
        public static bool loglevel { get; set; }

        public static void ColorWrite(ConsoleColor color, string s)
        {
            ConsoleColor oldColor = Console.ForegroundColor;
            try
            {
                Console.ForegroundColor = color;
                Console.Write(s);
            }
            finally
            {
                Console.ForegroundColor = oldColor;
            }
        }

        public static void ColorWriteLine(ConsoleColor color, string s)
        {
            ConsoleColor oldColor = Console.ForegroundColor;
            try
            {
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
