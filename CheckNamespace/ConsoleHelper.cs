using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace CheckNamespace
{
	class ConsoleHelper
	{
		public static void WriteColor(string s, ConsoleColor color)
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

		public static void WriteLineColor(string s, ConsoleColor color)
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
