// Gather output files from compiled projects
// Wish list features:
//  * Different output folders for x86/x84/AnyCPU
//  * Fallback for vc2008 projects (simple search for exe)

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
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
            string[] solutions = Directory.GetFiles(".", "*.sln", SearchOption.AllDirectories);

            IEnumerable<string> paths = ParseSolutionFiles(solutions, config);

            if (!Directory.Exists(outputpath))
            {
                Directory.CreateDirectory(outputpath);
            }

            foreach (string path in paths)
            {
                if (File.Exists(path))
                {
                    string path2 = Path.Combine(outputpath, Path.GetFileName(path));
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
                    ConsoleColor old = Console.ForegroundColor;
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine($"'{path}'");
                    Console.ForegroundColor = old;
                }
            }
        }

        static IEnumerable<string> ParseSolutionFiles(string[] solutions, string config)
        {
            foreach (string solutionfile in solutions)
            {
                //Console.WriteLine($"solution: '{solutionfile}'");

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

                foreach (string row in rows)
                {
                    // Project("{FAE04EC0-301F-11D3-BF4B-00C04F79EFBC}") = "MyCsProject", "Folder\Folder\MyCsProject.csproj", "{01010101-0101-0101-0101-010101010101}"
                    // Project("{F184B08F-C81C-45F6-A57F-5ABD9991F28F}") = "MyVbProject", "Folder\Folder\MyVbProject.vbproj", "{02020202-0202-0202-0202-020202020202}"
                    // Project("{8BC9CEB8-8B4A-11D0-8D11-00A0C91BC942}") = "MyVcProject", "Folder\Folder\MyVcProject.vcxproj", "{03030303-0303-0303-0303-030303030303}"

                    string[] projtypeguids = {
                        "{FAE04EC0-301F-11D3-BF4B-00C04F79EFBC}",
                        "{F184B08F-C81C-45F6-A57F-5ABD9991F28F}",
                        "{8BC9CEB8-8B4A-11D0-8D11-00A0C91BC942}" };

                    foreach (string projtypeguid in projtypeguids)
                    {
                        string projtypeline = $"Project(\"{projtypeguid}\") =";

                        if (row.StartsWith(projtypeline))
                        {
                            string[] values = row.Substring(projtypeline.Length).Split(',');
                            if (values.Length != 3)
                            {
                                continue;
                            }

                            string projectfilepath = values[1].Trim().Trim('"');

                            IEnumerable<string> paths = ParseProjectFile(solutionfile, projectfilepath, config);

                            foreach (string path in paths)
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
            XNamespace ns;

            string SolutionDir = Path.GetDirectoryName(solutionfile);
            string filename = Path.Combine(SolutionDir, projectfilepath);

            try
            {
                xdoc = XDocument.Load(filename);
            }
            catch (IOException ex)
            {
                Console.WriteLine($"Couldn't load project: '{filename}': {ex.Message}");
                yield break;
            }
            catch (System.Xml.XmlException ex)
            {
                Console.WriteLine($"Couldn't load project: '{filename}': {ex.Message}");
                yield break;
            }

            ns = xdoc.Root.Name.Namespace;

            // VC++ 2008 projects has obsolete xml schema - no outputpaths exists.

            // When msbuild builds have multiple matching configs with the same name,
            //   VS chooce one at random: (Release|x86, Release|AnyCPU).
            // We have to check all (distinct) output paths for built assembly files.

            // VC: Uses OutDir tag instead, but is usually missing default, and that means
            //   = $(SolutionDir)$(Configuration)\ -> no bin folder
            // OutDir can also also be used on CS/VB projects - but is only used if
            //   OutputPath is missing.

            IEnumerable<string> outputpaths =
                    (from el in xdoc.Elements(ns + "Project").Elements(ns + "PropertyGroup").Elements(ns + "OutputPath")
                     where MatchCondition(el.Parent.Attribute("Condition"), config)
                     select el.Value)
                    .Distinct();

            if (outputpaths.Any())
            {
                // OutputPath>bin\Release\<

                foreach (string outputpath in outputpaths)
                {
                    string path = Path.Combine(
                            SolutionDir,
                            Path.GetDirectoryName(projectfilepath),
                            outputpath,
                            GetAssName(projectfilepath));

                    //Console.WriteLine($"1: '{path}'");
                    yield return path;
                }
            }
            else
            {
                IEnumerable<string> outdirs =
                        (from el in xdoc.Elements(ns + "Project").Elements(ns + "PropertyGroup").Elements(ns + "OutDir")
                         where MatchCondition(el.Parent.Attribute("Condition"), config)
                         select el.Value)
                        .Distinct();

                if (outdirs.Any())
                {
                    // OutDir>$(SolutionDir)$(Configuration)\mysubdir<

                    foreach (string outdir in outdirs)
                    {
                        string path = outdir
                                .Replace("$(SolutionDir)", SolutionDir + Path.DirectorySeparatorChar)
                                .Replace("$(Configuration)", config);

                        path = Path.Combine(path, GetAssName(projectfilepath));

                        //Console.WriteLine($"2: '{path}'");
                        yield return path;
                    }
                }
                else
                {
                    // Assume VC default: $(SolutionDir)$(Configuration)\
                    string path = Path.Combine(
                            SolutionDir + Path.DirectorySeparatorChar,
                            config,
                            GetAssName(projectfilepath));
                    //Console.WriteLine($"3: '{path}'");
                    yield return path;
                }
            }
        }

        static bool MatchCondition(XAttribute xattr, string config)
        {
            if (xattr == null)
                return false;

            string condition = xattr.Value;
            int pos = condition.IndexOf("==");
            if (pos >= 0)
            {
                string[] conditionvalues = condition.Substring(pos + 2).Trim().Trim('\'').Split('|');
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
