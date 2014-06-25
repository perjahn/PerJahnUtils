using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace RemoveMissingFiles
{
	class ConsoleHelper
	{
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
