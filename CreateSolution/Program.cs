﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Xml.Linq;

namespace CreateSolution
{
    class proj
    {
        public string path;
        public string name;
        public string guid;
        public string assemblyname;
        public List<string> targets;
    };

    class Program
    {
        enum VSVersion { VS2010, VS2012, VS2013 };

        static void Main(string[] args)
        {
            ParseArguments(args);
            if (Environment.UserInteractive)
            {
                Console.WriteLine("Press any key to continue...");
                Console.ReadKey();
            }
        }

        static void ParseArguments(string[] args)
        {
            // Make all string comparisons (and sort/order) invariant of current culture
            // Thus, solution output file is written in a consistent manner
            System.Threading.Thread.CurrentThread.CurrentCulture = System.Globalization.CultureInfo.InvariantCulture;

            string usage =
@"CreateSolution 1.5 - Creates VS solution file.

Usage: CreateSolution [-g] [-vX] <path> <solutionfile> [excludeprojs...]

-g: Generate global sections (autogenerated by VS, required by msbuild).
-v0: Generate VS2010 sln file.
-v2: Generate VS2012 sln file.
-v3: Generate VS2013 sln file (default).

Example: CreateSolution . all.sln myproj1 myproj2";

            bool generateGlobalSection = false;
            VSVersion vsVersion = VSVersion.VS2013;

            List<string> argsWithoutFlags = new List<string>();
            foreach (string arg in args)
            {
                switch (arg)
                {
                    case "-v0":
                        vsVersion = VSVersion.VS2010;
                        break;
                    case "-v2":
                        vsVersion = VSVersion.VS2012;
                        break;
                    case "-v3":
                        vsVersion = VSVersion.VS2013;
                        break;
                    case "-g":
                        generateGlobalSection = true;
                        break;
                    default:
                        argsWithoutFlags.Add(arg);
                        break;
                }
            }

            if (argsWithoutFlags.Count < 2)
            {
                Console.WriteLine(usage);
                return;
            }


            string path = argsWithoutFlags[0];
            string solutionfile = argsWithoutFlags[1];
            IEnumerable<string> excludeprojects = argsWithoutFlags.Skip(2);

            CreateSolution(path, solutionfile, generateGlobalSection, vsVersion, excludeprojects);
        }

        static void CreateSolution(string path, string solutionfile, bool generateGlobalSection, VSVersion vsVersion, IEnumerable<string> excludeprojects)
        {
            List<string> files;

            string[] exts = { ".csproj", ".vbproj", ".vcxproj", ".sqlproj", ".modelproj" };
            string[] typeguids = {
                "FAE04EC0-301F-11D3-BF4B-00C04F79EFBC",
                "F184B08F-C81C-45F6-A57F-5ABD9991F28F",
                "8BC9CEB8-8B4A-11D0-8D11-00A0C91BC942",
                "00D1A9C2-B5F0-4AF3-8072-F6C62B433612",
                "F088123C-0E9E-452A-89E6-6BA2F21D5CAC" };

            try
            {
                files = Directory.GetFiles(path, "*.*proj", SearchOption.AllDirectories)
                        .Where(filename => exts.Any(ext => Path.GetExtension(filename) == ext))
                        .ToList();
            }
            catch (DirectoryNotFoundException ex)
            {
                Console.WriteLine(ex.Message);
                return;
            }

            Console.WriteLine("Generating solution for: " + vsVersion.ToString());

            files.Sort();

            bool first = true;
            foreach (string filename in files.ToList())  // Create tmp list
            {
                if (excludeprojects.Any(p => exts.Any(ext => filename.EndsWith(@"\" + p + ext))))
                {
                    files.Remove(filename);
                    if (first)
                    {
                        Console.WriteLine("Excluding projects:");
                        first = false;
                    }
                    Console.WriteLine("  '" + filename + "'");
                }
            }


            IEnumerable<proj> projs = GetProjects(files, solutionfile);


            projs = RemoveDups(projs);


            int projcount = 0;

            StringBuilder sb = new StringBuilder();

            foreach (proj p in projs.OrderBy(p => p.name))
            {
                for (int i = 0; i < exts.Length; i++)
                {
                    if (Path.GetExtension(p.path) == exts[i])
                    {
                        sb.AppendLine(
                                "Project(\"{" + typeguids[i] + "}\") = \"" + p.name + "\", \"" + p.path + "\", \"" + p.guid + "\"" + Environment.NewLine +
                                "EndProject");
                    }
                }
                projcount++;
            }

            if (projcount == 0)
            {
                Console.WriteLine("Couldn't find any projects in: '" + path + "'");
                return;
            }


            if (generateGlobalSection)
            {
                sb.Append(GenerateGlobalSection(projs));
            }


            string s = sb.ToString();

            switch (vsVersion)
            {
                case VSVersion.VS2010:
                    s = "Microsoft Visual Studio Solution File, Format Version 11.00" + Environment.NewLine +
                        "# Visual Studio 2010" + Environment.NewLine +
                        s;
                    break;
                case VSVersion.VS2012:
                    s = "Microsoft Visual Studio Solution File, Format Version 12.00" + Environment.NewLine +
                        "# Visual Studio 2012" + Environment.NewLine +
                        s;
                    break;
                case VSVersion.VS2013:
                    s = "Microsoft Visual Studio Solution File, Format Version 12.00" + Environment.NewLine +
                        "# Visual Studio 2013" + Environment.NewLine +
                        s;
                    break;
            }

            Console.WriteLine("Writing " + projcount + " projects to " + solutionfile + ".");
            using (StreamWriter sw = new StreamWriter(solutionfile))
            {
                sw.Write(s);
            }


            return;
        }

        static IEnumerable<proj> GetProjects(List<string> files, string solutionfile)
        {
            foreach (string file in files)
            {
                proj newproj;

                try
                {
                    XDocument xdoc = XDocument.Load(file);
                    XNamespace ns = xdoc.Root.Name.Namespace;

                    newproj = new proj();

                    try
                    {
                        newproj.guid = xdoc.Element(ns + "Project").Elements(ns + "PropertyGroup").Elements(ns + "ProjectGuid").Single().Value.ToUpper();
                    }
                    catch (System.InvalidOperationException ex)
                    {
                        Console.WriteLine("Couldn't load project: '" + file + "': Not a valid cs/vb/vc/sql project file: Couldn't find one valid guid: " + ex.Message + ".");
                        continue;
                    }
                    catch (System.NullReferenceException ex)
                    {
                        Console.WriteLine("Couldn't load project: '" + file + "': Not a valid cs/vb/vc/sql project file: Couldn't find one valid guid: " + ex.Message + ".");
                        continue;
                    }

                    XElement xele = xdoc.Element(ns + "Project").Element(ns + "PropertyGroup").Element(ns + "AssemblyName");
                    newproj.assemblyname = (xele == null) ? null : xele.Value;

                    newproj.name = Path.GetFileNameWithoutExtension(file);

                    string file1 = Path.GetFullPath(solutionfile);
                    string file2 = Path.GetFullPath(file);

                    string file3 = GetRelativePath(file1, file2);

                    newproj.path = file3;

                    try
                    {
                        newproj.targets =
                                (from el in xdoc.Element(ns + "Project").Elements(ns + "PropertyGroup")
                                 where el.Attribute("Condition") != null
                                 orderby GetTarget(el.Attribute("Condition").Value)
                                 select GetTarget(el.Attribute("Condition").Value)).ToList();
                    }
                    catch (System.NullReferenceException)
                    {
                        // No targets found
                        newproj.targets = new List<string>();
                    }
                }
                catch (System.Exception ex)
                {
                    Console.WriteLine("Couldn't load project: '" + file + "': " + ex.ToString());
                    continue;
                }

                yield return newproj;
            }
        }

        private IEnumerable<proj> GetDuplicates(IEnumerable<proj> projs)
        {
            var results =
                    from proj a in projs
                    group a by a.guid into g
                    where g.Count() > 1
                    select g;

            foreach (var group in results)
            {
                foreach (var item in group)
                {
                    yield return item;
                }
            }
        }

        static IEnumerable<string> GetAllTargets(IEnumerable<proj> projs)
        {
            List<string> targets = new List<string>();

            foreach (proj p in projs)
            {
                foreach (string target in p.targets)
                {
                    targets.Add(target);
                }
            }

            IEnumerable<string> targets2 =
                    from t in targets
                    select t;

            List<string> targets3 = targets2.Distinct().ToList();

            List<string> targets4 = new List<string>();

            // Remove lowercase dups (keep most uppercased)
            foreach (string target in targets3)
            {
                bool add = true;
                foreach (string target2 in targets3)
                {
                    if (string.Compare(target, target2, StringComparison.InvariantCultureIgnoreCase) == 0 &&
                            string.Compare(target, target2) < 0)
                    {
                        add = false;
                        break;
                    }
                }
                if (add)
                {
                    targets4.Add(target);
                }
            }

            return targets4;
        }

        // " '$(Configuration)|$(Platform)' == 'Debug|AnyCPU' "
        // Debug
        static string GetTarget(string s)
        {
            int pos = s.IndexOf("==");
            if (pos < 0)
                throw new System.NullReferenceException();

            int pos2 = s.IndexOf("'", pos);
            if (pos2 < 0)
                throw new System.NullReferenceException();

            int pos3 = s.IndexOf("|", pos2);
            if (pos3 < 0)
                throw new System.NullReferenceException();

            return s.Substring(pos2 + 1, pos3 - pos2 - 1);
        }

        static IEnumerable<proj> RemoveDups(IEnumerable<proj> projs)
        {
            List<proj> projs2 = projs.ToList();

            RemoveByGuid(projs2);
            RemoveByAssemblyName(projs2);
            RemoveByFileName(projs2);

            foreach (proj p in projs2)
            {
                if (p != null)
                {
                    yield return p;
                }
            }
        }

        private static void RemoveByGuid(List<proj> projs2)
        {
            List<proj> projs3 = projs2.OrderBy(p => p.guid).ThenBy(p => p.path).ToList();  // Create tmp list
            bool first = true;
            foreach (proj p1 in projs3)
            {
                int count = 0;
                foreach (proj p2 in projs2)
                {
                    if (p1.guid == p2.guid)
                    {
                        count++;
                    }
                }

                if (count > 1)
                {
                    if (first)
                    {
                        Console.WriteLine("Removing duplicate projects (by guid):");
                        first = false;
                    }

                    foreach (proj p2 in projs3)
                    {
                        if (p1.guid == p2.guid)
                        {
                            Console.WriteLine("  Guid: '" + p2.guid + "', File: '" + p2.path + "', Project: '" + p2.name + "'.");
                            projs2.Remove(p2);
                        }
                    }
                }
            }
        }

        private static void RemoveByAssemblyName(List<proj> projs2)
        {
            List<proj> projs3 = projs2.OrderBy(p => p.assemblyname).ThenBy(p => p.path).ToList();  // Create tmp list
            bool first = true;
            foreach (proj p1 in projs3)
            {
                int count = 0;
                foreach (proj p2 in projs2)
                {
                    if (p1.assemblyname != null && p2.assemblyname != null && string.CompareOrdinal(p1.assemblyname, p2.assemblyname) == 0)
                    {
                        count++;
                    }
                }

                if (count > 1)
                {
                    if (first)
                    {
                        Console.WriteLine("Removing duplicate projects (by assembly name):");
                        first = false;
                    }

                    foreach (proj p2 in projs3)
                    {
                        if (p1.assemblyname != null && p2.assemblyname != null && string.CompareOrdinal(p1.assemblyname, p2.assemblyname) == 0)
                        {
                            Console.WriteLine("  Assemblyname: '" + p2.assemblyname + "', File: '" + p2.path + "', Project: '" + p2.name + "'.");
                            projs2.Remove(p2);
                        }
                    }
                }
            }
        }

        private static void RemoveByFileName(List<proj> projs2)
        {
            List<proj> projs3 = projs2.OrderBy(p => p.name).ThenBy(p => p.path).ToList();  // Create tmp list
            bool first = true;
            foreach (proj p1 in projs3)
            {
                int count = 0;
                foreach (proj p2 in projs2)
                {
                    if (string.CompareOrdinal(p1.name, p2.name) == 0)
                    {
                        count++;
                    }
                }

                if (count > 1)
                {
                    if (first)
                    {
                        Console.WriteLine("Removing duplicate projects (by file name):");
                        first = false;
                    }

                    foreach (proj p2 in projs3)
                    {
                        if (string.CompareOrdinal(p1.name, p2.name) == 0)
                        {
                            Console.WriteLine("  Project: '" + p2.name + "', File: '" + p2.path + "', Guid: '" + p2.guid + "'.");
                            projs2.Remove(p2);
                        }
                    }
                }
            }
        }

        static string GetRelativePath(string pathFrom, string pathTo)
        {
            string s = pathFrom;

            int pos = 0, dirs = 0;
            while (!pathTo.StartsWith(s) && s.Length > 0)
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

            string s2 = string.Join(string.Empty, Enumerable.Repeat(".." + Path.DirectorySeparatorChar, dirs).ToArray());
            string s3 = pathTo.Substring(pos + 1);
            string s4 = s2 + s3;

            return s4;
        }

        static string GenerateGlobalSection(IEnumerable<proj> projs)
        {
            // TODO: Analyze why this algorithm is different from VS,
            // it's noticeable on projects with more complex configs
            // than Debug & Release / AnyCPU.

            // A naive, simple algorithm was also wrong. :(

            StringBuilder sb = new StringBuilder();

            sb.AppendLine(
                    "Global" + Environment.NewLine +
                    "\tGlobalSection(SolutionConfigurationPlatforms) = preSolution");

            IEnumerable<string> targets = GetAllTargets(projs).OrderBy(t => t).Distinct();

            foreach (string target in targets)
            {
                sb.AppendLine(
                        "\t\t" + target + "|Any CPU = " + target + "|Any CPU" + Environment.NewLine +
                        "\t\t" + target + "|Mixed Platforms = " + target + "|Mixed Platforms" + Environment.NewLine +
                        "\t\t" + target + "|x86 = " + target + "|x86");
            }

            sb.AppendLine(
                    "\tEndGlobalSection" + Environment.NewLine +
                    "\tGlobalSection(ProjectConfigurationPlatforms) = postSolution");

            foreach (proj p in projs.OrderBy(p => p.name))
            {
                foreach (string target in p.targets)
                {
                    sb.AppendLine(
                            "\t\t" + p.guid + "." + target + "|Any CPU.ActiveCfg = " + target + "|Any CPU" + Environment.NewLine +
                            "\t\t" + p.guid + "." + target + "|Any CPU.Build.0 = " + target + "|Any CPU" + Environment.NewLine +
                            "\t\t" + p.guid + "." + target + "|Mixed Platforms.ActiveCfg = " + target + "|Any CPU" + Environment.NewLine +
                            "\t\t" + p.guid + "." + target + "|Mixed Platforms.Build.0 = " + target + "|Any CPU" + Environment.NewLine +
                            "\t\t" + p.guid + "." + target + "|x86.ActiveCfg = " + target + "|Any CPU");
                }
            }

            sb.AppendLine(
                    "\tEndGlobalSection" + Environment.NewLine +
                    "\tGlobalSection(SolutionProperties) = preSolution" + Environment.NewLine +
                    "\t\tHideSolutionNode = FALSE" + Environment.NewLine +
                    "\tEndGlobalSection" + Environment.NewLine +
                    "EndGlobal");

            return sb.ToString();
        }
    }
}
