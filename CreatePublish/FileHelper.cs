using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace CreatePublish
{
    class FileHelper
    {
        public static void TestGetRelativePath()
        {
            ConsoleHelper.WriteLine("Testing GetRelativePath():", false);
            string[] paths =
            [
                @"Folder1\Folder2\Folder3",
                @"Folder1\Folder4\File1.csproj",
                @"..\..\Folder4\File1.csproj",

                @"dir1\dir2\dir3\dir4",
                @"dir1\dir5",
                @"..\..\..\dir5",

                @"c:\dir1\dir2",
                @"c:\dir1\dir3",
                @"..\dir3",

                @"c:\dir1\dir2",
                @"d:\dir1\dir3",
                @"d:\dir1\dir3",

                @"dir1\dir2",
                @"dir1\dir3\dir4\dir5",
                @"..\dir3\dir4\dir5",

                @"dir1\dir1", @"dir2\file2", @"..\..\dir2\file2",
                @"dir1\", @"dir2\file2", @"..\..\dir2\file2",

                @"c:\dir1\dir1", @"c:\dir2\file2.txt", @"..\..\dir2\file2.txt",
                @"c:\dir1", @"c:\dir2\file2.txt", @"..\dir2\file2.txt",

                @"dir1\file.sln",
                @"dir1\My.Web\My.Web.csproj",
                @"..\My.Web\My.Web.csproj",

                @"dir1",
                @"dir1\My.Web",
                @"My.Web",

                @"",
                @"dir1\My.Web",
                @"dir1\My.Web"
            ];

            for (var i = 0; i < paths.Length; i += 3)
            {
                var relpath = GetRelativePath(paths[i], paths[i + 1]);
                if (relpath == paths[i + 2])
                {
                    ConsoleHelper.ColorWrite(ConsoleColor.Green, $"'{paths[i]}' -> '{paths[i + 1]}' = '{relpath}'");
                }
                else
                {
                    ConsoleHelper.ColorWrite(ConsoleColor.Red, $"'{paths[i]}' -> '{paths[i + 1]}' = '{relpath}' ({paths[i + 2]})");
                }
            }

            var path1 = @"d:\dir1\dir3";
            var path2 = @"dir1\dir2";
            try
            {
                var relpath = GetRelativePath(path1, path2);
                ConsoleHelper.ColorWrite(ConsoleColor.Red, $"'{path1}' -> '{path2}' = 'ArgumentException' ({relpath})");
            }
            catch (Exception ex)
            {
                if (ex.GetType() == typeof(ArgumentException))
                {
                    ConsoleHelper.ColorWrite(ConsoleColor.Green, $"'{path1}' -> '{path2}' = '{ex.Message}'");
                }
                else
                {
                    ConsoleHelper.ColorWrite(ConsoleColor.Red, $"'{path1}' -> '{path2}' = 'ArgumentException' ({ex})");
                }
            }
        }

        // Handles both absolute and relative paths.
        // Caution: Assumes pathFrom is a folder!
        // Does not remove redundant ..
        // Does not touch the file system.
        public static string GetRelativePath(string pathFrom, string pathTo)
        {
            if (Path.IsPathRooted(pathFrom) && !Path.IsPathRooted(pathTo))
            {
                // Not obvious what to do here, using the current
                // directory can sometimes be dangerous, because
                // some processes execute in the OS system dir, and
                // you don't want to mess with that folder.
                throw new ArgumentException("If pathFrom is absolute, pathTo must be it too. Path.GetFullPath might be of use.");
            }

            if (pathFrom == string.Empty)
            {
                return pathTo;
            }

            var dirsFrom = pathFrom.Split(Path.DirectorySeparatorChar);
            var dirsTo = pathTo.Split(Path.DirectorySeparatorChar);

            var dirs = 0;
            for (var i = 0; i < dirsFrom.Length && i < dirsTo.Length; i++)
            {
                if (dirsFrom[i] == dirsTo[i])
                {
                    dirs++;
                }
                else
                {
                    break;
                }
            }

            var s1 = string.Join(Path.DirectorySeparatorChar.ToString(), Enumerable.Repeat("..", dirsFrom.Length - dirs));
            var s2 = string.Join(Path.DirectorySeparatorChar.ToString(), dirsTo.Skip(dirs));

            return Path.Combine(s1, s2);
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
