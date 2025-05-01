using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

class AgentTestMatrix
{
    class Test
    {
        public string Agentname { get; set; } = string.Empty;
        public string Buildid { get; set; } = string.Empty;
        public string Testname { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public bool? Muted { get; set; }
        public string Buildstart { get; set; } = string.Empty;
    }

    static async Task<int> Main(string[] args)
    {
        var result = 0;
        if (args.Length != 1)
        {
            Console.WriteLine(
@"AgentTestMatrix 1.1 - For each build agent, retrieves the latest test result, shows all failing tests in matrix.

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
                await WriteTests(args[0]);
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
            _ = Console.ReadKey();
        }

        return result;
    }

    static async Task WriteTests(string outfile)
    {
        var server = GetServer();
        var buildconfig = GetBuildconfig();
        var excludeMuted = GetExcludeMuted();
        var excludeAgents = GetExcludeAgents();

        GetCredentials(out string? username, out string? password);

        GetAgentPrefixes(out string[] prefixFrom, out string[] prefixTo);

        List<Test> tests = await GetTests(server, username, password, buildconfig);

        if (excludeAgents != null)
        {
            tests = [.. tests.Where(t => !excludeAgents.Contains(t.Agentname))];
        }

        if (!string.IsNullOrEmpty(Environment.GetEnvironmentVariable("TestDebug")))
        {
            string[] header = ["buildid\tagentname\ttestname\tstatus\tmuted\tbuildstart"];

            File.WriteAllLines("TestDebug4.txt", Enumerable.Concat(header, tests.Select(t => $"{t.Buildid}\t{t.Agentname}\t{t.Testname}\t{t.Status}\t{t.Muted}\t{t.Buildstart}")));
        }

        string[] builds = [.. tests.GroupBy(t => t.Buildid).Select(t => t.Key)];
        string[] agents = [.. tests.GroupBy(t => t.Agentname).Select(a => a.Key)];

        tests = [.. tests.Where(t => !excludeMuted || !t.Muted.HasValue || !t.Muted.Value)];

        Log($"Found {tests.Count} tests, of which {tests.Count(t => t.Status == "FAILURE")} failed, in {builds.Length} builds containing tests, on {agents.Length} build agents.");

        Dictionary<string, List<Test>> agentTests = [];

        foreach (var build in builds)
        {
            var agent = tests.First(t => t.Buildid == build).Agentname;

            if (!agentTests.ContainsKey(agent))
            {
                agentTests[agent] = [.. tests.Where(t => t.Buildid == build)];
            }
        }

        var testmatrix = PrintFailMatrix(agents, agentTests, prefixFrom, prefixTo);

        WriteHtml(testmatrix, outfile);
    }

    static string GetServer()
    {
        var server = Environment.GetEnvironmentVariable("TestServer");

        if (server != null)
        {
            Log($"Got server from environment variable: '{server}'");
        }

        if (server == null)
        {
            Dictionary<string, string> tcvariables = GetTeamcityConfigVariables();

            if (tcvariables.TryGetValue("teamcity.serverUrl", out string? value))
            {
                server = value;
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
        var buildconfig = Environment.GetEnvironmentVariable("TestBuildconfig");

        if (buildconfig != null)
        {
            Log($"Got buildconfig from environment variable: '{buildconfig}'");
        }

        if (buildconfig == null)
        {
            Dictionary<string, string> tcvariables = GetTeamcityBuildVariables();

            if (tcvariables.TryGetValue("teamcity.buildType.id", out string? value))
            {
                buildconfig = value;
                Log($"Got buildconfig from Teamcity: '{buildconfig}'");
            }
        }

        return buildconfig ?? throw new ApplicationException("No buildconfig specified.");
    }

    static bool GetExcludeMuted()
    {
        var excludeMuted = Environment.GetEnvironmentVariable("TestExcludeMuted");

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

    static string[]? GetExcludeAgents()
    {
        var excludeAgents = Environment.GetEnvironmentVariable("TestExcludeAgents");

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

    static void GetCredentials(out string? username, out string? password)
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

            if (username == null && tcvariables.TryGetValue("teamcity.auth.userId", out string? valueUserId))
            {
                username = valueUserId;
                Log("Got username from Teamcity.");
            }
            if (password == null && tcvariables.TryGetValue("teamcity.auth.password", out string? valuePassword))
            {
                password = valuePassword;
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
        var testAgentPrefixFrom = Environment.GetEnvironmentVariable("TestAgentPrefixFrom");
        var testAgentPrefixTo = Environment.GetEnvironmentVariable("TestAgentPrefixTo");

        if (testAgentPrefixFrom == null || testAgentPrefixTo == null)
        {
            Log("No valid agent prefixes specified.");
            prefixesFrom = [];
            prefixesTo = [];
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
        var buildpropfile = Environment.GetEnvironmentVariable("TEAMCITY_BUILD_PROPERTIES_FILE");
        if (string.IsNullOrEmpty(buildpropfile))
        {
            Log("Couldn't find Teamcity build properties file.");
            return [];
        }
        if (!File.Exists(buildpropfile))
        {
            Log($"Couldn't find Teamcity build properties file: '{buildpropfile}'");
            return [];
        }

        Log($"Reading Teamcity build properties file: '{buildpropfile}'");
        var rows = File.ReadAllLines(buildpropfile);

        var valuesBuild = GetPropValues(rows);

        if (!string.IsNullOrEmpty(Environment.GetEnvironmentVariable("TestDebug")))
        {
            LogTCSection("Teamcity Properties", valuesBuild.Select(p => $"Build: {p.Key}={p.Value}"));
        }

        return valuesBuild;
    }

    static Dictionary<string, string> GetTeamcityConfigVariables()
    {
        var buildpropfile = Environment.GetEnvironmentVariable("TEAMCITY_BUILD_PROPERTIES_FILE");
        if (string.IsNullOrEmpty(buildpropfile))
        {
            Log("Couldn't find Teamcity build properties file.");
            return [];
        }
        if (!File.Exists(buildpropfile))
        {
            Log($"Couldn't find Teamcity build properties file: '{buildpropfile}'");
            return [];
        }

        Log($"Reading Teamcity build properties file: '{buildpropfile}'");
        var rows = File.ReadAllLines(buildpropfile);

        var valuesBuild = GetPropValues(rows);

        if (!string.IsNullOrEmpty(Environment.GetEnvironmentVariable("TestDebug")))
        {
            LogTCSection("Teamcity Properties", valuesBuild.Select(p => $"Build: {p.Key}={p.Value}"));
        }

        var configpropfile = valuesBuild["teamcity.configuration.properties.file"];
        if (string.IsNullOrEmpty(configpropfile))
        {
            Log("Couldn't find Teamcity config properties file.");
            return [];
        }
        if (!File.Exists(configpropfile))
        {
            Log($"Couldn't find Teamcity config properties file: '{configpropfile}'");
            return [];
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
        Dictionary<string, string> dic = [];

        foreach (var row in rows)
        {
            var index = row.IndexOf('=');
            if (index != -1)
            {
                var key = row[..index];
                var value = Regex.Unescape(row[(index + 1)..]);
                dic[key] = value;
            }
        }

        return dic;
    }

    static async Task<List<Test>> GetTests(string server, string? username, string? password, string buildconfig)
    {
        List<Test> tests = [];

        using HttpClient client = new();

        if (username != null && password != null)
        {
            string credentials = Convert.ToBase64String(Encoding.ASCII.GetBytes($"{username}:{password}"));
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", credentials);
        }

        var address = $"{server}/app/rest/builds?locator=buildType:{buildconfig}";
        client.DefaultRequestHeaders.Add("Accept", "application/json");

        dynamic builds = DownloadJsonContent(client, address, "TestDebug1.txt");

        foreach (JProperty property in builds)
        {
            if (property?.First?.Type == JTokenType.Array)
            {
                foreach (dynamic build in property.First)
                {
                    string buildhref = build.href;

                    address = $"{server}{buildhref}";
                    client.DefaultRequestHeaders.Add("Accept", "application/json");

                    var buildresult = await DownloadJsonContent(client, address, "TestDebug2.txt");

                    var agentname = buildresult["agent"]?["name"]?.Value<string>();

                    if (agentname != null && buildresult != null && buildresult["testOccurrences"] != null && buildresult["testOccurrences"]?["href"] != null)
                    {
                        var testhref = buildresult["testOccurrences"]?["href"];
                        if (testhref == null)
                        {
                            Log("Invalid href.");
                            continue;
                        }

                        address = $"{server}{testhref},count:10000";
                        client.DefaultRequestHeaders.Add("Accept", "application/json");

                        var testresults = await DownloadJsonContent(client, address, "TestDebug3.txt");

                        var testOccurrences = testresults["testOccurrence"];
                        if (testOccurrences != null)
                        {
                            foreach (var testOccurrence in testOccurrences)
                            {
                                if (testOccurrence == null || testOccurrence.Type != JTokenType.Object)
                                {
                                    Log("Invalid test occurrence.");
                                    continue;
                                }

                                tests.Add(new Test
                                {
                                    Agentname = agentname,
                                    Buildid = buildresult["id"]?.Value<string>() ?? string.Empty,
                                    Testname = testOccurrence["name"]?.Value<string>() ?? string.Empty,
                                    Status = testOccurrence["status"]?.Value<string>() ?? string.Empty,
                                    Muted = testOccurrence["muted"]?.Value<bool>(),
                                    Buildstart = buildresult["startDate"]?.Value<string>() ?? string.Empty
                                });
                            }
                        }
                    }
                }
            }
        }

        return tests;
    }

    static Dictionary<string, bool> writtenLogs = [];

    static async Task<JObject> DownloadJsonContent(HttpClient client, string address, string debugFilename)
    {
        Log($"Address: '{address}'");
        try
        {
            var content = await client.GetStringAsync(address);
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
            throw new ApplicationException(ex.Message, ex);
        }
    }

    static string PrintFailMatrix(string[] agents, Dictionary<string, List<Test>> agentTests, string[] prefixFrom, string[] prefixTo)
    {
        string[] testnames = [.. agentTests
            .SelectMany(a => a.Value)
            .Select(t => t.Testname)
            .Distinct()
            .OrderBy(t => t)];

        StringBuilder sb = new();

        var now = DateTime.Now;

        _ = sb.Append("Test/Agent");
        foreach (var agentname in agents.OrderBy(a => a))
        {
            var agentname2 = agentname;

            for (var i = 0; i < prefixFrom.Length && i < prefixTo.Length; i++)
            {
                if (agentname2.StartsWith(prefixFrom[i]))
                {
                    var datestring = agentTests.Where(t => t.Value.First().Agentname == agentname).Select(t => t.Value.First().Buildstart).First();
                    var datetime = DateTime.ParseExact(datestring, "yyyyMMddTHHmmss+ffff", CultureInfo.InvariantCulture, DateTimeStyles.None);

                    agentname2 = $"{prefixTo[i]}{agentname2[prefixFrom[i].Length..]}|{agentname2}|{datetime}|";
                    break;
                }
            }

            _ = sb.Append($"\t{agentname2}");
        }

        _ = sb.AppendLine();

        _ = sb.Append("Hours since last run");
        foreach (var agentname in agents.OrderBy(a => a))
        {
            var datestring = agentTests.Where(t => t.Value.First().Agentname == agentname).Select(t => t.Value.First().Buildstart).First();
            var datetime = DateTime.ParseExact(datestring, "yyyyMMddTHHmmss+ffff", CultureInfo.InvariantCulture, DateTimeStyles.None);
            var hourssince = (now - datetime).TotalHours.ToString("0", CultureInfo.InvariantCulture);

            _ = sb.Append($"\t{hourssince}");
        }

        _ = sb.AppendLine();

        var failcount = 0;

        var totalfailcount = 0;
        var totalsuccesscount = 0;
        var totalmissingcount = 0;

        foreach (var testname in testnames.OrderBy(t => t))
        {
            _ = sb.Append(testname);

            var failed = false;

            foreach (var agentname in agents.OrderBy(a => a))
            {
                if (agentTests[agentname].Any(t => t.Testname == testname))
                {
                    if (agentTests[agentname].Any(t => t.Testname == testname && t.Status == "FAILURE"))
                    {
                        _ = sb.Append("\tx");
                        totalfailcount++;
                        failed = true;
                    }
                    else
                    {
                        _ = sb.Append("\t.");
                        totalsuccesscount++;
                    }
                }
                else
                {
                    _ = sb.Append("\t-");
                    totalmissingcount++;
                }
            }

            if (failed)
            {
                failcount++;
            }

            _ = sb.AppendLine();
        }

        var agentSums = string.Join("\t", agentTests.OrderBy(t => t.Value.First().Agentname).Select(t => t.Value.Count(tt => tt.Status == "FAILURE")));

        _ = sb.AppendLine($"{testnames.Length} tests, {failcount} failed, {testnames.Length - failcount} succeded (Total: {totalfailcount} failed, {totalsuccesscount} succeded, {totalmissingcount} missing)\t{agentSums}");

        return sb.ToString();
    }

    static void WriteHtml(string tablecontent, string filename)
    {
        StringBuilder sb = new();

        tablecontent = tablecontent.Replace("\r", "");

        var rows = tablecontent.Replace("\r", "").Split(['\n'], StringSplitOptions.RemoveEmptyEntries);

        _ = sb.AppendLine("<html>");
        _ = sb.AppendLine("<body>");
        _ = sb.AppendLine("<style>");
        _ = sb.AppendLine(
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
        _ = sb.AppendLine("</style>");
        _ = sb.AppendLine("<script src='http://code.jquery.com/jquery-latest.min.js'></script>");
        _ = sb.AppendLine("<script>");
        _ = sb.AppendLine(
@"$(document).ready(function(){$('#checkboxID').change(function(){
    var self = this;
    $('tr.successes').toggle(self.checked);
}).change();});");
        _ = sb.AppendLine("</script>");
        _ = sb.AppendLine("</head>");
        _ = sb.AppendLine("<body>");

        _ = sb.AppendLine(@"<p><label for='checkboxID'>Show all tests</label><input type='checkbox' id='checkboxID' /></p>");
        _ = sb.AppendLine("<p>x Test failed.<br/>");
        _ = sb.AppendLine(". Test passed.<br/>");
        _ = sb.AppendLine("- Test missing.</p>");
        _ = sb.AppendLine("<p>Obvious thing you should know: There's a tooltip on column headers!</p>");

        _ = sb.AppendLine("<table border=\"1\">");

        foreach (var row in rows)
        {
            var values = row.Split('\t');
            var allsuccess = values.Skip(1).All(v => v == ".");
            var anyfails = values.Skip(1).Any(v => v == "x");

            if (allsuccess)
            {
                _ = sb.Append("<tr class='successes' style='display: none;'>");
            }
            if (anyfails)
            {
                _ = sb.Append("<tr class='fails'>");
            }

            foreach (var value in values)
            {
                var start = value.IndexOf('|');
                var end = value.LastIndexOf('|');
                if (start != -1 && end != -1 && start < end)
                {
                    var title = value.Substring(start + 1, end - start - 1).Replace("|", "\n");
                    var cleanvalue = value[..start];
                    _ = sb.Append($"<td title='{title}'>{cleanvalue}</td>");
                }
                else
                {
                    var classAttribute = value switch
                    {
                        "x" => " class='fail'",
                        "." => " class='pass'",
                        "-" => " class='missing'",
                        _ => string.Empty
                    };
                    _ = sb.Append($"<td{classAttribute}>{value}</td>");
                }
            }

            _ = sb.AppendLine("</tr>");
        }

        _ = sb.AppendLine("</table>");
        _ = sb.AppendLine("</body>");
        _ = sb.AppendLine("</html>");

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
        var oldColor = Console.ForegroundColor;
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
        var hostname = Dns.GetHostName();
        Console.WriteLine($"{hostname}: {message}");
    }
}
