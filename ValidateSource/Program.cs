using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace ValidateSource
{
    class Program
    {
        #region RestoreConsoleColorOnBreak
        static ConsoleColor _color;
        delegate bool ConsoleEventDelegate(int eventType);
        static ConsoleEventDelegate handler;
        [DllImport("kernel32.dll", SetLastError = true)]
        static extern bool SetConsoleCtrlHandler(ConsoleEventDelegate callback, bool add);

        static bool ConsoleEventCallback(int eventType)
        {
            Console.ForegroundColor = _color;
            return false;
        }
        #endregion

        static long _rowsTrailing;
        static long _filesTrailing;
        static long _rowsTotalTrailing;
        static long _filesTotalTrailing;
        static long _charsTrailing;

        static long _rowsIndentation;
        static long _rowsTotalIndentation;
        static long _filesIndentation;
        static long _filesTotalIndentation;

        static bool _fixTrailing;
        static bool _fixIndentation;
        static int _indentationsize;
        static int _loglevel;


        static int Main(string[] args)
        {
            _color = Console.ForegroundColor;
            handler = new ConsoleEventDelegate(ConsoleEventCallback);
            SetConsoleCtrlHandler(handler, true);

            string[] filteredArgs = ParseArguments(args);
            if (filteredArgs == null)
            {
                return 2;
            }

            if (filteredArgs.Length < 1)
            {
                WriteUsage();
                return 2;
            }


            string[] excluderegexs = filteredArgs.Skip(1).ToArray();

            string path;
            string pattern;
            ParsePath(filteredArgs[0], out path, out pattern);

            return SearchForMisformattedFiles(path, pattern, excluderegexs);
        }

        static string[] ParseArguments(string[] args)
        {
            _fixTrailing = _fixIndentation = false;
            _indentationsize = 4;
            _loglevel = 3;

            if (args.Contains("-fix"))
            {
                _fixTrailing = _fixIndentation = true;
            }
            if (args.Contains("-fixTrailing"))
            {
                _fixTrailing = true;
            }
            if (args.Contains("-fixIndentation"))
            {
                _fixIndentation = true;
            }

            if (args.Any(a => a.StartsWith("-i")))
            {
                string value = args.Last(a => a.StartsWith("-i")).Substring(2);
                if (!int.TryParse(value, out _indentationsize))
                {
                    WriteLineColor("Couldn't parse indentation size: '" + value + "'", ConsoleColor.Red, 0);
                    return null;
                }
            }

            if (args.Any(a => a.StartsWith("-l")))
            {
                string value = args.Last(a => a.StartsWith("-l")).Substring(2);
                if (!int.TryParse(value, out _loglevel))
                {
                    WriteLineColor("Couldn't parse log level: '" + value + "'", ConsoleColor.Red, 0);
                    return null;
                }
            }

            string[] filteredArgs = args
                .Where(a => !(new string[] { "-fix", "-fixTrailing", "-fixIndentation" })
                    .Contains(a, StringComparer.OrdinalIgnoreCase))
                .Where(a => !(new string[] { "-i", "-l" })
                    .Any(aa => a.StartsWith(aa, StringComparison.OrdinalIgnoreCase)))
                .ToArray();

            return filteredArgs;
        }

        static void WriteUsage()
        {
            WriteLineLogLevel(
@"ValidateSource 1.0 - Validates and optionally reformats source code files.

Usage: ValidateSource [-fix] [-fixTrailing] [-fixIndentation] [-iX] [-lX] <path> [exclude regex 1] [exclude regex 2] ...

If no wildcards (? *) are specified in path, a folder is assumed:
Files that will be parsed: *.cpp, *.cs, *.ps1, *.psm1, *.sql
Files that will be excluded: *.Designer.cs, AssemblyInfo.cs

Options:
-fix:             Fix both trailing whitespaces and inconsistent indentation. Default false.
-fixTrailing:     Fix trailing whitespaces. Default false.
-fixIndentation:  Fix inconsistent indentation. Default false.
-iX:              Set indentation size in spaces, X. Default 4.
-lX:              Log level 0-3. 0=No output, 1=minimal, 2=only filenames, 3=verbose (default).

Arguments:
path:             Root folder.
exclude regex:    Any amount of regex matched against file path, used to exclude files.

Return values:
0:                No misformatted files found.
1:                At least one misformatted file found.
2:                Fatal error occured.

Example:          ValidateSource myfolder -fix -l2 \\notthisfolder\\", 0);
        }

        static void ParsePath(string inpath, out string outpath, out string pattern)
        {
            if (Path.GetFileName(inpath).Contains('?') || Path.GetFileName(inpath).Contains('*'))
            {
                outpath = Path.GetDirectoryName(inpath);
                if (outpath == string.Empty)
                {
                    outpath = ".";
                }
                pattern = Path.GetFileName(inpath);
            }
            else
            {
                outpath = inpath;
                pattern = null;
            }
        }

        static int SearchForMisformattedFiles(string path, string pattern, string[] excluderegexs)
        {
            string[] filesTrailing = GetTrailingFiles(path, pattern, excluderegexs);
            if (filesTrailing == null)
            {
                return 2;
            }
            string[] filesIndentation = GetIndentationFiles(path, pattern, excluderegexs);
            if (filesIndentation == null)
            {
                return 2;
            }

            _filesTrailing = filesTrailing.Length;
            _filesIndentation = filesIndentation.Length;

            WriteStats();

            if (_fixTrailing)
            {
                FixTrailing(filesTrailing);
            }

            if (_fixIndentation)
            {
                FixIndentation(filesIndentation);
            }

            if (_filesTrailing > 0 || _filesIndentation > 0)
            {
                return 1;
            }

            return 0;
        }

        static void WriteStats()
        {
            if (_rowsTrailing == 0)
            {
                WriteLineColor("No trailing whitespace found.",
                    ConsoleColor.Green, 1);
            }
            else
            {
                long percent = _filesTrailing * 100 / _filesTotalTrailing;
                string percentFiles = (percent == 0 && _filesTrailing > 0) ? "<1" : percent.ToString();

                percent = _rowsTrailing * 100 / _rowsTotalTrailing;
                string percentRows = (percent == 0 && _rowsTrailing > 0) ? "<1" : percent.ToString();

                WriteLineColor(
                    "Trailing whitespace found in " + _filesTrailing + " (" + percentFiles + "%) source files, on " +
                    _rowsTrailing + " (" + percentRows + "%) rows, " +
                    _charsTrailing + " characters.",
                    ConsoleColor.Red, 1);
            }

            if (_rowsIndentation == 0)
            {
                WriteLineColor("No inconsistent indentation found.",
                    ConsoleColor.Green, 1);
            }
            else
            {
                long percent = _filesIndentation * 100 / _filesTotalIndentation;
                string percentFiles = (percent == 0 && _filesIndentation > 0) ? "<1" : percent.ToString();

                percent = _rowsIndentation * 100 / _rowsTotalIndentation;
                string percentRows = (percent == 0 && _rowsIndentation > 0) ? "<1" : percent.ToString();

                WriteLineColor(
                    "Inconsistent indentation found in " + _filesIndentation + " (" + percentFiles + "%) source files, on " +
                    _rowsIndentation + " (" + percentRows + "%) rows.",
                    ConsoleColor.Red, 1);
            }
        }

        static string[] GetTrailingFiles(string path, string pattern, string[] excluderegexs)
        {
            WriteLineColor("-=-=- Searching for trailing whitespaces -=-=-", ConsoleColor.Magenta, 1);

            _rowsTotalTrailing = 0;
            _rowsTrailing = 0;
            _charsTrailing = 0;
            List<string> outfiles = new List<string>();

            string[] infiles;
            try
            {
                if (pattern == null)
                {
                    infiles = Directory.GetFiles(path, "*", SearchOption.AllDirectories);
                }
                else
                {
                    infiles = Directory.GetFiles(path, pattern, SearchOption.AllDirectories);
                }
            }
            catch (Exception ex) when (ex is ArgumentException || ex is DirectoryNotFoundException || ex is UnauthorizedAccessException)
            {
                WriteLineColor("Path: '" + path + "'" + Environment.NewLine +
                    ex.Message, ConsoleColor.Red, 0);
                return null;
            }
            _filesTotalTrailing = infiles.Length;


            infiles = infiles
                .Select(f => f.StartsWith(@".\") ? f.Substring(2) : f)
                .ToArray();
            if (pattern == null)
            {
                infiles = infiles
                    .Where(f => (new string[] { ".cpp", ".cs", ".ps1", ".psm1", ".sql" }).Contains(Path.GetExtension(f)))
                    .Where(f => !(new string[] { ".Designer.cs" }).Any(ff => f.EndsWith(ff)))
                    .Where(f => !(new string[] { "AssemblyInfo.cs" }).Contains(Path.GetFileName(f)))
                    .ToArray();
            }


            foreach (string excluderegex in excluderegexs)
            {
                try
                {
                    infiles = infiles
                        .Where(f => !Regex.IsMatch(f, excluderegex, RegexOptions.IgnoreCase))
                        .ToArray();
                }
                catch (ArgumentException ex)
                {
                    WriteLineColor("Invalid RegEx: " + ex.Message, ConsoleColor.Red, 0);
                    return null;
                }
            }

            foreach (string filename in infiles)
            {
                string[] rows = File.ReadAllLines(filename);
                _rowsTotalTrailing += rows.Length;
                bool first = true;

                for (int i = 0; i < rows.Length; i++)
                {
                    string row = rows[i];
                    if (row.EndsWith(" ") || row.EndsWith("\t"))
                    {
                        if (first)
                        {
                            WriteLogLevel("File: '", 2);
                            WriteColor(filename, ConsoleColor.Cyan, 2);
                            WriteLineLogLevel(_loglevel < 3 ? "'" : "':", 2);
                            outfiles.Add(filename);
                            first = false;
                        }

                        int offset;
                        for (offset = row.Length; offset > 0; offset--)
                        {
                            if (row[offset - 1] == ' ' || row[offset - 1] == '\t')
                            {
                                _charsTrailing++;
                            }
                            else
                            {
                                break;
                            }
                        }

                        WriteLogLevel("" + (i + 1) + ": " + row.Substring(0, offset), 3);
                        WriteLineColor(new string('_', row.Length - offset).ToString(), ConsoleColor.Yellow, 3);

                        _rowsTrailing++;
                    }
                }
            }

            return outfiles.ToArray();
        }

        static string[] GetIndentationFiles(string path, string pattern, string[] excluderegexs)
        {
            WriteLineColor("-=-=- Searching for inconsistent indentation -=-=-", ConsoleColor.Magenta, 1);

            _rowsTotalIndentation = 0;
            _rowsIndentation = 0;
            List<string> outfiles = new List<string>();

            string[] infiles;
            try
            {
                if (pattern == null)
                {
                    infiles = Directory.GetFiles(path, "*", SearchOption.AllDirectories);
                }
                else
                {
                    infiles = Directory.GetFiles(path, pattern, SearchOption.AllDirectories);
                }
            }
            catch (Exception ex) when (ex is ArgumentException || ex is DirectoryNotFoundException || ex is UnauthorizedAccessException)
            {
                WriteLineColor("Path: '" + path + "'" + Environment.NewLine +
                    ex.Message, ConsoleColor.Red, 0);
                return null;
            }
            _filesTotalIndentation = infiles.Length;


            infiles = infiles
                .Select(f => f.StartsWith(@".\") ? f.Substring(2) : f)
                .ToArray();
            if (pattern == null)
            {
                infiles = infiles
                    .Where(f => (new string[] { ".cpp", ".cs", ".ps1", ".psm1", ".sql" }).Contains(Path.GetExtension(f)))
                    .Where(f => !(new string[] { ".Designer.cs" }).Any(ff => f.EndsWith(ff)))
                    .Where(f => !(new string[] { "AssemblyInfo.cs" }).Contains(Path.GetFileName(f)))
                    .ToArray();
            }


            foreach (string excluderegex in excluderegexs)
            {
                try
                {
                    infiles = infiles
                        .Where(f => !Regex.IsMatch(f, excluderegex, RegexOptions.IgnoreCase))
                        .ToArray();
                }
                catch (ArgumentException ex)
                {
                    WriteLineColor("Invalid RegEx: " + ex.Message, ConsoleColor.Red, 0);
                    return null;
                }
            }

            foreach (string filename in infiles)
            {
                string[] rows = File.ReadAllLines(filename);
                _rowsTotalIndentation += rows.Length;
                bool first = true;

                for (int i = 0; i < rows.Length; i++)
                {
                    string row = rows[i];
                    if (row.StartsWith(" "))
                    {
                        int offset = 0;
                        while (offset < row.Length && row[offset] == ' ')
                        {
                            offset++;
                        }

                        if (offset % _indentationsize != 0)
                        {
                            if (first)
                            {
                                WriteLogLevel("File: '", 2);
                                WriteColor(filename, ConsoleColor.Cyan, 2);
                                WriteLineLogLevel(_loglevel < 3 ? "'" : "':", 2);
                                outfiles.Add(filename);
                                first = false;
                            }

                            WriteLogLevel("" + (i + 1) + ": ", 3);
                            WriteColor(new string('_', offset).ToString(), ConsoleColor.Yellow, 3);
                            WriteLineLogLevel(row.Substring(offset), 3);

                            _rowsIndentation++;
                        }
                    }
                }
            }

            return outfiles.ToArray();
        }

        static void FixTrailing(string[] files)
        {
            WriteLineColor("Fixing trailing whitespaces...", ConsoleColor.Yellow, 1);

            if (files.Length == 0)
            {
                Console.WriteLine("No trailing whitespaces to remove.");
                return;
            }

            Console.WriteLine("All trailing whitespaces will be removed from the " + files.Length + " files...");

            foreach (string filename in files)
            {
                WriteLineLogLevel("Fixing file '" + filename + "'", 3);

                string[] rows = File.ReadAllLines(filename);
                for (int i = 0; i < rows.Length; i++)
                {
                    rows[i] = rows[i].TrimEnd();
                }
                File.WriteAllLines(filename, rows);
            }

            WriteLineColor("Fixing trailing whitespaces: Done!", ConsoleColor.Green, 1);
        }

        static void FixIndentation(string[] files)
        {
            WriteLineColor("Fixing inconsistent indentation...", ConsoleColor.Yellow, 1);

            if (files.Length == 0)
            {
                Console.WriteLine("No inconsistent indentation to fix.");
                return;
            }

            Console.WriteLine("All inconsistent indentation will be fixed in the " + files.Length + " files...");

            foreach (string filename in files)
            {
                WriteLineLogLevel("Fixing file '" + filename + "'", 3);

                string[] rows = File.ReadAllLines(filename);

                for (int i = 0; i < rows.Length; i++)
                {
                    string row = rows[i];
                    if (row.StartsWith(" "))
                    {
                        int offset = 0;
                        while (offset < row.Length && row[offset] == ' ')
                        {
                            offset++;
                        }

                        if (offset % _indentationsize != 0)
                        {
                            int add = _indentationsize - offset % _indentationsize;
                            rows[i] = new string(' ', add) + rows[i];
                        }
                    }
                }
                File.WriteAllLines(filename, rows);
            }

            WriteLineColor("Fixing inconsistent indentation: Done!", ConsoleColor.Green, 1);
        }

        static void WriteLogLevel(string s, int loglevel)
        {
            if (loglevel > _loglevel)
            {
                return;
            }

            Console.Write(s);
        }

        static void WriteLineLogLevel(string s, int loglevel)
        {
            if (loglevel > _loglevel)
            {
                return;
            }

            Console.WriteLine(s);
        }

        static void WriteColor(string s, ConsoleColor color, int loglevel)
        {
            if (loglevel > _loglevel)
            {
                return;
            }

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

        static void WriteLineColor(string s, ConsoleColor color, int loglevel)
        {
            if (loglevel > _loglevel)
            {
                return;
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
    }
}
