using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;

namespace CleanDiffJson
{
    class Program
    {
        static bool _SortChildren, _WinDiff, _DontDiffIfEqual;
        static bool _VerboseLogging;

        static List<string> _searchPaths = [];

        static int Main(string[] args)
        {
            var usage =
                @"CleanDiffJson 0.3 - Compare normalized json files

Usage: CleanDiffJson [flags] <filename1> <filename2>

Optional flags:
-DontSortChildren  - Don't sort children.
-DontWinDiff       - Don't start WinDiff.
-DontDiffIfEqual   - Only start WinDiff if different.
-Log               - Verbose logging.";

            if (args.Length < 2)
            {
                Console.WriteLine(usage);
                return -1;
            }

            for (var arg = 0; arg < args.Length - 2; arg++)
            {
                if (!CheckArg(args[arg]))
                {
                    Console.WriteLine(usage);
                    return -1;
                }
            }

            var file1 = args[^2];
            var file2 = args[^1];
            SetFlags(args, args.Length - 1);

            var windiffpath = FindWinDiff();
            if (windiffpath == null)
            {
                Console.WriteLine("Couldn't find windiff.exe, the following paths was searched:" + Environment.NewLine +
                    "'" + string.Join("'" + Environment.NewLine + "'", _searchPaths) + "'");
                return -1;
            }

            Console.WriteLine($"Using windiff: '{windiffpath}'");

            var result = DiffJson(windiffpath, file1, file2);

            return result ? 1 : 0;
        }

        static string FindWinDiff()
        {
            string[] windiffpaths =
            [
                Environment.CurrentDirectory,
                @"C:\Program Files (x86)\Microsoft SDKs\Windows\v7.0A\bin",
                @"C:\Utils"
            ];

            _searchPaths.AddRange(windiffpaths);

            _searchPaths.AddRange(Environment.GetEnvironmentVariable("path").Split(';'));

            var asspath = Assembly.GetExecutingAssembly().Location;
            _searchPaths.Add(Path.GetDirectoryName(asspath));

            foreach (var path in _searchPaths)
            {
                var windiffpath = Path.Combine(path, "windiff.exe");
                if (File.Exists(windiffpath))
                {
                    return windiffpath;
                }
            }

            return null;
        }

        static bool CheckArg(string arg)
        {
            string[] allowedFlags = ["-DontSortChildren", "-DontWinDiff", "-DontDiffIfEqual", "-Log"];

            if (!allowedFlags.Contains(arg))
            {
                Console.WriteLine($"Unrecognized argument: '{arg}'");
                return false;
            }

            return true;
        }

        static void SetFlags(string[] args, int flags)
        {
            _SortChildren = _WinDiff = _DontDiffIfEqual = true;
            _VerboseLogging = false;

            for (var i = 0; i < flags; i++)
            {
                if (args[i] == "-DontSortChildren")
                {
                    _SortChildren = false;
                }
                if (args[i] == "-DontWinDiff")
                {
                    _WinDiff = false;
                }
                if (args[i] == "-DontDiffIfEqual")
                {
                    _DontDiffIfEqual = false;
                }
                if (args[i] == "-Log")
                {
                    _VerboseLogging = true;
                }
            }
        }

        static string GetSemiUniqueFileName(string filepath, string ext)
        {
            for (var i = 1; i <= 10000; i++)
            {
                var fullfilename = $"{filepath}.CleanDiffJson{i}{ext}";
                if (File.GetLastWriteTime(fullfilename) < DateTime.Now.AddMinutes(-5))
                {
                    return fullfilename;
                }
            }

            throw new Exception("Please clean your temp folder!");
        }

        static bool DiffJson(string windiffpath, string file1, string file2)
        {
            var tempfolder = Path.GetTempPath();

            var outfile1 = GetSemiUniqueFileName(Path.Combine(tempfolder, Path.GetFileNameWithoutExtension(file1)), ".json");
            if (!CleanFile(file1, outfile1))
            {
                return false;
            }

            var outfile2 = GetSemiUniqueFileName(Path.Combine(tempfolder, Path.GetFileNameWithoutExtension(file2)), ".json");
            if (!CleanFile(file2, outfile2))
            {
                return false;
            }

            bool diff;

            var buf1 = File.ReadAllBytes(outfile1);
            var buf2 = File.ReadAllBytes(outfile2);

            IStructuralEquatable eqa1 = buf1;
            diff = !eqa1.Equals(buf2, StructuralComparisons.StructuralEqualityComparer);

            if (_DontDiffIfEqual || diff)
            {
                if (_WinDiff)
                {
                    Console.WriteLine($"Diffing: '{file1}' and '{file2}'");
                    _ = Process.Start(windiffpath, $"\"{outfile1}\" \"{outfile2}\"");
                }
            }

            return diff;
        }

        static bool CleanFile(string infile, string outfile)
        {
            string content;

            try
            {
                Console.WriteLine($"Reading: '{infile}'");
                content = File.ReadAllText(infile);
            }
            catch (IOException ex)
            {
                Console.WriteLine(ex.Message);
                return false;
            }

            var jtoken = JToken.Parse(content);

            if (_SortChildren)
            {
                jtoken = GetSortedJson(jtoken);
            }

            var pretty = jtoken.ToString(Formatting.Indented);

            File.WriteAllText(outfile, pretty);

            return true;
        }

        static JToken GetSortedJson(JToken jtoken)
        {
            if (jtoken.Type == JTokenType.Object)
            {
                Log($"Adding object: >>{jtoken.Path}<<");

                var old = jtoken as JObject;
                JObject jobject = [];

                foreach (var child in old.Children().OrderByDescending(c => c.Path, StringComparer.InvariantCultureIgnoreCase))
                {
                    Log($"Adding object child: >>{child.Path}<<");
                    jobject.AddFirst(GetSortedJson(child));
                }

                return jobject;
            }
            else if (jtoken.Type == JTokenType.Property)
            {
                Log($"Adding property: >>{jtoken.Path}<<");

                var old = jtoken as JProperty;
                JProperty jproperty = new(old.Name, old.Value);

                foreach (var child in old.Children().OrderByDescending(c => c.Path, StringComparer.InvariantCultureIgnoreCase))
                {
                    Log($"Adding property child: >>{child.Path}<<");
                    JToken newchild = GetSortedJson(child);
                    jproperty.Value = newchild;
                }

                return jproperty;
            }
            else if (jtoken.Type == JTokenType.Array)
            {
                Log($"Adding array: >>{jtoken.Path}<<");

                var old = jtoken as JArray;
                JArray jarray = [];

                var sortedChildren = old.Select(GetSortedJson).OrderBy(c => c.ToString(), StringComparer.InvariantCultureIgnoreCase);

                foreach (var child in sortedChildren)
                {
                    Log($"Adding array child: >>{child.Path}<<");
                    jarray.Add(child);
                }

                return jarray;
            }
            else
            {
                Log($"Generic type: '{jtoken.Type}'");
                return jtoken;
            }
        }

        static void Log(string message)
        {
            if (_VerboseLogging)
            {
                Console.WriteLine(message);
            }
        }
    }
}
