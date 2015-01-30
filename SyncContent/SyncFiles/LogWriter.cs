using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SyncFiles
{
	class LogWriter
	{
		public static bool verbose { get; set; }
		public static string logfile { get; set; }

		public static void WriteLine(string message, bool verbose = false)
		{
			if (LogWriter.verbose || !verbose)
			{
				Console.WriteLine(message);

				using (StreamWriter sw = new StreamWriter(logfile, true))
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
