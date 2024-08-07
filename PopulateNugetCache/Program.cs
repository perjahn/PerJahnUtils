﻿using System;
using System.IO;
using System.Linq;

namespace PopulateNugetCache
{
    class Program
    {
        static int Main(string[] args)
        {
            var dryrun = args.Contains("-dryrun");
            var operation = args.Contains("-move") ? Populate.OperationMode.move : Populate.OperationMode.copy;
            var verbose = args.Contains("-verbose");
            var allowedFlags = new[] { "-dryrun", "-move", "-verbose" };
            string[] parsedArgs = [.. args.Where(a => !allowedFlags.Contains(a))];

            if (parsedArgs.Length != 1)
            {
                Log("0.001 - Populates the local nuget cache from multiple sources.\n" +
                    "\n" +
                    "Usage: populate <sourcepath> [-dryrun] [-move] [-verbose]\n" +
                    "\n" +
                    "-dryrun:   Simulate without side effects.\n" +
                    "-move:     Move files insted of copy (usually faster).\n" +
                    "-verbose:  Loglevel verbose logging.");
                return 1;
            }

            var sourceRootFolder = parsedArgs[0];

            if (!Directory.Exists(sourceRootFolder))
            {
                Log($"Folder not found: '{sourceRootFolder}'");
                return 1;
            }

            var populate = new Populate
            {
                Dryrun = dryrun,
                Operation = operation,
                Verbose = verbose
            };

            populate.PopulateNugetCache(sourceRootFolder);

            return 0;
        }

        static void Log(string message)
        {
            Console.WriteLine(message);
        }
    }
}
