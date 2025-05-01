using System;

namespace CreatePublish
{
    class ConsoleHelper
    {
        public static bool Verboselogging { get; set; }

        private static string Deferredline;
        private static int Deferredcoloroffset;
        private static int Deferredcolorlength;
        private static ConsoleColor Deferredcolor;

        public static void WriteLineDeferred(string s)
        {
            if (s == null)
            {
                Deferredline = null;
                return;
            }

            var pos1 = s.LastIndexOf('\\') + 1;
            var pos2 = s.LastIndexOf('.');
            if (pos1 == -1 || pos2 == -1 || pos1 > pos2)
            {
                WriteLineDeferredColor(s, 0, 0, ConsoleColor.Green);
            }
            else
            {
                WriteLineDeferredColor(s, pos1, pos2 - pos1, ConsoleColor.White);
            }
        }

        private static void WriteLineDeferredColor(string s, int coloroffset, int colorlength, ConsoleColor color)
        {
            Deferredline = s;
            Deferredcoloroffset = coloroffset;
            Deferredcolorlength = colorlength;
            Deferredcolor = color;
        }

        public static void WriteLine(string s, bool verbose)
        {
            if (verbose && !Verboselogging)
            {
                return;
            }

            if (Deferredline != null)
            {
                ColorWriteDeferred();
            }

            Console.WriteLine(s);
        }

        public static void ColorWrite(ConsoleColor color, string s)
        {
            if (Deferredline != null)
            {
                ColorWriteDeferred();
            }

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

        private static void ColorWriteDeferred()
        {
            var oldColor = Console.ForegroundColor;
            try
            {
                var s1 = Deferredline[..Deferredcoloroffset];
                var s2 = Deferredline.Substring(Deferredcoloroffset, Deferredcolorlength);
                var s3 = Deferredline[(Deferredcoloroffset + Deferredcolorlength)..];

                Console.Write(s1);
                Console.ForegroundColor = Deferredcolor;
                Console.Write(s2);
                Console.ForegroundColor = oldColor;
                Console.WriteLine(s3);
            }
            finally
            {
                Deferredline = null;
                Console.ForegroundColor = oldColor;
            }
        }
    }
}
