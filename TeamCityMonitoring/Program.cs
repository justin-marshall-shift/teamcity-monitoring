using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using CommandLine;
using TeamCityMonitoring.Exporting;
using TeamCityMonitoring.Monitoring;
using TeamCityMonitoring.Options;
using Monitor = TeamCityMonitoring.Monitoring.Monitor;

namespace TeamCityMonitoring
{
    internal class Program
    {
        private static void Main(string[] args)
        {
            Parser.Default.ParseArguments<MonitorOptions, GraphOptions, DeepMonitorOptions>(args)
                .MapResult(
                    (MonitorOptions opts) => RunOptions(opts),
                    (GraphOptions opts) => RunOptions(opts),
                    (DeepMonitorOptions opts) => RunOptions(opts),
                    HandleParseError);
        }

        private static int RunOptions(DeepMonitorOptions opts)
        {
            var monitor = new DeepMonitor(opts.Url, opts.Token, opts.Folder, opts.GitHubToken);

            var cancellationTokenSource = new CancellationTokenSource();

            if (opts.Duration > 0)
                cancellationTokenSource = new CancellationTokenSource(TimeSpan.FromHours(opts.Duration));

            try
            {
                Console.WriteLine("Beginning of deep monitoring");
                monitor.RunAsync(opts.Period, cancellationTokenSource.Token).Wait(cancellationTokenSource.Token);
            }
            catch (OperationCanceledException)
            {
                // Ignore
            }
            finally
            {
                Console.WriteLine("End of deep monitoring");
            }
            return 0;
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
            try
            {
                Console.WriteLine("Beginning of monitoring");
                monitor.RunAsync(opts.Period, cancellationTokenSource.Token).Wait(cancellationTokenSource.Token);
            }
            catch (OperationCanceledException)
            {
                // Ignore
            }
            finally
            {
                Console.WriteLine("End of monitoring");
            }
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
