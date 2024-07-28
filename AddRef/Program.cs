using System;

namespace AddRef
{
    class Program
    {
        static void Main(string[] args)
        {
            var usage =
@"AddRef 1.0 - Program for adding assembly reference to Visual Studio project file.

Usage: AddRef <ReferenceName> <ProjectFile>

Example: AddRef System.Something folder\myproject.csproj";

            if (args.Length != 2)
            {
                Console.WriteLine(usage);
                return;
            }

            var referencename = args[0];
            var projectfilepath = args[1];

            Project.AddRef(projectfilepath, referencename);
        }
    }
}
