// todo:
// prevent circular references.
// multiple referenced to same ass seems to be compacted. print warning?
using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;

namespace ProjFix
{
    class Program
    {
        private static string eol = Environment.NewLine;

        static int Main(string[] args)
        {
            // Make all string comparisons (and sort/order) invariant of current culture
            // Thus, project output files is written in a consistent manner
            System.Threading.Thread.CurrentThread.CurrentCulture = System.Globalization.CultureInfo.InvariantCulture;

            string usage =  // Todo
@"Usage: ProjFix [-b] [-c] [-hPaths] [-oPath] [-r] [-s] [-v] <solutionfile>

NOT: -c:   Force copy local true (NOT IMPLEMENTED YET). Why is this useful? GAC dlls should usually not be copied to output folder.
NOT: -c:   If copy local is true, don't remove hint path. (copy local means that VS copies the referenced dll to the output folder,
           *even when the file is in GAC*). This should be done automatically, I see no reason for not doing it. Option is unnecessary.
           Private without Hintpath is useless, probably invalid.
 -d:   On all dll references: (Force specific version=False and) remove all version info.
 -o:   Set common output path for all projects (NOT IMPLEMENTED YET).

Example: ProjFix -h..\Dir1,Dir2 -obin mysol.sln

Rootpath will be search recursively for matching .targets files, must be uniquely named.";

            usage =
@"ProjFix 2.7 - Program for patching Visual Studio project files.

Usage: ProjFix [-b] [-hPaths] [-r] [-s] [-v] <solutionfile>

 -b:   Don't create backup files.
 -d:   On all dll references: Remove all version info.
 -h:   Hint path folders for assembly references (dlls).
 -r:   Restore project files from backups.
 -s:   Simulate: Don't write anything to file system.
 -v:   Verbose logging.

Example: ProjFix -h..\Dir1,Dir2 -obin mysol.sln

Hintpaths are relative from current directory.";

            bool nobackup = false;
            bool copylocal = false;
            bool removeversion = false;
            bool restore = false;
            bool simulate = false;
            List<string> hintpaths = null;
            string outputpath = null;
            string solutionfile;

            int arg;

            ConsoleHelper.verboselogging = false;

            for (arg = 0; arg < args.Length; arg++)
            {
                if (args[arg].StartsWith("-b") && args[arg].Length == 2)
                {
                    nobackup = true;
                }
                else if (args[arg].StartsWith("-c") && args[arg].Length == 2)
                {
                    copylocal = true;
                }
                else if (args[arg].StartsWith("-d") && args[arg].Length == 2)
                {
                    removeversion = true;
                }
                else if (args[arg].StartsWith("-h") && args[arg].Length >= 3)
                {
                    hintpaths = args[arg].Substring(2).Split(',').ToList();
                }
                else if (args[arg].StartsWith("-o") && args[arg].Length >= 3)
                {
                    outputpath = args[arg].Substring(2);
                }
                else if (args[arg].StartsWith("-r") && args[arg].Length == 2)
                {
                    restore = true;
                }
                else if (args[arg].StartsWith("-s") && args[arg].Length == 2)
                {
                    simulate = true;
                }
                else if (args[arg].StartsWith("-v") && args[arg].Length == 2)
                {
                    ConsoleHelper.verboselogging = true;
                }
                else
                {
                    break;
                }
            }

            if (arg != args.Length - 1)
            {
                ConsoleHelper.WriteLine(usage, false);
                return 1;
            }

            if (args[0] == "TestCompactPath")
            {
                FileHelper.TestCompactPath();
                return 99;
            }

            if (args[0] == "TestGetRelativePath")
            {
                FileHelper.TestGetRelativePath();
                return 99;
            }

            solutionfile = args[args.Length - 1];

            ConsoleHelper.WriteLine(
                "solutionfile:   " + (solutionfile == null ? "<null>" : "'" + solutionfile + "'") + eol +
                "nobackup:       " + nobackup + eol +
                "copylocal:      " + copylocal + eol +
                "hintpaths:      " + (hintpaths == null ? "<null>" : "'" + string.Join(",", hintpaths) + "'") + eol +
                "outputpath:     " + (outputpath == null ? "<null>" : "'" + outputpath + "'") + eol +
                "removeversion:  " + removeversion + eol +
                "restore:        " + restore + eol +
                "simulate:       " + simulate + eol +
                "verboselogging: " + ConsoleHelper.verboselogging,
                true);

            if (restore)
            {
                Solution s = new Solution(solutionfile);
                s.RestoreProjects();
            }
            else
            {
                Solution s = new Solution(solutionfile);

                List<Project> projects = s.LoadProjects();
                if (projects == null)
                    return 2;

                bool success = s.FixProjects(projects, hintpaths, outputpath, copylocal, removeversion);
                if (success == false)
                    return 3;

                s.WriteProjects(projects, hintpaths, outputpath, simulate, nobackup);
            }

            return 0;
        }
    }

    /*
			public static class StringExtensions
			{
					public static int LessThan(this string s1, string s2)
					{
							return string.Compare(s1, s2);
					}

					public static int MoreThan(this string s1, string s2)
					{
							return string.Compare(s1, s2);
					}
			}
	*/
}
