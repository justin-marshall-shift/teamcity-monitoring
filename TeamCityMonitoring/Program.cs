using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using CommandLine;
using TeamCityMonitoring.Exporting;
using TeamCityMonitoring.Options;
using Monitor = TeamCityMonitoring.Monitoring.Monitor;

namespace TeamCityMonitoring
{
    internal class Program
    {
        private static void Main(string[] args)
        {
            Parser.Default.ParseArguments<MonitorOptions, GraphOptions>(args)
                .MapResult(
                    (MonitorOptions opts) => RunOptions(opts),
                    (GraphOptions opts) => RunOptions(opts),
                    HandleParseError);
        }

        private static int RunOptions(GraphOptions opts)
        {
            var exporter = new Exporter(opts.Csv, opts.Excel);
            exporter.ExportAsync().Wait();
            return 0;
        }

        private static int RunOptions(MonitorOptions opts)
        {
            var monitor = new Monitor(opts.Url, opts.Token, opts.Csv);

            var delay = TimeSpan.FromHours(opts.Duration);
            var cancellationTokenSource = new CancellationTokenSource(delay);
            
            monitor.RunAsync(opts.Period, cancellationTokenSource.Token).Wait(cancellationTokenSource.Token);
            return 0;
        }

        private static int HandleParseError(IEnumerable<Error> errs)
        {
            foreach (var error in errs.Select(err => err.Tag).Distinct())
            {
                Console.WriteLine($"{error}");
            }

            return 1;
        }
    }
}
