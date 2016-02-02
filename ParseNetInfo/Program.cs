using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ParseNetInfo
{
    class Program
    {
        static int Main(string[] args)
        {
            if (args.Length != 6)
            {
                Console.WriteLine(
@"Usage: ParseNetInfo <states.txt> <states2.txt> <rules.txt> <rules2.txt> <nics.txt> <nics2.txt>

states:  Output from netsh advfirewall show allprofiles state
rutes:   Output from netsh advfirewall firewall show rule name=all
nics:    Output from netsh interface ipv4 show addresses");
                return 2;
            }

            ParseStates(args[0]);
            ParseRules(args[1]);
            ParseNics(args[2]);

            return 0;
        }

        public class state
        {
            public string _server;
            public string _domain;
            public string _private;
            public string _public;
        }

        static void ParseStates(string filename)
        {
            Console.WriteLine("-=-=- Parsing states: " + filename + " -=-=-");
            string[] rows = File.ReadAllLines(filename);

            List<state> states = new List<state>();


            state currentstate = null;
            string currentprofile = null;

            foreach (string row in rows)
            {
                if (row.StartsWith("Server: "))
                {
                    currentstate = new state { _server = row.Substring(8) };
                    continue;
                }
                if (currentstate == null || row == string.Empty || row.StartsWith("----------"))
                {
                    continue;
                }
                if (row == "Ok.")
                {
                    states.Add(currentstate);
                    currentstate = null;
                    continue;
                }
                if (!row.Contains(' '))
                {
                    continue;
                }

                string name = row.Split(' ').First();
                string value = row.Split(new char[] { ' ' }, 2).Last().Trim();

                //Console.WriteLine("name: '" + name + "'");
                //Console.WriteLine("value: '" + value + "'");

                if (value == "Profile Settings:")
                {
                    currentprofile = name;
                }
                else
                {
                    if (name == "State")
                    {
                        if (currentprofile == "Domain")
                        {
                            currentstate._domain = value;
                        }
                        else if (currentprofile == "Private")
                        {
                            currentstate._private = value;
                        }
                        else if (currentprofile == "Public")
                        {
                            currentstate._public = value;
                        }
                        else
                        {
                            Console.WriteLine("Unknown profile: Server: " + currentstate._server + ", profile: '" + currentprofile + "'");
                        }
                    }
                    else
                    {
                        Console.WriteLine("Unknown row: Server: " + currentstate._server + ", name: '" + name + "'");
                    }
                }
            }

            List<string> output = new List<string>();
            output.Add("Server\tDomain\tPrivate\tPubic");
            output.AddRange(states
                .Select(s => s._server + '\t' + s._domain + '\t' + s._private + '\t' + s._public)
                .OrderBy(s => s));

            File.WriteAllLines("states.txt", output);
        }

        public class rule
        {
            public string _server;
            public string _rulename;
            public string _enabled;
            public string _direction;
            public string _profiles;
            public string _grouping;
            public string _localip;
            public string _remoteip;
            public string _protocol;
            public string _localport;
            public string _remoteport;
            public string _edgetraversal;
            public string _action;
        }

        static void ParseRules(string filename)
        {
            Console.WriteLine("-=-=- Parsing rules: " + filename + " -=-=-");
            string[] rows = File.ReadAllLines(filename);

            List<rule> rules = new List<rule>();


            string currentserver = null;
            rule currentrule = null;
            string currentname = null;

            foreach (string row in rows)
            {
                if (row.StartsWith("Server: "))
                {
                    currentserver = row.Substring(8);
                    continue;
                }
                if (currentserver == null || row == string.Empty || row.StartsWith("----------"))
                {
                    continue;
                }
                if (!row.Contains(' '))
                {
                    continue;
                }

                string name = row.Contains(':') ? row.Split(':').First() : string.Empty;
                string value = row.Contains(':') ? row.Split(new char[] { ':' }, 2).Last().Trim() : row.Trim();

                //Console.WriteLine("name: '" + name + "'");
                //Console.WriteLine("value: '" + value + "'");

                if (name == "Rule Name")
                {
                    currentrule = new rule { _server = currentserver };
                    currentrule._rulename = value;
                }
                else if (name == "Enabled")
                {
                    currentrule._enabled = value;
                }
                else if (name == "Direction")
                {
                    currentrule._direction = value;
                }
                else if (name == "Profiles")
                {
                    currentrule._profiles = value;
                }
                else if (name == "Grouping")
                {
                    currentrule._grouping = value;
                }
                else if (name == "LocalIP")
                {
                    currentrule._localip = value;
                }
                else if (name == "RemoteIP")
                {
                    currentrule._remoteip = value;
                }
                else if (name == "Protocol")
                {
                    currentrule._protocol = value;
                }
                else if (name == string.Empty && currentname == "Protocol")
                {
                    if (value.Split(' ').First() == "Type" && value.Split(new char[] { ' ' }, 2).Last().Trim() == "Code")
                    {
                    }
                    else if (value.Contains(' '))
                    {
                        currentrule._protocol += ": Type: " + value.Split(' ').First() + ", Code: " + value.Split(new char[] { ' ' }, 2).Last().Trim();
                    }
                    else
                    {
                        Console.WriteLine("Unknown protocol row: Server: " + currentrule._server + ", value: '" + value + "'");
                    }
                }
                else if (name == "LocalPort")
                {
                    currentrule._localport = value;
                }
                else if (name == "RemotePort")
                {
                    currentrule._remoteport = value;
                }
                else if (name == "Edge traversal")
                {
                    currentrule._edgetraversal = value;
                }
                else if (name == "Action")
                {
                    currentrule._action = value;
                    rules.Add(currentrule);
                    currentrule = null;
                }
                else
                {
                    Console.WriteLine("Unknown row: Server: " + currentrule._server + ", name: '" + name + "'");
                }

                if (name != string.Empty)
                {
                    currentname = name;
                }
            }

            List<string> output = new List<string>();
            output.Add(
                "Server\tRuleName\tEnabled\tDirection\t" +
                "Profiles\tGrouping\tLocalIP\tRemoteIP\t" +
                "Protocol\tLocalPort\tRemotePort\tEdgeTraversal\t" +
                "Action");
            output.AddRange(rules
                .Select(s =>
                s._server + '\t' + s._rulename + '\t' + s._enabled + '\t' + s._direction + '\t' +
                s._profiles + '\t' + s._grouping + '\t' + s._localip + '\t' + s._remoteip + '\t' +
                s._protocol + '\t' + s._localport + '\t' + s._remoteport + '\t' + s._edgetraversal + '\t' +
                s._action)
                .OrderBy(s => s));

            File.WriteAllLines("rules.txt", output);

            return;
        }

        public class nic
        {
            public string _server;
            public string _name;
            public string _dhcpenabled;
            public string _ipaddresses;
            public string _subnetprefixes;
            public string _defaultgateway;
            public string _gatewaymetric;
            public string _interfacemetric;
        }

        static void ParseNics(string filename)
        {
            Console.WriteLine("-=-=- Parsing nics: " + filename + " -=-=-");
            string[] rows = File.ReadAllLines(filename);

            List<nic> nics = new List<nic>();


            string currentserver = null;
            nic currentnic = null;
            string currentname = null;

            foreach (string row in rows)
            {
                if (row.StartsWith("Server: "))
                {
                    currentserver = row.Substring(8);
                    continue;
                }
                if (currentserver == null || row == string.Empty || row.StartsWith("----------"))
                {
                    continue;
                }
                if (!row.Contains(' '))
                {
                    continue;
                }

                string name = row.Contains(':') ? row.Split(':').First().TrimStart() : row;
                string value = row.Contains(':') ? row.Split(new char[] { ':' }, 2).Last().Trim() : string.Empty;

                //Console.WriteLine("name: '" + name + "'");
                //Console.WriteLine("value: '" + value + "'");

                if (name.StartsWith("Configuration for interface "))
                {
                    currentnic = new nic { _server = currentserver };
                    currentnic._name = name.Substring(28).Trim('"');
                }
                else if (name == "DHCP enabled")
                {
                    currentnic._dhcpenabled = value;
                }
                else if (name == "IP Address")
                {
                    if (currentnic._ipaddresses == null)
                    {
                        currentnic._ipaddresses = value;
                    }
                    else
                    {
                        currentnic._ipaddresses += ", " + value;
                    }
                }
                else if (name == "Subnet Prefix")
                {
                    if (currentnic._subnetprefixes == null)
                    {
                        currentnic._subnetprefixes = value;
                    }
                    else
                    {
                        currentnic._subnetprefixes += ", " + value;
                    }
                }
                else if (name == "Default Gateway")
                {
                    currentnic._defaultgateway = value;
                }
                else if (name == "Gateway Metric")
                {
                    currentnic._gatewaymetric = value;
                }
                else if (name == "InterfaceMetric")
                {
                    currentnic._interfacemetric = value;
                    nics.Add(currentnic);
                    currentnic = null;
                }
                else
                {
                    Console.WriteLine("Unknown row: Server: " + currentnic._server + ", name: '" + name + "'");
                }

                if (name != string.Empty)
                {
                    currentname = name;
                }
            }

            List<string> output = new List<string>();
            output.Add(
                "Server\tName\t_DhcpEnabled\tIpAddresses\t" +
                "SubnetPrefixes\tDefaultGateway\tGatewayMetric\tInterfaceMetric");
            output.AddRange(nics
                .Select(s =>
                s._server + '\t' + s._name + '\t' + s._dhcpenabled + '\t' + s._ipaddresses + '\t' +
                s._subnetprefixes + '\t' + s._defaultgateway + '\t' + s._gatewaymetric + '\t' + s._interfacemetric)
                .OrderBy(s => s));

            File.WriteAllLines("nics.txt", output);

            return;
        }
    }
}
