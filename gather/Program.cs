// Gather output files from compiled projects
// Wish list features:
//  * Different output folders for x86/x84/AnyCPU
//  * Fallback for vc2008 projects (simple search for exe)

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml;
using System.Xml.Linq;

namespace gather
{
    class Program
    {
        static void Main(string[] args)
        {
            if (args.Length != 2)
            {
                Console.WriteLine(
                    "Usage: gather <config> <outputpath>" + Environment.NewLine +
                    Environment.NewLine +
                    "Example: gather Release c:\\utils");
                return;
            }

            Gather(args[0], args[1]);
        }

        static void Gather(string config, string outputpath)
        {
            var solutions = Directory.GetFiles(".", "*.sln", SearchOption.AllDirectories);

            string[] paths = [.. ParseSolutionFiles(solutions, config)];

            if (!Directory.Exists(outputpath))
            {
                Directory.CreateDirectory(outputpath);
            }

            foreach (var path in paths)
            {
                if (File.Exists(path))
                {
                    var path2 = Path.Combine(outputpath, Path.GetFileName(path));
                    Console.WriteLine($"'{path}' -> '{path2}'");
                    try
                    {
                        File.Copy(path, path2, true);
                    }
                    catch (IOException ex)
                    {
                        Console.WriteLine($"Couldn't copy file: '{path}' -> '{path2}': {ex.Message}");
                    }
                }
                else
                {
                    var old = Console.ForegroundColor;
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine($"'{path}'");
                    Console.ForegroundColor = old;
                }
            }
        }

        static IEnumerable<string> ParseSolutionFiles(string[] solutions, string config)
        {
            foreach (var solutionfile in solutions)
            {
                string[] rows;
                try
                {
                    rows = File.ReadAllLines(solutionfile);
                }
                catch (FileNotFoundException ex)
                {
                    Console.WriteLine(ex.Message);
                    yield break;
                }

                foreach (var row in rows)
                {
                    // Project("{FAE04EC0-301F-11D3-BF4B-00C04F79EFBC}") = "MyCsProject", "Folder\Folder\MyCsProject.csproj", "{01010101-0101-0101-0101-010101010101}"
                    // Project("{F184B08F-C81C-45F6-A57F-5ABD9991F28F}") = "MyVbProject", "Folder\Folder\MyVbProject.vbproj", "{02020202-0202-0202-0202-020202020202}"
                    // Project("{8BC9CEB8-8B4A-11D0-8D11-00A0C91BC942}") = "MyVcProject", "Folder\Folder\MyVcProject.vcxproj", "{03030303-0303-0303-0303-030303030303}"

                    string[] projtypeguids = [
                        "{FAE04EC0-301F-11D3-BF4B-00C04F79EFBC}",
                        "{F184B08F-C81C-45F6-A57F-5ABD9991F28F}",
                        "{8BC9CEB8-8B4A-11D0-8D11-00A0C91BC942}" ];

                    foreach (var projtypeguid in projtypeguids)
                    {
                        var projtypeline = $"Project(\"{projtypeguid}\") =";

                        if (row.StartsWith(projtypeline))
                        {
                            var values = row[projtypeline.Length..].Split(',');
                            if (values.Length != 3)
                            {
                                continue;
                            }

                            var projectfilepath = values[1].Trim().Trim('"');

                            string[] paths = [.. ParseProjectFile(solutionfile, projectfilepath, config)];
                            foreach (var path in paths)
                            {
                                yield return path;
                            }
                        }
                    }
                }
            }
        }

        static IEnumerable<string> ParseProjectFile(string solutionfile, string projectfilepath, string config)
        {
            XDocument xdoc;

            var SolutionDir = Path.GetDirectoryName(solutionfile);
            var filename = Path.Combine(SolutionDir, projectfilepath);

            try
            {
                xdoc = XDocument.Load(filename);
            }
            catch (Exception ex) when (ex is IOException or XmlException)
            {
                Console.WriteLine($"Couldn't load project: '{filename}': {ex.Message}");
                yield break;
            }

            var ns = xdoc.Root.Name.Namespace;

            // VC++ 2008 projects has obsolete xml schema - no outputpaths exists.

            // When msbuild builds have multiple matching configs with the same name,
            //   VS chooce one at random: (Release|x86, Release|AnyCPU).
            // We have to check all (distinct) output paths for built assembly files.

            // VC: Uses OutDir tag instead, but is usually missing default, and that means
            //   = $(SolutionDir)$(Configuration)\ -> no bin folder
            // OutDir can also also be used on CS/VB projects - but is only used if
            //   OutputPath is missing.

            string[] outputpaths = [.. xdoc
                .Elements(ns + "Project").Elements(ns + "PropertyGroup").Elements(ns + "OutputPath")
                .Where(el => MatchCondition(el.Parent.Attribute("Condition"), config))
                .Select(el => el.Value)
                .Distinct()];

            if (outputpaths.Length != 0)
            {
                // OutputPath>bin\Release\<

                foreach (var outputpath in outputpaths)
                {
                    var path = Path.Combine(SolutionDir, Path.GetDirectoryName(projectfilepath), outputpath, GetAssName(projectfilepath));
                    yield return path;
                }
            }
            else
            {
                string[] outdirs = [.. xdoc
                    .Elements(ns + "Project").Elements(ns + "PropertyGroup").Elements(ns + "OutDir")
                    .Where(el => MatchCondition(el.Parent.Attribute("Condition"), config))
                    .Select(el => el.Value)
                    .Distinct()];

                if (outdirs.Length != 0)
                {
                    // OutDir>$(SolutionDir)$(Configuration)\mysubdir<

                    foreach (var outdir in outdirs)
                    {
                        var path = outdir.Replace("$(SolutionDir)", SolutionDir + Path.DirectorySeparatorChar).Replace("$(Configuration)", config);
                        path = Path.Combine(path, GetAssName(projectfilepath));
                        yield return path;
                    }
                }
                else
                {
                    // Assume VC default: $(SolutionDir)$(Configuration)\
                    var path = Path.Combine(SolutionDir + Path.DirectorySeparatorChar, config, GetAssName(projectfilepath));
                    yield return path;
                }
            }
        }

        static bool MatchCondition(XAttribute xattr, string config)
        {
            if (xattr == null)
            {
                return false;
            }

            var condition = xattr.Value;
            var pos = condition.IndexOf("==");
            if (pos >= 0)
            {
                var conditionvalues = condition[(pos + 2)..].Trim().Trim('\'').Split('|');
                if (conditionvalues.Any(c => c.Trim() == config))
                {
                    return true;
                }
            }
            return false;
        }

        static string GetAssName(string projectfilepath)
        {
            // todo: 1. read assemby name tag if exists. 2. check if exe/dll? 3. more?
            return Path.GetFileNameWithoutExtension(projectfilepath) + ".exe";
        }
    }
}
