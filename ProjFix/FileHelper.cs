using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace ProjFix
{
    class FileHelper
    {
        public static void TestGetRelativePath()
        {
            ConsoleHelper.WriteLine("Testing GetRelativePath():", false);
            string[] paths =
            [
                @"Folder1\Folder2\File1.csproj",
                @"Folder3\Folder4\File2.csproj",
                @"..\..\Folder3\Folder4\File2.csproj",

                @"dir1\file1", @"dir2\file2", @"..\dir2\file2",
                @"dir1\", @"dir2\file2", @"..\dir2\file2"
            ];

            for (var i = 0; i < paths.Length; i += 3)
            {
                var s = GetRelativePath(paths[i], paths[i + 1]);
                if (s == paths[i + 2])
                {
                    ConsoleHelper.ColorWrite(ConsoleColor.Green, $"'{paths[i]}' -> '{paths[i + 1]}' = '{s}'");
                }
                else
                {
                    ConsoleHelper.ColorWrite(ConsoleColor.Red, $"'{paths[i]}' -> '{paths[i + 1]}' = '{s}' ({paths[i + 2]})");
                }
            }
        }

        // Assumes same drive letter on both paths.
        // Sorry, code is not 100% robust.
        public static string GetRelativePath(string pathFrom, string pathTo)
        {
            var s = pathFrom;

            int pos = 0, dirs = 0;
            while (!pathTo.StartsWith(s + Path.DirectorySeparatorChar, StringComparison.InvariantCultureIgnoreCase) && s.Length > 0)
            {
                pos = s.LastIndexOf(Path.DirectorySeparatorChar);
                s = pos == -1 ? string.Empty : s[..pos];
                dirs++;
            }

            dirs--;

            var s2 = GetDirs(dirs);
            var s3 = pathTo[(pos + 1)..];
            var s4 = s2 + s3;

            return s4;
        }

        private static string GetDirs(int count)
        {
            StringBuilder sb = new();

            for (var i = 0; i < count; i++)
            {
                _ = sb.Append($"..{Path.DirectorySeparatorChar}");
            }

            return sb.ToString();
        }

        public static void TestCompactPath()
        {
            ConsoleHelper.WriteLine("Testing CompactPath():", false);
            string[] paths =
            [
                @"", @"",
                @"\", @"\",
                @"..", @"..",
                @"\..", @"\..",
                @"..\", @"..\",
                @"ab\", @"ab\",
                @"\ba", @"\ba",
                @"a", @"a",
                @"..\a", @"..\a",

                @"a\..", @"",
                @"..\a\..", @"..",
                @"..\..\a", @"..\..\a",
                @"abc\def\..", @"abc",
                @"abc\..\def", @"def",
                @"..\abc\def", @"..\abc\def",

                @"\a\..", @"\",
                @"\..\a\..", @"\..",
                @"\..\..\a", @"\..\..\a",
                @"\abc\def\..", @"\abc",
                @"\abc\..\def", @"\def",
                @"\..\abc\def", @"\..\abc\def",

                @"a\..\", @"\",
                @"..\a\..\", @"..\",
                @"..\..\a\", @"..\..\a\",
                @"abc\def\..\", @"abc\",
                @"abc\..\def\", @"def\",
                @"..\abc\def\", @"..\abc\def\",

                @"\a\..\", @"\",
                @"\..\a\..\", @"\..\",
                @"\..\..\a\", @"\..\..\a\",
                @"\abc\def\..\", @"\abc\",
                @"\abc\..\def\", @"\def\",
                @"\..\abc\def\", @"\..\abc\def\",

                @"dir1\dir2\dir3\..\dir4", @"dir1\dir2\dir4",

                @"\\", @"\",
                @"\\x", @"\x",
                @"x\\", @"x\",

                @"\\", @"\",
                @"\\..", @"\..",
                @"..\\", @"..\",

                @"\\\\", @"\"
            ];

            for (var i = 0; i < paths.Length; i += 2)
            {
                var s = CompactPath(paths[i]);
                if (s == paths[i + 1])
                {
                    ConsoleHelper.ColorWrite(ConsoleColor.Green, $"'{paths[i]}' -> '{s}'");
                }
                else
                {
                    ConsoleHelper.ColorWrite(ConsoleColor.Red, $"'{paths[i]}' -> '{s}' ({paths[i + 1]})");
                }
            }
        }

        // Remove unnecessary .. from path
        // dir1\dir2\..\dir3 -> dir1\dir3
        // This code is 100% robust!
        public static string CompactPath(string path)
        {
            List<string> folders = [.. path.Split(Path.DirectorySeparatorChar)];

            // Remove redundant folders
            for (var i = 0; i < folders.Count;)
            {
                if (i > 0 && folders[i] == ".." && folders[i - 1] != ".." && folders[i - 1] != string.Empty)
                {
                    folders.RemoveAt(i - 1);
                    folders.RemoveAt(i - 1);
                    i--;
                }
                else if (i > 0 && folders[i] == string.Empty && folders[i - 1] == string.Empty)
                {
                    folders.RemoveAt(i);
                }
                else
                {
                    i++;
                }
            }

            // Combine folders into path2
            var path2 = string.Join(Path.DirectorySeparatorChar.ToString(), folders);

            // If path had a starting/ending \, keep it
            var sep = Path.DirectorySeparatorChar.ToString();
            if (path2 == string.Empty && (path.StartsWith(sep) || path.EndsWith(sep)))
            {
                path2 = Path.DirectorySeparatorChar.ToString();
            }

            return path2;
        }

        public static void RemoveRO(string filename)
        {
            var fa = File.GetAttributes(filename);
            if ((fa & FileAttributes.ReadOnly) == FileAttributes.ReadOnly)
            {
                File.SetAttributes(filename, fa & ~FileAttributes.ReadOnly);
            }
        }
    }
}
