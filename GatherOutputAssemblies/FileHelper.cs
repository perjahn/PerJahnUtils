using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace GatherOutputAssemblies
{
    class FileHelper
    {
        public static void TestGetRelativePath()
        {
            Console.WriteLine("Testing GetRelativePath():");
            string[] paths =
            {
                @"Folder1\Folder2\File1.csproj",
                @"Folder3\Folder4\File2.csproj",
                @"..\..\Folder3\Folder4\File2.csproj",

                @"dir1\file1", @"dir2\file2", @"..\dir2\file2",
                @"dir1\", @"dir2\file2", @"..\dir2\file2"
            };

            for (int i = 0; i < paths.Length; i += 3)
            {
                string s = FileHelper.GetRelativePath(paths[i], paths[i + 1]);
                if (s == paths[i + 2])
                    ConsoleHelper.ColorWriteLine(ConsoleColor.Green, "'" + paths[i] + "' -> '" + paths[i + 1] + "' = '" + s + "'");
                else
                    ConsoleHelper.ColorWriteLine(ConsoleColor.Red, "'" + paths[i] + "' -> '" + paths[i + 1] + "' = '" + s + "' (" + paths[i + 2] + ")");
            }
        }

        // Assumes same drive letter on both paths.
        // Sorry, code is not 100% robust.
        public static string GetRelativePath(string pathFrom, string pathTo)
        {
            string s = pathFrom;

            int pos = 0, dirs = 0;
            while (!pathTo.StartsWith(s + Path.DirectorySeparatorChar, StringComparison.InvariantCultureIgnoreCase) && s.Length > 0)
            {
                pos = s.LastIndexOf(Path.DirectorySeparatorChar);
                if (pos == -1)
                {
                    s = string.Empty;
                }
                else
                {
                    s = s.Substring(0, pos);
                }

                dirs++;
            }

            dirs--;

            string s2 = GetDirs(dirs);
            string s3 = pathTo.Substring(pos + 1);
            string s4 = s2 + s3;

            return s4;
        }

        private static string GetDirs(int count)
        {
            StringBuilder sb = new StringBuilder();

            for (int i = 0; i < count; i++)
            {
                sb.Append(".." + Path.DirectorySeparatorChar);
            }

            return sb.ToString();
        }

        public static void TestCompactPath()
        {
            Console.WriteLine("Testing CompactPath():", false);
            string[] paths =
            {
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
            };

            for (int i = 0; i < paths.Length; i += 2)
            {
                string s = FileHelper.CompactPath(paths[i]);
                if (s == paths[i + 1])
                    ConsoleHelper.ColorWriteLine(ConsoleColor.Green, "'" + paths[i] + "' -> '" + s + "'");
                else
                    ConsoleHelper.ColorWriteLine(ConsoleColor.Red, "'" + paths[i] + "' -> '" + s + "' (" + paths[i + 1] + ")");
            }
        }

        // Remove unnecessary .. from path
        // dir1\dir2\..\dir3 -> dir1\dir3
        // This code is 100% robust!
        public static string CompactPath(string path)
        {
            List<string> folders = path.Split(Path.DirectorySeparatorChar).ToList();

            // Remove redundant folders
            for (int i = 0; i < folders.Count;)
            {
                if (i > 0 && folders[i] == ".." && folders[i - 1] != ".." && folders[i - 1] != "")
                {
                    folders.RemoveAt(i - 1);
                    folders.RemoveAt(i - 1);
                    i--;
                }
                else if (i > 0 && folders[i] == "" && folders[i - 1] == "")
                {
                    folders.RemoveAt(i);
                }
                else
                {
                    i++;
                }
            }

            // Combine folders into path2
            string path2 = string.Join(Path.DirectorySeparatorChar.ToString(), folders.ToArray());

            // If path had a starting/ending \, keep it
            string sep = Path.DirectorySeparatorChar.ToString();
            if (path2 == "" && (path.StartsWith(sep) || path.EndsWith(sep)))
            {
                path2 = Path.DirectorySeparatorChar.ToString();
            }

            return path2;
        }

        public static void RemoveRO(string filename)
        {
            FileAttributes fa = File.GetAttributes(filename);
            if ((fa & FileAttributes.ReadOnly) == FileAttributes.ReadOnly)
            {
                File.SetAttributes(filename, fa & ~FileAttributes.ReadOnly);
            }
        }

        public static string GetCleanFolderName(string dirtyFolderName)
        {
            StringBuilder sb = new StringBuilder();
            foreach (char c in dirtyFolderName)
            {
                if (char.IsLetterOrDigit(c) || c == '.' || c == '_' || c == '-')
                {
                    sb.Append(c);
                }
            }

            string cleaned = sb.ToString();

            if (cleaned != dirtyFolderName)
            {
                ConsoleHelper.ColorWriteLine(ConsoleColor.Yellow, "Project named cleaned up: '" + dirtyFolderName + "' -> '" + cleaned + "¨'");
            }

            return cleaned;
        }

        public static bool CopyFolder(DirectoryInfo source, DirectoryInfo target, bool simulate, bool verbose, ref int copiedFiles)
        {
            if (!source.Exists)
            {
                ConsoleHelper.ColorWriteLine(ConsoleColor.Red, "Ignoring folder, it does not exist: '" + source.FullName + "'");
                return false;
            }

            if (!target.Exists)
            {
                if (verbose)
                {
                    Console.WriteLine("Creating folder: '" + target.FullName + "'");
                }
                if (!simulate)
                {
                    Directory.CreateDirectory(target.FullName);
                }
            }

            foreach (FileInfo fi in source.GetFiles())
            {
                string sourcefile = fi.FullName;
                string targetfile = Path.Combine(target.FullName, fi.Name);
                if (verbose)
                {
                    Console.WriteLine("Copying file: '" + sourcefile + "' -> '" + targetfile + "'");
                }
                if (!simulate)
                {
                    File.Copy(sourcefile, targetfile, true);
                }
                copiedFiles++;
            }

            foreach (DirectoryInfo di in source.GetDirectories())
            {
                DirectoryInfo targetSubdir = new DirectoryInfo(Path.Combine(target.FullName, di.Name));
                CopyFolder(di, targetSubdir, simulate, verbose, ref copiedFiles);
            }

            return true;
        }
    }
}
