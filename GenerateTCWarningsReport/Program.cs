using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace GenerateTCWarningsReport
{
    class Program
    {
        static void Main(string[] args)
        {
            if (args.Length != 4)
            {
                Console.WriteLine(
@"Usage: GenerateTCWarningsReport <infile> <rawoutfile> <reportfolder> <tcserver>

Example: GenerateTCWarningsReport BuildLogFilename.txt BuildWarnings.txt BuildWarningReport tc");

                return;
            }

            GenereateReport(args[0], args[1], args[2], args[3]);
        }

        static void GenereateReport(string BuildLogFilename, string RawOutputFile, string ReportFolder, string ServerUrl)
        {
            if (!(File.Exists(BuildLogFilename)))
            {
                Console.WriteLine("Compilation warnings file not found: '" + BuildLogFilename + "'");
                Console.WriteLine("This file should be written by VS file logger.");
                return;
            }

            if (!ServerUrl.StartsWith("http"))
            {
                ServerUrl = "http://" + ServerUrl;
            }

            // Every warning is written two times to the log by VS, selet unique warnings
            string[] wrows = File.ReadAllLines(BuildLogFilename);
            List<string> warnings = wrows
                .Where(w => Regex.IsMatch(w, "^.*warning CS.*$"))
                .Select(w => w.Trim().Replace(@"^\s*\d+>", ""))
                .Distinct()
                .OrderBy(w => w)
                .ToList();

            // raw output
            Console.WriteLine("MSBuild Warnings - " + warnings.Count + " warnings ===================================================");
            foreach (var warning in warnings)
            {
                Console.WriteLine(" * " + warning);
            }

            List<string> previousWarnings;

            string propfile = Environment.GetEnvironmentVariable("TEAMCITY_BUILD_PROPERTIES_FILE");
            if (string.IsNullOrEmpty(propfile) || !File.Exists(propfile))
            {
                previousWarnings = new List<string>();
                Console.WriteLine("Couldn't find Teamcity properties file.");
            }
            else
            {
                Console.WriteLine("Reading Teamcity properties file: '" + propfile + "'");
                string[] prows = File.ReadAllLines(propfile);

                string buildtypid = prows
                    .Where(p => p.StartsWith("teamcity.buildType.id="))
                    .Select(p => Regex.Replace(p.Substring(22), "\\.", "."))
                    .First();
                string username = prows
                    .Where(p => p.StartsWith("teamcity.auth.userId="))
                    .Select(p => Regex.Replace(p.Substring(21), "\\.", ""))
                    .First();
                string password = prows
                    .Where(p => p.StartsWith("teamcity.auth.password="))
                    .Select(p => p.Substring(23))
                    .First();

                string tcurl = ServerUrl + "/httpAuth/repository/download/" + buildtypid + "/.lastSuccessful/" + RawOutputFile;
                Console.WriteLine("Teamcity url: '" + tcurl + "'");

                using (var webclient = new WebClient())
                {
                    webclient.Credentials = new NetworkCredential(username, password);

                    try
                    {
                        string content = webclient.DownloadString(tcurl);

                        previousWarnings = content
                            .Trim()
                            .Split(new char[] { '\r', '\n' })
                            .Where(w => !string.IsNullOrEmpty(w))
                            .ToList();
                    }
                    catch (System.Exception ex)
                    {
                        previousWarnings = new List<string>();
                        Console.WriteLine(ex.Message);
                    }
                }
            }

            List<string> newwarnings = new List<string>();
            List<string> oldwarnings = new List<string>();

            foreach (string warning in warnings)
            {
                if (previousWarnings.Contains(warning))
                {
                    oldwarnings.Add(warning);
                }
                else
                {
                    newwarnings.Add(warning);
                }
            }

            // TeamCity output
            Console.WriteLine("##teamcity[buildStatus text='{build.status.text}, Build warnings: " + warnings.Count +
                " (+" + newwarnings.Count + "/-" + (previousWarnings.Count - oldwarnings.Count) + ")']");
            Console.WriteLine("##teamcity[buildStatisticValue key='buildWarnings' value='" + warnings.Count + "']");

            // file output
            Console.WriteLine("Writing to raw file");
            using (var sw = new StreamWriter(RawOutputFile))
            {
                foreach (var warning in warnings)
                {
                    sw.WriteLine(warning);
                }
            }

            // html report output
            if (!Directory.Exists(ReportFolder))
            {
                Directory.CreateDirectory(ReportFolder);
            }
            using (var sw = new StreamWriter(Path.Combine(ReportFolder + "index.html")))
            {
                sw.WriteLine("<html><head></head><body><h1>" + warnings.Count + " Build Warnings</h1>");

                if (newwarnings.Count > 0)
                {
                    sw.WriteLine("New warnings:<ul>");
                    foreach (var warning in newwarnings)
                    {
                        sw.WriteLine("<li style='color:red'>" + warning + "</li>");
                    }
                    sw.WriteLine("</ul>");
                }
                if (oldwarnings.Count > 0)
                {
                    sw.WriteLine("Old warnings:<ul>");
                    foreach (var warning in oldwarnings)
                    {
                        sw.WriteLine("<li>_</li>");
                    }
                    sw.WriteLine("</ul>");
                }
                sw.WriteLine("</body></html>");
            }
        }
    }
}
