using System;

namespace GatherOutputAssemblies
{
    class ConsoleHelper
    {
        public static bool Loglevel { get; set; }

        public static void ColorWrite(ConsoleColor color, string s)
        {
            var oldColor = Console.ForegroundColor;
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
            var oldColor = Console.ForegroundColor;
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
