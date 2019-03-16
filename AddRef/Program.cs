using System;
using System.Collections.Generic;
using System.Linq;

namespace AddRef
{
    class Program
    {
        static void Main(string[] args)
        {
            string usage =
@"AddRef 1.0 - Program for adding assembly reference to Visual Studio project file.

Usage: AddRef <ReferenceName> <ProjectFile>

Example: AddRef System.Something folder\myproject.csproj";

            if (args.Length != 2)
            {
                Console.WriteLine(usage);
                return;
            }

            string referencename = args[0];
            string projectfilepath = args[1];


            Project.AddRef(projectfilepath, referencename);
        }
    }
}
