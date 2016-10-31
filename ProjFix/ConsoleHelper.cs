using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ProjFix
{
    class ConsoleHelper
    {
        public static bool verboselogging { get; set; }

        private static string _deferredline = null;
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

            int pos1 = s.LastIndexOf('\\') + 1;
            int pos2 = s.LastIndexOf('.');
            if (pos1 == -1 || pos2 == -1 || pos1 > pos2)
                WriteLineDeferredColor(s, 0, 0, ConsoleColor.Green);
            else
                WriteLineDeferredColor(s, pos1, pos2 - pos1, ConsoleColor.White);
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
            if (verbose && !verboselogging)
            {
                return;
            }

            if (_deferredline != null)
            {
                ColorWriteDeferred(_deferredcolor, _deferredline, _deferredcoloroffset, _deferredcolorlength);
            }

            Console.WriteLine(s);
        }

        public static void ColorWrite(ConsoleColor color, string s)
        {
            if (_deferredline != null)
            {
                ColorWriteDeferred(_deferredcolor, _deferredline, _deferredcoloroffset, _deferredcolorlength);
            }

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

        private static void ColorWriteDeferred(ConsoleColor color, string s, int coloroffset, int colorlength)
        {
            ConsoleColor oldColor = Console.ForegroundColor;
            try
            {
                string s1 = _deferredline.Substring(0, _deferredcoloroffset);
                string s2 = _deferredline.Substring(_deferredcoloroffset, _deferredcolorlength);
                string s3 = _deferredline.Substring(_deferredcoloroffset + _deferredcolorlength);

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
