using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using CommandLine;
using TeamCityMonitoring.Options;
using Monitor = TeamCityMonitoring.Monitoring.Monitor;

namespace TeamCityMonitoring
{
    internal class Program
    {
        private static void Main(string[] args)
        {
            Parser.Default.ParseArguments<DefaultOptions>(args)
                .WithParsed(RunOptions)
                .WithNotParsed(HandleParseError);
        }

        private static void RunOptions(DefaultOptions opts)
        {
            var monitor = new Monitor();

            var delay = TimeSpan.FromHours(opts.Duration);
            var cancellationTokenSource = new CancellationTokenSource(delay);
            
            monitor.RunAsync(opts.Url, opts.Token, opts.Csv, opts.Period, cancellationTokenSource.Token).Wait(cancellationTokenSource.Token);
        }

        private static void HandleParseError(IEnumerable<Error> errs)
        {
            foreach (var error in errs.Select(err => err.Tag).Distinct())
            {
                Console.WriteLine($"{error}");
            }
        }
    }
}
