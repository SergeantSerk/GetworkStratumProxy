using CommandLine;
using GetworkStratumProxy.Extension;
using GetworkStratumProxy.Node;
using GetworkStratumProxy.Node.Eth;
using GetworkStratumProxy.Proxy.Server;
using GetworkStratumProxy.Proxy.Server.Eth;
using System;
using System.Threading.Tasks;

namespace GetworkStratumProxy.ConsoleApp
{
    public class Program
    {
        public static bool IsRunning { get; set; } = true;

        public static async Task Main(string[] args)
        {
            await Parser.Default.ParseArguments<CommandLineOptions>(args)
                .WithParsedAsync(OptionParseOkAsync);
        }

        private static async Task OptionParseOkAsync(CommandLineOptions options)
        {
            if (options.PollInterval <= 0)
            {
                options.PollInterval = 500;
            }

            ConsoleHelper.IsVerbose = options.Verbose;
            Console.CancelKeyPress += (o, e) =>
            {
                e.Cancel = true;
                ConsoleHelper.Log(typeof(Program), $"Caught {e.SpecialKey}, stopping", LogLevel.Information);
                IsRunning = false;
            };

            using (BaseEthNode pollingNode = new PollingEthNode(options.RpcUri, options.PollInterval))
            using (IProxy proxy = new GetworkEthProxy(pollingNode, options.StratumIPAddress, options.StratumPort))
            {
                pollingNode.Start();
                proxy.Start();

                while (IsRunning)
                {
                    await Task.Delay(1000);
                }
            }

            ConsoleHelper.Log(typeof(Program), "Exited gracefully", LogLevel.Information);
        }
    }
}
