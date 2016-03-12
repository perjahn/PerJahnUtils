using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace VerifyReferences
{
    class ConsoleHelper
    {
        public static void WriteLineColor(string message, ConsoleColor color)
        {
            ConsoleColor oldColor = Console.ForegroundColor;
            try
            {
                Console.ForegroundColor = color;
                Console.WriteLine(message);
            }
            finally
            {
                Console.ForegroundColor = oldColor;
            }
        }

        public static void WriteColor(string message, ConsoleColor color)
        {
            ConsoleColor oldColor = Console.ForegroundColor;
            try
            {
                Console.ForegroundColor = color;
                Console.Write(message);
            }
            finally
            {
                Console.ForegroundColor = oldColor;
            }
        }
    }
}
