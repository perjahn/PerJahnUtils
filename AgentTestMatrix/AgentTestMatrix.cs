using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

class AgentTestMatrix
{
    class test
    {
        public string agentname { get; set; }
        public string buildid { get; set; }
        public string testname { get; set; }
        public string status { get; set; }
        public bool? muted { get; set; }
        public string buildstart { get; set; }
    }

    static int Main(string[] args)
    {
        int result = 0;

        bool excludeMuted = args.Any(a => a == "-excludemuted");
        string[] argsWithoutFlags = args.Where(a => a != "-excludemuted").ToArray();

        if (argsWithoutFlags.Length != 3 && argsWithoutFlags.Length != 4)
        {
            Console.WriteLine(
@"AgentTestMatrix 1.0 - For each build agent, retrieves the latest test result, shows all failing tests in matrix.

Usage: AgentTestMatrix.exe [-excludemuted] <server> <buildconfig> <outfile> [excludagents]

Optional environment variables:
TestUsername
TestPassword
TestAgentPrefixFrom
TestAgentPrefixTo
TEAMCITY_BUILD_PROPERTIES_FILE
TestDebug");
            result = 1;
        }
        else
        {
            try
            {
                GetTests(argsWithoutFlags, excludeMuted);
            }
            catch (ApplicationException ex)
            {
                LogColor(ex.Message, ConsoleColor.Red);
                result = 1;
            }
            catch (Exception ex)
            {
                LogColor(ex.ToString(), ConsoleColor.Red);
                result = 1;
            }
        }

        if (Environment.UserInteractive)
        {
            Console.WriteLine("Press any key to continue...");
            Console.ReadKey();
        }

        return result;
    }

    static void GetTests(string[] argsWithoutFlags, bool excludeMuted)
    {
        string server = argsWithoutFlags[0];
        string buildconfig = argsWithoutFlags[1];
        string outfile = argsWithoutFlags[2];
        string excludeagents = argsWithoutFlags.Length < 4 ? null : argsWithoutFlags[3];

        string username, password;
        GetCredentials(out username, out password);

        string[] prefixFrom, prefixTo;
        GetAgentPrefixes(out prefixFrom, out prefixTo);


        List<test> tests = GetTests(server, username, password, buildconfig);

        if (excludeagents != null)
        {
            tests = tests.Where(t => !excludeagents.Split(',').Contains(t.agentname)).ToList();
        }

        if (!string.IsNullOrEmpty(Environment.GetEnvironmentVariable("TestDebug")))
        {
            string[] header = { "buildid\tagentname\ttestname\tstatus\tmuted" };

            File.WriteAllLines("TestDebug4.txt", Enumerable.Concat(header, tests.Select(t => $"{t.buildid}\t{t.agentname}\t{t.testname}\t{t.status}\t{t.muted}")));
        }

        string[] builds = tests.GroupBy(t => t.buildid).Select(t => t.Key).ToArray();
        string[] agents = tests.GroupBy(t => t.agentname).Select(a => a.Key).ToArray();

        test[] failedTests = tests.Where(t => t.status == "FAILURE" && (!excludeMuted || !t.muted.HasValue || !t.muted.Value)).ToArray();
        string[] failedBuilds = failedTests.GroupBy(t => t.buildid).Select(a => a.Key).ToArray();
        string[] failedAgents = failedTests.GroupBy(t => t.agentname).Select(a => a.Key).ToArray();

        Log($"Found {tests.Count} tests, of which {failedTests.Length} failed, in {builds.Length} builds containing tests, on {agents.Length} build agents.");


        Dictionary<string, List<test>> failedAgentTests = new Dictionary<string, List<test>>();

        foreach (string build in failedBuilds)
        {
            string agent = failedTests.Where(t => t.buildid == build).First().agentname;

            if (!failedAgentTests.ContainsKey(agent))
            {
                failedAgentTests[agent] = failedTests.Where(t => t.buildid == build).ToList();
            }
        }

        string testmatrix = PrintFailMatrix(failedAgents, failedAgentTests, prefixFrom, prefixTo);

        WriteHtml(testmatrix, outfile);
    }

    static void GetCredentials(out string username, out string password)
    {
        username = Environment.GetEnvironmentVariable("TestUsername");
        password = Environment.GetEnvironmentVariable("TestPassword");

        if (username != null)
        {
            Log("Got username from environment variable.");
        }
        if (password != null)
        {
            Log("Got password from environment variable.");
        }

        if (username == null || password == null)
        {
            Dictionary<string, string> tcvariables = GetTeamcityVariables();

            if (username == null && tcvariables.ContainsKey("teamcity.auth.userId"))
            {
                username = tcvariables["teamcity.auth.userId"];
                Log("Got username from Teamcity.");
            }
            if (password == null && tcvariables.ContainsKey("teamcity.auth.password"))
            {
                password = tcvariables["teamcity.auth.password"];
                Log("Got password from Teamcity.");
            }
        }

        if (username == null)
        {
            Log("No username specified.");
        }
        if (password == null)
        {
            Log("No password specified.");
        }
    }

    static void GetAgentPrefixes(out string[] prefixesFrom, out string[] prefixesTo)
    {
        string testAgentPrefixFrom = Environment.GetEnvironmentVariable("TestAgentPrefixFrom");
        string testAgentPrefixTo = Environment.GetEnvironmentVariable("TestAgentPrefixTo");

        if (testAgentPrefixFrom == null || testAgentPrefixTo == null)
        {
            Log("No valid agent prefixes specified.");
            prefixesFrom = new string[] { };
            prefixesTo = new string[] { };
        }
        else
        {
            prefixesFrom = testAgentPrefixFrom.Split(',');
            prefixesTo = testAgentPrefixTo.Split(',');
            if (prefixesFrom.Length != prefixesTo.Length)
            {
                string message = $"Number of TestAgentPrefixFrom ({prefixesFrom.Length}) didn't match number of TestAgentPrefixTo ({prefixesTo.Length}).";
                throw new ApplicationException(message);
            }

            Log($"Agent prefixes: {string.Join(", ", prefixesFrom.Zip(prefixesTo, (from, to) => $"'{from}' -> '{to}'"))}.");
        }
    }

    static Dictionary<string, string> GetTeamcityVariables()
    {
        string buildpropfile = Environment.GetEnvironmentVariable("TEAMCITY_BUILD_PROPERTIES_FILE");
        if (string.IsNullOrEmpty(buildpropfile))
        {
            Log("Couldn't find Teamcity build properties file.");
            return new Dictionary<string, string>();
        }
        if (!File.Exists(buildpropfile))
        {
            Log($"Couldn't find Teamcity build properties file: '{buildpropfile}'");
            return new Dictionary<string, string>();
        }

        Log($"Reading Teamcity build properties file: '{buildpropfile}'");
        string[] rows = File.ReadAllLines(buildpropfile);

        var valuesBuild = GetPropValues(rows);

        string configpropfile = valuesBuild["teamcity.configuration.properties.file"];
        if (string.IsNullOrEmpty(configpropfile))
        {
            Log("Couldn't find Teamcity config properties file.");
            return new Dictionary<string, string>();
        }
        if (!File.Exists(configpropfile))
        {
            Log($"Couldn't find Teamcity config properties file: '{configpropfile}'");
            return new Dictionary<string, string>();
        }

        Log($"Reading Teamcity config properties file: '{configpropfile}'");
        rows = File.ReadAllLines(configpropfile);

        var valuesConfig = GetPropValues(rows);

        return valuesConfig;
    }

    static Dictionary<string, string> GetPropValues(string[] rows)
    {
        Dictionary<string, string> dic = new Dictionary<string, string>();

        foreach (string row in rows)
        {
            int index = row.IndexOf('=');
            if (index != -1)
            {
                string key = row.Substring(0, index);
                string value = Regex.Unescape(row.Substring(index + 1));
                dic[key] = value;
            }
        }

        return dic;
    }

    static List<test> GetTests(string server, string username, string password, string buildconfig)
    {
        List<test> tests = new List<test>();

        using (WebClient client = new WebClient())
        {
            if (username != null && password != null)
            {
                string credentials = Convert.ToBase64String(Encoding.ASCII.GetBytes(username + ":" + password));
                client.Headers[HttpRequestHeader.Authorization] = "Basic " + credentials;
            }

            string address = $"http://{server}/app/rest/builds?locator=buildType:{buildconfig}";
            client.Headers["Accept"] = "application/json";

            dynamic builds = DownloadJsonContent(client, address, "TestDebug1.txt");

            foreach (JProperty property in builds)
            {
                if (property.First.Type == JTokenType.Array)
                {
                    foreach (dynamic build in property.First)
                    {
                        string buildhref = build.href;

                        address = $"http://{server}{buildhref}";
                        client.Headers["Accept"] = "application/json";

                        dynamic buildresult = DownloadJsonContent(client, address, "TestDebug2.txt");

                        string agentname = buildresult.agent.name;

                        if (buildresult != null && buildresult.testOccurrences != null && buildresult.testOccurrences.href != null)
                        {
                            string testhref = buildresult.testOccurrences.href;

                            address = $"http://{server}{testhref},count:10000";
                            client.Headers["Accept"] = "application/json";

                            dynamic testresults = DownloadJsonContent(client, address, "TestDebug3.txt");

                            foreach (dynamic testOccurrence in testresults.testOccurrence)
                            {
                                tests.Add(new test
                                {
                                    agentname = agentname,
                                    buildid = buildresult.id,
                                    testname = testOccurrence.name,
                                    status = testOccurrence.status,
                                    muted = testOccurrence.muted,
                                    buildstart = buildresult.startDate
                                });
                            }
                        }
                    }
                }
            }
        }

        return tests;
    }

    static Dictionary<string, bool> writtenLogs = new Dictionary<string, bool>();

    static JObject DownloadJsonContent(WebClient client, string address, string debugFilename)
    {
        Log($"Address: '{address}'");
        try
        {
            string content = client.DownloadString(address);
            if (!string.IsNullOrEmpty(Environment.GetEnvironmentVariable("TestDebug")))
            {
                if (!writtenLogs.ContainsKey(debugFilename))
                {
                    File.WriteAllText(debugFilename, content);
                    writtenLogs[debugFilename] = true;
                }
            }
            JObject jobects = JObject.Parse(content);
            return jobects;
        }
        catch (WebException ex)
        {
            throw new ApplicationException(ex.Message);
        }
    }

    static string PrintFailMatrix(string[] failedAgents, Dictionary<string, List<test>> failedAgentTests, string[] prefixFrom, string[] prefixTo)
    {
        string[] testnames = failedAgentTests
            .SelectMany(a => a.Value)
            .Select(t => t.testname)
            .Distinct()
            .OrderBy(t => t)
            .ToArray();

        StringBuilder sb = new StringBuilder();

        sb.Append("Test/Agent");
        foreach (string agentname in failedAgents.OrderBy(a => a))
        {
            string agentname2 = agentname;

            for (int i = 0; i < prefixFrom.Length && i < prefixTo.Length; i++)
            {
                if (agentname2.StartsWith(prefixFrom[i]))
                {
                    string datestring = failedAgentTests.Where(t => t.Value.First().agentname == agentname).Select(t => t.Value.First().buildstart).First();
                    DateTime datetime = DateTime.ParseExact(datestring, "yyyyMMddTHHmmss+ffff", CultureInfo.InvariantCulture, DateTimeStyles.None);

                    agentname2 = $"{prefixTo[i]}{agentname2.Substring(prefixFrom[i].Length)} ({agentname2},{datetime})";
                    break;
                }
            }

            sb.Append($"\t{agentname2}");
        }

        sb.AppendLine();

        foreach (string testname in testnames.OrderBy(t => t))
        {
            sb.Append(testname);

            foreach (string agentname in failedAgents.OrderBy(a => a))
            {
                if (failedAgentTests[agentname].Any(t => t.testname == testname))
                {
                    sb.Append("\tx");
                }
                else
                {
                    sb.Append("\t.");
                }
            }

            sb.AppendLine();
        }

        sb.AppendLine($"{testnames.Length} fails\t{string.Join("\t", failedAgentTests.OrderBy(t => t.Value.First().agentname).Select(t => t.Value.Count))}");


        return sb.ToString();
    }

    static void WriteHtml(string tablecontent, string filename)
    {
        StringBuilder sb = new StringBuilder();

        tablecontent = tablecontent.Replace("\r", "");

        string[] rows = tablecontent.Replace("\r", "").Split(new[] { '\n' }, StringSplitOptions.RemoveEmptyEntries);

        sb.AppendLine("<html>");
        sb.AppendLine("<body>");
        sb.AppendLine("<style>");
        sb.AppendLine(
@"body {
	font-family: Verdana, Arial, Helvetica, sans-serif;
	font-size: 12px;
}
table {
	border-collapse: collapse;
}
td {
	font-family: Verdana, Arial, Helvetica, sans-serif;
	font-size: 12px;
	padding: 1px 5px 1px 5px;
	vertical-align: top; 
	border: solid 1px black;
	white-space: nowrap;
}");
        sb.AppendLine("</style>");
        sb.AppendLine("</head>");
        sb.AppendLine("<body>");
        sb.AppendLine("<table border=\"1\">");

        foreach (string row in rows)
        {
            sb.Append("<tr>");

            string[] values = row.Split('\t');
            foreach (string value in values)
            {
                int start = value.IndexOf('(');
                int end = value.IndexOf(')');
                if (start != -1 && end != -1 && start < end)
                {
                    string title = value.Substring(start + 1, end - start - 1).Replace(",", "\n");
                    string cleanvalue = value.Substring(0, start - 1);
                    sb.Append($"<td title='{title}'>{cleanvalue}</td>");
                }
                else
                {
                    sb.Append($"<td>{value}</td>");
                }
            }

            sb.AppendLine("</tr>");
        }

        sb.AppendLine("</table>");
        sb.AppendLine("</body>");
        sb.AppendLine("</html>");

        File.WriteAllText(filename, sb.ToString());
    }

    private static void LogColor(string message, ConsoleColor color)
    {
        ConsoleColor oldColor = Console.ForegroundColor;
        try
        {
            Console.ForegroundColor = color;
            Log(message);
        }
        finally
        {
            Console.ForegroundColor = oldColor;
        }
    }

    private static void Log(string message)
    {
        string hostname = Dns.GetHostName();
        Console.WriteLine($"{hostname}: {message}");
    }
}

//return Program.Main(Environment.GetCommandLineArgs().Skip(2).ToArray());
