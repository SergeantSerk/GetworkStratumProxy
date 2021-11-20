using CommandLine;
using System;
using System.Net;

namespace GetworkStratumProxy.ConsoleApp
{
    internal class CommandLineOptions
    {
        [Option('r', "rpc", Required = true, HelpText = "RPC endpoint URI for the node to getWork from, such as http://127.0.0.1:8545/")]
        public Uri RpcUri { get; set; }

        [Option("poll-interval", Required = false, Default = 500, HelpText = "Interval to poll the getWork jobs, in milliseconds")]
        public int PollInterval { get; set; }

        [Option('a', "address", Required = false, Default = "0.0.0.0", HelpText = "IP address to listen stratum requests from e.g. 0.0.0.0")]
        public string StratumIPAddressString { get; set; }
        public IPAddress StratumIPAddress => IPAddress.TryParse(StratumIPAddressString, out IPAddress address) ? address : IPAddress.Any;

        [Option('p', "port", Required = false, Default = 3131, HelpText = "Port number to listen stratum requests from e.g. 3131")]
        public int StratumPort { get; set; }

        [Option('v', "verbose", Required = false, HelpText = "Show more detailed and verbose output")]
        public bool Verbose { get; set; }
    }
}
