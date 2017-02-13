﻿using Newtonsoft.Json.Linq;
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
        if (args.Length != 1)
        {
            Console.WriteLine(
@"AgentTestMatrix 1.0 - For each build agent, retrieves the latest test result, shows all failing tests in matrix.

Usage: AgentTestMatrix.exe <outfile>

Environment variables:
TestServer
TestBuildconfig
TestUsername
TestPassword
TEAMCITY_BUILD_PROPERTIES_FILE (can retrieve the 4 above: Server, Buildconfig, Username, Password)

Optional environment variables:
TestExcludeMuted
TestAgentPrefixFrom
TestAgentPrefixTo
TestExcludeAgents
TestDebug");
            result = 1;
        }
        else
        {
            try
            {
                WriteTests(args[0]);
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

    static void WriteTests(string outfile)
    {
        string server = GetServer();
        string buildconfig = GetBuildconfig();
        bool excludeMuted = GetExcludeMuted();
        string[] excludeAgents = GetExcludeAgents();

        string username, password;
        GetCredentials(out username, out password);

        string[] prefixFrom, prefixTo;
        GetAgentPrefixes(out prefixFrom, out prefixTo);



        List<test> tests = GetTests(server, username, password, buildconfig);

        if (excludeAgents != null)
        {
            tests = tests.Where(t => !excludeAgents.Contains(t.agentname)).ToList();
        }

        if (!string.IsNullOrEmpty(Environment.GetEnvironmentVariable("TestDebug")))
        {
            string[] header = { "buildid\tagentname\ttestname\tstatus\tmuted\tbuildstart" };

            File.WriteAllLines("TestDebug4.txt", Enumerable.Concat(header, tests.Select(t => $"{t.buildid}\t{t.agentname}\t{t.testname}\t{t.status}\t{t.muted}\t{t.buildstart}")));
        }

        string[] builds = tests.GroupBy(t => t.buildid).Select(t => t.Key).ToArray();
        string[] agents = tests.GroupBy(t => t.agentname).Select(a => a.Key).ToArray();

        tests = tests.Where(t => !excludeMuted || !t.muted.HasValue || !t.muted.Value).ToList();

        Log($"Found {tests.Count} tests, of which {tests.Count(t => t.status == "FAILURE")} failed, in {builds.Length} builds containing tests, on {agents.Length} build agents.");


        Dictionary<string, List<test>> agentTests = new Dictionary<string, List<test>>();

        foreach (string build in builds)
        {
            string agent = tests.Where(t => t.buildid == build).First().agentname;

            if (!agentTests.ContainsKey(agent))
            {
                agentTests[agent] = tests.Where(t => t.buildid == build).ToList();
            }
        }

        string testmatrix = PrintFailMatrix(agents, agentTests, prefixFrom, prefixTo);

        WriteHtml(testmatrix, outfile);
    }

    static string GetServer()
    {
        string server = Environment.GetEnvironmentVariable("TestServer");

        if (server != null)
        {
            Log($"Got server from environment variable: '{server}'");
        }

        if (server == null)
        {
            Dictionary<string, string> tcvariables = GetTeamcityConfigVariables();

            if (server == null && tcvariables.ContainsKey("teamcity.serverUrl"))
            {
                server = tcvariables["teamcity.serverUrl"];
                Log($"Got server from Teamcity: '{server}'");
            }
        }

        if (server == null)
        {
            throw new ApplicationException("No server specified.");
        }
        else
        {
            if (!server.StartsWith("http://") && !server.StartsWith("https://"))
            {
                server = $"http://{server}";
            }
        }

        return server;
    }

    static string GetBuildconfig()
    {
        string buildconfig = Environment.GetEnvironmentVariable("TestBuildconfig");

        if (buildconfig != null)
        {
            Log($"Got buildconfig from environment variable: '{buildconfig}'");
        }

        if (buildconfig == null)
        {
            Dictionary<string, string> tcvariables = GetTeamcityBuildVariables();

            if (tcvariables.ContainsKey("teamcity.buildType.id"))
            {
                buildconfig = tcvariables["teamcity.buildType.id"];
                Log($"Got buildconfig from Teamcity: '{buildconfig}'");
            }
        }

        if (buildconfig == null)
        {
            throw new ApplicationException("No buildconfig specified.");
        }

        return buildconfig;
    }

    static bool GetExcludeMuted()
    {
        string excludeMuted = Environment.GetEnvironmentVariable("TestExcludeMuted");

        if (excludeMuted != null)
        {
            Log($"Got excludemuted from environment variable: '{excludeMuted}'");
        }
        else
        {
            Log("No excludemuted specified.");
        }

        return !string.IsNullOrEmpty(excludeMuted);
    }

    static string[] GetExcludeAgents()
    {
        string excludeAgents = Environment.GetEnvironmentVariable("TestExcludeAgents");

        if (excludeAgents != null)
        {
            Log($"Got excludeagents from environment variable: '{excludeAgents}'");
            return excludeAgents.Split(',');
        }
        else
        {
            Log("No excludeagents specified.");
        }

        return null;
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
            Dictionary<string, string> tcvariables = GetTeamcityBuildVariables();

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

    static Dictionary<string, string> GetTeamcityBuildVariables()
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

        if (!string.IsNullOrEmpty(Environment.GetEnvironmentVariable("TestDebug")))
        {
            LogTCSection("Teamcity Properties", valuesBuild.Select(p => $"Build: {p.Key}={p.Value}"));
        }

        return valuesBuild;
    }

    static Dictionary<string, string> GetTeamcityConfigVariables()
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

        if (!string.IsNullOrEmpty(Environment.GetEnvironmentVariable("TestDebug")))
        {
            LogTCSection("Teamcity Properties", valuesBuild.Select(p => $"Build: {p.Key}={p.Value}"));
        }

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

        if (!string.IsNullOrEmpty(Environment.GetEnvironmentVariable("TestDebug")))
        {
            LogTCSection("Teamcity Properties", valuesConfig.Select(p => $"Config: {p.Key}={p.Value}"));
        }

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

            string address = $"{server}/app/rest/builds?locator=buildType:{buildconfig}";
            client.Headers["Accept"] = "application/json";

            dynamic builds = DownloadJsonContent(client, address, "TestDebug1.txt");

            foreach (JProperty property in builds)
            {
                if (property.First.Type == JTokenType.Array)
                {
                    foreach (dynamic build in property.First)
                    {
                        string buildhref = build.href;

                        address = $"{server}{buildhref}";
                        client.Headers["Accept"] = "application/json";

                        dynamic buildresult = DownloadJsonContent(client, address, "TestDebug2.txt");

                        string agentname = buildresult.agent.name;

                        if (buildresult != null && buildresult.testOccurrences != null && buildresult.testOccurrences.href != null)
                        {
                            string testhref = buildresult.testOccurrences.href;

                            address = $"{server}{testhref},count:10000";
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

    static string PrintFailMatrix(string[] agents, Dictionary<string, List<test>> agentTests, string[] prefixFrom, string[] prefixTo)
    {
        string[] testnames = agentTests
            .SelectMany(a => a.Value)
            .Select(t => t.testname)
            .Distinct()
            .OrderBy(t => t)
            .ToArray();

        StringBuilder sb = new StringBuilder();

        DateTime now = DateTime.Now;

        sb.Append("Test/Agent");
        foreach (string agentname in agents.OrderBy(a => a))
        {
            string agentname2 = agentname;

            for (int i = 0; i < prefixFrom.Length && i < prefixTo.Length; i++)
            {
                if (agentname2.StartsWith(prefixFrom[i]))
                {
                    string datestring = agentTests.Where(t => t.Value.First().agentname == agentname).Select(t => t.Value.First().buildstart).First();
                    DateTime datetime = DateTime.ParseExact(datestring, "yyyyMMddTHHmmss+ffff", CultureInfo.InvariantCulture, DateTimeStyles.None);

                    agentname2 = $"{prefixTo[i]}{agentname2.Substring(prefixFrom[i].Length)}|{agentname2}|{datetime}|";
                    break;
                }
            }

            sb.Append($"\t{agentname2}");
        }

        sb.AppendLine();

        sb.Append("Hours since last run");
        foreach (string agentname in agents.OrderBy(a => a))
        {
            string datestring = agentTests.Where(t => t.Value.First().agentname == agentname).Select(t => t.Value.First().buildstart).First();
            DateTime datetime = DateTime.ParseExact(datestring, "yyyyMMddTHHmmss+ffff", CultureInfo.InvariantCulture, DateTimeStyles.None);
            string hourssince = (now - datetime).TotalHours.ToString("0", CultureInfo.InvariantCulture);

            sb.Append($"\t{hourssince}");
        }

        sb.AppendLine();

        int failcount = 0;

        int totalfailcount = 0;
        int totalsuccesscount = 0;
        int totalmissingcount = 0;

        foreach (string testname in testnames.OrderBy(t => t))
        {
            sb.Append(testname);

            bool failed = false;

            foreach (string agentname in agents.OrderBy(a => a))
            {
                if (agentTests[agentname].Any(t => t.testname == testname))
                {
                    if (agentTests[agentname].Any(t => t.testname == testname && t.status == "FAILURE"))
                    {
                        sb.Append("\tx");
                        totalfailcount++;
                        failed = true;
                    }
                    else
                    {
                        sb.Append("\t.");
                        totalsuccesscount++;
                    }
                }
                else
                {
                    sb.Append("\t-");
                    totalmissingcount++;
                }
            }

            if (failed)
            {
                failcount++;
            }

            sb.AppendLine();
        }

        string agentSums = string.Join("\t", agentTests.OrderBy(t => t.Value.First().agentname).Select(t => t.Value.Where(tt => tt.status == "FAILURE").Count()));

        sb.AppendLine($"{testnames.Length} tests, {failcount} failed, {testnames.Length - failcount} succeded (Total: {totalfailcount} failed, {totalsuccesscount} succeded, {totalmissingcount} missing)\t{agentSums}");


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
}
.fail {
	background-color: rgb(255,200,200);
}
.pass {
	background-color: rgb(200,255,200);
}
.missing {
	background-color: rgb(200,200,200);
}");
        sb.AppendLine("</style>");
        sb.AppendLine("<script src='http://code.jquery.com/jquery-latest.min.js'></script>");
        sb.AppendLine("<script>");
        sb.AppendLine(
@"$(document).ready(function(){$('#checkboxID').change(function(){
	var self = this;
	$('tr.successes').toggle(self.checked); 
}).change();});");
        sb.AppendLine("</script>");
        sb.AppendLine("</head>");
        sb.AppendLine("<body>");

        sb.AppendLine(@"<p><label for='checkboxID'>Show all tests</label><input type='checkbox' id='checkboxID' /></p>");
        sb.AppendLine("<p>x Test failed.<br/>");
        sb.AppendLine(". Test passed.<br/>");
        sb.AppendLine("- Test missing.</p>");
        sb.AppendLine("<p>Obvious thing you should know: There's a tooltip on column headers!</p>");

        sb.AppendLine("<table border=\"1\">");

        foreach (string row in rows)
        {
            string[] values = row.Split('\t');
            bool allsuccess = values.Skip(1).All(v => v == ".");
            bool anyfails = values.Skip(1).Any(v => v == "x");

            if (allsuccess)
            {
                sb.Append("<tr class='successes' style='display: none;'>");
            }
            if (anyfails)
            {
                sb.Append("<tr class='fails'>");
            }

            foreach (string value in values)
            {
                int start = value.IndexOf('|');
                int end = value.LastIndexOf('|');
                if (start != -1 && end != -1 && start < end)
                {
                    string title = value.Substring(start + 1, end - start - 1).Replace("|", "\n");
                    string cleanvalue = value.Substring(0, start);
                    sb.Append($"<td title='{title}'>{cleanvalue}</td>");
                }
                else
                {
                    if (value == "x")
                    {
                        sb.Append($"<td class='fail'>{value}</td>");
                    }
                    else if (value == ".")
                    {
                        sb.Append($"<td class='pass'>{value}</td>");
                    }
                    else if (value == "-")
                    {
                        sb.Append($"<td class='missing'>{value}</td>");
                    }
                    else
                    {
                        sb.Append($"<td>{value}</td>");
                    }
                }
            }

            sb.AppendLine("</tr>");
        }

        sb.AppendLine("</table>");
        sb.AppendLine("</body>");
        sb.AppendLine("</html>");

        File.WriteAllText(filename, sb.ToString());
    }

    private static void LogTCSection(string message, IEnumerable<string> collection)
    {
        Console.WriteLine(
            $"##teamcity[blockOpened name='{message}']{Environment.NewLine}" +
            string.Join(string.Empty, collection.Select(t => $"{t}{Environment.NewLine}")) +
            $"##teamcity[blockClosed name='{message}']");
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
