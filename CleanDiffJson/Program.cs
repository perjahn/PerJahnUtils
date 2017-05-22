﻿using Newtonsoft.Json.Linq;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;

namespace CleanDiffJson
{
    class Program
    {
        static bool _SortChildren, _WinDiff, _DontDiffIfEqual;

        static List<string> _searchPaths = new List<string>();

        static int Main(string[] args)
        {
            string usage =
                @"CleanDiffJson 0.1 - Compare normalized json files

Usage: CleanDiffJson [flags] <filename1> <filename2>

Optional flags:
-DontSortChildren  - Don't sort children
-DontWinDiff       - Don't start WinDiff
-DontDiffIfEqual   - Only start WinDiff if different.";


            if (args.Length < 2)
            {
                Console.WriteLine(usage);
                return -1;
            }

            for (int arg = 0; arg < args.Length - 2; arg++)
            {
                if (!CheckArg(args[arg]))
                {
                    Console.WriteLine(usage);
                    return -1;
                }
            }

            string file1 = args[args.Length - 2];
            string file2 = args[args.Length - 1];
            SetFlags(args, args.Length - 1);

            string windiffpath = FindWinDiff();
            if (windiffpath == null)
            {
                Console.WriteLine("Couldn't find windiff.exe, the following paths was searched:" + Environment.NewLine +
                    "'" + string.Join("'" + Environment.NewLine + "'", _searchPaths.ToArray()) + "'");
                return -1;
            }

            Console.WriteLine("Using windiff: '" + windiffpath + "'");

            bool result = DiffJson(windiffpath, file1, file2);

            if (result)
                return 1;
            else
                return 0;
        }

        static string FindWinDiff()
        {
            string[] windiffpaths =
            {
                Environment.CurrentDirectory,
                @"C:\Program Files (x86)\Microsoft SDKs\Windows\v7.0A\bin",
                @"C:\Utils"
            };

            _searchPaths.AddRange(windiffpaths);

            _searchPaths.AddRange(Environment.GetEnvironmentVariable("path").Split(';'));

            string asspath = Assembly.GetExecutingAssembly().Location;
            _searchPaths.Add(Path.GetDirectoryName(asspath));

            foreach (string path in _searchPaths)
            {
                string windiffpath = Path.Combine(path, "windiff.exe");
                if (File.Exists(windiffpath))
                {
                    return windiffpath;
                }
            }

            return null;
        }

        static bool CheckArg(string arg)
        {
            if (arg != "-DontSortChildren" && arg != "-DontWinDiff" && arg != "-DontDiffIfEqual")
            {
                Console.WriteLine("Unrecognized argument: '" + arg + "'");
                return false;
            }

            return true;
        }

        static void SetFlags(string[] args, int flags)
        {
            _SortChildren = _WinDiff = _DontDiffIfEqual = true;

            for (int i = 0; i < flags; i++)
            {
                if (args[i] == "-DontSortChildren")
                    _SortChildren = false;
                if (args[i] == "-DontWinDiff")
                    _WinDiff = false;
                if (args[i] == "-DontDiffIfEqual")
                    _DontDiffIfEqual = false;
            }
        }

        static string GetSemiUniqueFileName(string filepath, string ext)
        {
            for (int i = 1; i <= 10000; i++)
            {
                string fullfilename = filepath + ".CleanDiffJson" + i + ext;
                if (File.GetLastWriteTime(fullfilename) < DateTime.Now.AddMinutes(-5))
                {
                    return fullfilename;
                }
            }

            throw new Exception("Please clean your temp folder!");
        }

        static bool DiffJson(string windiffpath, string file1, string file2)
        {
            string tempfolder = Path.GetTempPath();

            string outfile1 = GetSemiUniqueFileName(Path.Combine(tempfolder, Path.GetFileNameWithoutExtension(file1)), ".json");
            if (!CleanFile(file1, outfile1))
                return false;

            string outfile2 = GetSemiUniqueFileName(Path.Combine(tempfolder, Path.GetFileNameWithoutExtension(file2)), ".json");
            if (!CleanFile(file2, outfile2))
                return false;

            bool diff;

            byte[] buf1 = File.ReadAllBytes(outfile1);
            byte[] buf2 = File.ReadAllBytes(outfile2);

            IStructuralEquatable eqa1 = buf1;
            diff = !eqa1.Equals(buf2, StructuralComparisons.StructuralEqualityComparer);


            if (_DontDiffIfEqual || diff)
            {
                if (_WinDiff)
                {
                    Console.WriteLine("Diffing: '" + file1 + "' and '" + file2 + "'");
                    System.Diagnostics.Process.Start(windiffpath, "\"" + outfile1 + "\" \"" + outfile2 + "\"");
                }
            }

            return diff;
        }

        static bool CleanFile(string infile, string outfile)
        {
            string content;

            try
            {
                content = File.ReadAllText(infile);
            }
            catch (IOException ex)
            {
                Console.WriteLine(ex.Message);
                return false;
            }

            JObject jobject = null;
            jobject = JObject.Parse(content);

            if (_SortChildren)
            {
                jobject = GetSortedObject(jobject) as JObject;
            }

            string pretty = jobject.ToString(Newtonsoft.Json.Formatting.Indented);

            File.WriteAllText(outfile, pretty);

            return true;
        }

        static JToken GetSortedObject(JToken jtoken)
        {
            if (jtoken.Type == JTokenType.Object)
            {
                Console.WriteLine($"Adding object: >>{jtoken.Path}<<");

                JObject old = jtoken as JObject;
                JObject jobject = new JObject();

                foreach (JToken child in old.Children().OrderByDescending(c => c.Path))
                {
                    Console.WriteLine($"Adding object child: >>{child.Path}<<");
                    jobject.AddFirst(GetSortedObject(child));
                }

                return jobject;
            }
            else if (jtoken.Type == JTokenType.Property)
            {
                Console.WriteLine($"Adding property: >>{jtoken.Path}<<");

                JProperty old = jtoken as JProperty;
                JProperty jproperty = new JProperty(old.Name, old.Value);

                foreach (JToken child in old.Children().OrderByDescending(c => c.Path))
                {
                    Console.WriteLine($"Adding property child: >>{child.Path}<<");
                    JToken newchild = GetSortedObject(child);
                    jproperty.Value = newchild;
                }

                return jproperty;
            }
            else
            {
                return jtoken;
            }
        }
    }
}