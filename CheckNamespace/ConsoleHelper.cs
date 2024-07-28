using System;

namespace CheckNamespace
{
    class ConsoleHelper
    {
        public static void WriteColor(string s, ConsoleColor color)
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

        public static void WriteLineColor(string s, ConsoleColor color)
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
