using CommandLine;
using GetworkStratumProxy.Extension;
using GetworkStratumProxy.Node;
using GetworkStratumProxy.Proxy;
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

            using (BaseNode pollingNode = new PollingNode(options.RpcUri, options.PollInterval))
            using (IProxy proxy = new EthProxy(pollingNode, options.StratumIPAddress, options.StratumPort))
            {
                pollingNode.Start();
                proxy.Start();

                Console.CancelKeyPress += (o, e) =>
                {
                    e.Cancel = true;
                    ConsoleHelper.Log("Program", $"Caught {e.SpecialKey}, stopping", LogLevel.Information);
                    IsRunning = false;
                };

                while (IsRunning)
                {
                    await Task.Delay(1000);
                }
            }

            ConsoleHelper.Log("Program", "Exited gracefully", LogLevel.Information);
        }
    }
}
