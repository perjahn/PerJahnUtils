using System;

namespace ProjFix
{
    class ConsoleHelper
    {
        public static bool Verboselogging { get; set; }

        private static string _deferredline;
        private static int _deferredcoloroffset;
        private static int _deferredcolorlength;
        private static ConsoleColor _deferredcolor;

        public static void WriteLineDeferred(string s)
        {
            if (s == null)
            {
                _deferredline = null;
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
            _deferredline = s;
            _deferredcoloroffset = coloroffset;
            _deferredcolorlength = colorlength;
            _deferredcolor = color;
        }

        public static void WriteLine(string s, bool verbose)
        {
            if (verbose && !Verboselogging)
            {
                return;
            }

            if (_deferredline != null)
            {
                ColorWriteDeferred();
            }

            Console.WriteLine(s);
        }

        public static void ColorWrite(ConsoleColor color, string s)
        {
            if (_deferredline != null)
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
                var s1 = _deferredline[.._deferredcoloroffset];
                var s2 = _deferredline.Substring(_deferredcoloroffset, _deferredcolorlength);
                var s3 = _deferredline[(_deferredcoloroffset + _deferredcolorlength)..];

                Console.Write(s1);
                Console.ForegroundColor = _deferredcolor;
                Console.Write(s2);
                Console.ForegroundColor = oldColor;
                Console.WriteLine(s3);
            }
            finally
            {
                _deferredline = null;
                Console.ForegroundColor = oldColor;
            }
        }
    }
}
