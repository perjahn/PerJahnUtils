﻿using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Xml.Linq;

namespace CreateSolution
{
    class Proj
    {
        public string path;
        public string name;
        public string guid;
        public string assemblyname;
        public List<string> targets;
    };

    class Program
    {
        enum VSVersion { VS2010, VS2012, VS2013, VS2015, VS2017 };

        static void Main(string[] args)
        {
            ParseArguments(args);
            if (Environment.UserInteractive && string.IsNullOrEmpty(Environment.GetEnvironmentVariable("DontPrompt")))
            {
                Console.WriteLine("Press any key to continue...");
                Console.ReadKey();
            }
        }

        static void ParseArguments(string[] args)
        {
            // Make all string comparisons (and sort/order) invariant of current culture
            // Thus, solution output file is written in a consistent manner
            Thread.CurrentThread.CurrentCulture = CultureInfo.InvariantCulture;

            string usage =
@"CreateSolution 2.2 - Creates VS solution file.

Usage: CreateSolution [-g] [-vX] [-wWebSiteFolder] <path> <solutionfile> [excludeprojs...]

-g: Generate global sections (autogenerated by VS, required by msbuild).
-v0: Generate VS2010 sln file.
-v2: Generate VS2012 sln file.
-v3: Generate VS2013 sln file.
-v5: Generate VS2015 sln file.
-v7: Generate VS2017 sln file (default).

Example: CreateSolution -wSites\WebSite1 -wSites\WebSite2 . all.sln myproj1 myproj2";

            bool generateGlobalSection = false;
            VSVersion vsVersion = VSVersion.VS2017;
            List<string> websiteFolders = new List<string>();

            List<string> argsWithoutFlags = new List<string>();
            foreach (string arg in args)
            {
                switch (arg)
                {
                    case "-g":
                        generateGlobalSection = true;
                        break;
                    case "-v0":
                        vsVersion = VSVersion.VS2010;
                        break;
                    case "-v2":
                        vsVersion = VSVersion.VS2012;
                        break;
                    case "-v3":
                        vsVersion = VSVersion.VS2013;
                        break;
                    case "-v5":
                        vsVersion = VSVersion.VS2015;
                        break;
                    case "-v7":
                        vsVersion = VSVersion.VS2017;
                        break;
                    default:
                        if (arg.StartsWith("-w"))
                        {
                            websiteFolders.Add(arg.Substring(2));
                        }
                        else
                        {
                            argsWithoutFlags.Add(arg);
                        }
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
            List<string> excludeProjects = argsWithoutFlags.Skip(2).ToList();

            CreateSolution(path, solutionfile, generateGlobalSection, vsVersion, excludeProjects, websiteFolders);
        }

        static void CreateSolution(string path, string solutionfile, bool generateGlobalSection, VSVersion vsVersion, List<string> excludeProjects, List<string> websiteFolders)
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

            foreach (string websiteFolder in websiteFolders)
            {
                if (!Directory.Exists(websiteFolder))
                {
                    Console.WriteLine($"Website folder doesn't exist: '{websiteFolder}'");
                    return;
                }
            }

            Console.WriteLine($"Generating solution for: {vsVersion.ToString()}");

            files.Sort();

            bool first = true;
            foreach (string filename in files.ToList())  // Create tmp list
            {
                if (excludeProjects.Any(p => exts.Any(ext => filename.EndsWith($"\\{p}{ext}"))))
                {
                    files.Remove(filename);
                    if (first)
                    {
                        Console.WriteLine("Excluding projects:");
                        first = false;
                    }
                    Console.WriteLine($"  '{filename}'");
                }
            }


            List<Proj> projs = GetProjects(files, solutionfile);


            projs = RemoveDups(projs);


            int projcount = 0;

            var sb = new StringBuilder();

            foreach (var p in projs.OrderBy(p => p.name))
            {
                for (int i = 0; i < exts.Length; i++)
                {
                    if (Path.GetExtension(p.path) == exts[i])
                    {
                        sb.AppendLine("Project(\"{" + typeguids[i] + "}\") = \"" + $"{p.name}\", \"{p.path}\", \"{p.guid}\"{Environment.NewLine}EndProject");
                    }
                }
                projcount++;
            }


            foreach (string websiteFolder in websiteFolders)
            {
                Random r = new Random();
                int port = r.Next(1024, 65535);

                sb.AppendLine(
                    "Project(\"{E24C65DC-7377-472B-9ABA-BC803B73C61A}\") = \"" + $"{Path.GetFileName(websiteFolder)}\", \"http://localhost:{port}\", \"" + "{" + Guid.NewGuid().ToString().ToUpper() + "}" + $"\"{Environment.NewLine}" +
                    $"\tProjectSection(WebsiteProperties) = preProject{Environment.NewLine}" +
                    $"\t\tSlnRelativePath = \"{websiteFolder}\\\"{Environment.NewLine}" +
                    $"\tEndProjectSection{Environment.NewLine}" +
                    $"EndProject");
            }



            if (projcount == 0)
            {
                Console.WriteLine($"Couldn't find any projects in: '{path}'");
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
                    s = $"Microsoft Visual Studio Solution File, Format Version 11.00{Environment.NewLine}# Visual Studio 2010{Environment.NewLine}{s}";
                    break;
                case VSVersion.VS2012:
                    s = $"Microsoft Visual Studio Solution File, Format Version 12.00{Environment.NewLine}# Visual Studio 2012{Environment.NewLine}{s}";
                    break;
                case VSVersion.VS2013:
                    s = $"Microsoft Visual Studio Solution File, Format Version 12.00{Environment.NewLine}# Visual Studio 2013{Environment.NewLine}{s}";
                    break;
                case VSVersion.VS2015:
                    s = $"Microsoft Visual Studio Solution File, Format Version 12.00{Environment.NewLine}# Visual Studio 14{Environment.NewLine}{s}";
                    break;
                case VSVersion.VS2017:
                    s = $"Microsoft Visual Studio Solution File, Format Version 12.00{Environment.NewLine}# Visual Studio 15{Environment.NewLine}{s}";
                    break;
            }

            Console.WriteLine($"Writing {projcount} projects to {solutionfile}.");
            using (StreamWriter sw = new StreamWriter(solutionfile))
            {
                sw.Write(s);
            }


            return;
        }

        static List<Proj> GetProjects(List<string> files, string solutionfile)
        {
            List<Proj> projects = new List<Proj>();

            foreach (string file in files)
            {
                Proj newproj;

                try
                {
                    XDocument xdoc = XDocument.Load(file);
                    XNamespace ns = xdoc.Root.Name.Namespace;

                    newproj = new Proj();

                    var guidelement = xdoc.Element(ns + "Project").Elements(ns + "PropertyGroup").Elements(ns + "ProjectGuid").FirstOrDefault();
                    if (guidelement == null)
                    {
                        newproj.guid = Guid.NewGuid().ToString();
                    }
                    else
                    {
                        newproj.guid = guidelement.Value.ToUpper();
                    }

                    XElement xele = xdoc.Element(ns + "Project").Element(ns + "PropertyGroup").Element(ns + "AssemblyName");
                    newproj.assemblyname = xele?.Value;

                    newproj.name = Path.GetFileNameWithoutExtension(file);

                    string file1 = Path.GetFullPath(solutionfile);
                    string file2 = Path.GetFullPath(file);

                    string file3 = GetRelativePath(file1, file2);

                    newproj.path = file3;

                    try
                    {
                        newproj.targets =
                            xdoc.Element(ns + "Project").Elements(ns + "PropertyGroup")
                                .Where(el => el.Attribute("Condition") != null)
                                .OrderBy(el => GetTarget(el.Attribute("Condition").Value))
                                .Select(el => GetTarget(el.Attribute("Condition").Value))
                                .ToList();
                    }
                    catch (NullReferenceException)
                    {
                        // No targets found
                        newproj.targets = new List<string>();
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Couldn't load project: '{file}': {ex.ToString()}");
                    continue;
                }

                projects.Add(newproj);
            }

            return projects;
        }

        private List<Proj> GetDuplicates(List<Proj> projects)
        {
            List<Proj> dups = new List<Proj>();

            var results = projects
                .GroupBy(a => a.guid)
                .Where(g => g.Count() > 1);

            foreach (var group in results)
            {
                foreach (var item in group)
                {
                    dups.Add(item);
                }
            }

            return dups;
        }

        static List<string> GetAllTargets(List<Proj> projs)
        {
            var targets = projs
                .SelectMany(t => t.targets)
                .Distinct()
                .ToList();

            // Remove lowercase dups (keep most uppercased)
            return targets
                .Where(t => !targets
                    .Any(tt => string.Compare(t, tt, StringComparison.InvariantCultureIgnoreCase) == 0 && string.Compare(t, tt) < 0))
                .ToList();
        }

        // " '$(Configuration)|$(Platform)' == 'Debug|AnyCPU' "
        // Debug
        static string GetTarget(string s)
        {
            int pos = s.IndexOf("==");
            if (pos < 0)
                throw new NullReferenceException();

            int pos2 = s.IndexOf("'", pos);
            if (pos2 < 0)
                throw new NullReferenceException();

            int pos3 = s.IndexOf("|", pos2);
            if (pos3 < 0)
                throw new NullReferenceException();

            return s.Substring(pos2 + 1, pos3 - pos2 - 1);
        }

        static List<Proj> RemoveDups(List<Proj> projs)
        {
            List<Proj> projs2 = projs.ToList();

            RemoveByGuid(projs2);
            RemoveByAssemblyName(projs2);
            RemoveByFileName(projs2);

            return projs2
                .Where(p => p != null)
                .ToList();
        }

        private static void RemoveByGuid(List<Proj> projs2)
        {
            List<Proj> projs3 = projs2.OrderBy(p => p.guid).ThenBy(p => p.path).ToList();  // Create tmp list
            bool first = true;
            foreach (var proj1 in projs3)
            {
                int count = 0;
                foreach (var proj2 in projs2)
                {
                    if (proj1.guid == proj2.guid)
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

                    foreach (var proj2 in projs3)
                    {
                        if (proj1.guid == proj2.guid)
                        {
                            Console.WriteLine($"  Guid: '{proj2.guid}', File: '{proj2.path}', Project: '{proj2.name}'.");
                            projs2.Remove(proj2);
                        }
                    }
                }
            }
        }

        private static void RemoveByAssemblyName(List<Proj> projs2)
        {
            List<Proj> projs3 = projs2.OrderBy(p => p.assemblyname).ThenBy(p => p.path).ToList();  // Create tmp list
            bool first = true;
            foreach (var proj1 in projs3)
            {
                int count = 0;
                foreach (var proj2 in projs2)
                {
                    if (proj1.assemblyname != null && proj2.assemblyname != null && string.CompareOrdinal(proj1.assemblyname, proj2.assemblyname) == 0)
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

                    foreach (var proj2 in projs3)
                    {
                        if (proj1.assemblyname != null && proj2.assemblyname != null && string.CompareOrdinal(proj1.assemblyname, proj2.assemblyname) == 0)
                        {
                            Console.WriteLine($"  Assemblyname: '{proj2.assemblyname}', File: '{proj2.path}', Project: '{proj2.name}'.");
                            projs2.Remove(proj2);
                        }
                    }
                }
            }
        }

        private static void RemoveByFileName(List<Proj> projs2)
        {
            List<Proj> projs3 = projs2.OrderBy(p => p.name).ThenBy(p => p.path).ToList();  // Create tmp list
            bool first = true;
            foreach (var proj1 in projs3)
            {
                int count = 0;
                foreach (var proj2 in projs2)
                {
                    if (string.CompareOrdinal(proj1.name, proj2.name) == 0)
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

                    foreach (var proj2 in projs3)
                    {
                        if (string.CompareOrdinal(proj1.name, proj2.name) == 0)
                        {
                            Console.WriteLine($"  Project: '{proj2.name}', File: '{proj2.path}', Guid: '{proj2.guid}'.");
                            projs2.Remove(proj2);
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

            string s2 = string.Join(string.Empty, Enumerable.Repeat($"..{Path.DirectorySeparatorChar}", dirs).ToArray());
            string s3 = pathTo.Substring(pos + 1);
            string s4 = s2 + s3;

            return s4;
        }

        static string GenerateGlobalSection(List<Proj> projs)
        {
            // TODO: Analyze why this algorithm is different from VS,
            // it's noticeable on projects with more complex configs
            // than Debug & Release / AnyCPU.

            // A naive, simple algorithm was also wrong. :(

            StringBuilder sb = new StringBuilder();

            sb.AppendLine(
                    $"Global{Environment.NewLine}" +
                    $"\tGlobalSection(SolutionConfigurationPlatforms) = preSolution");

            var targets = GetAllTargets(projs)
                .OrderBy(t => t)
                .Distinct();

            foreach (string target in targets)
            {
                sb.AppendLine(
                    $"\t\t{target}|Any CPU = {target}|Any CPU{Environment.NewLine}" +
                    $"\t\t{target}|Mixed Platforms = {target}|Mixed Platforms{Environment.NewLine}" +
                    $"\t\t{target}|x86 = {target}|x86");
            }

            sb.AppendLine(
                $"\tEndGlobalSection{Environment.NewLine}" +
                $"\tGlobalSection(ProjectConfigurationPlatforms) = postSolution");

            foreach (var p in projs.OrderBy(p => p.name))
            {
                foreach (string target in p.targets)
                {
                    sb.AppendLine(
                        $"\t\t{p.guid}.{target}|Any CPU.ActiveCfg = {target}|Any CPU{Environment.NewLine}" +
                        $"\t\t{p.guid}.{target}|Any CPU.Build.0 = {target}|Any CPU{Environment.NewLine}" +
                        $"\t\t{p.guid}.{target}|Mixed Platforms.ActiveCfg = {target}|Any CPU{Environment.NewLine}" +
                        $"\t\t{p.guid}.{target}|Mixed Platforms.Build.0 = {target}|Any CPU{Environment.NewLine}" +
                        $"\t\t{p.guid}.{target}|x86.ActiveCfg = {target}|Any CPU");
                }
            }

            sb.AppendLine(
                $"\tEndGlobalSection{Environment.NewLine}" +
                $"\tGlobalSection(SolutionProperties) = preSolution{Environment.NewLine}" +
                $"\t\tHideSolutionNode = FALSE{Environment.NewLine}" +
                $"\tEndGlobalSection{Environment.NewLine}" +
                $"EndGlobal");

            return sb.ToString();
        }
    }
}
