using System;
using System.Globalization;
using System.IO;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using CsvHelper;
using CsvHelper.Configuration;
using TeamCityMonitoring.MonitoringMetrics;
using TeamCityMonitoring.TeamCityService;
using File = System.IO.File;
// ReSharper disable MethodSupportsCancellation

namespace TeamCityMonitoring.Monitoring
{
    public class Monitor
    {
        private readonly string _url;
        private readonly string _token;
        private readonly string _csv;

        public Monitor(string url, string token, string csv)
        {
            _url = url;
            _token = token;
            _csv = csv;
        }

        public async Task RunAsync(int period, CancellationToken cancellationToken)
        {
            var httpClient = new HttpClient();
            httpClient.DefaultRequestHeaders.Authorization =
                new AuthenticationHeaderValue("Bearer", _token);
            httpClient.BaseAddress = new Uri(_url);
            var client = new Client(httpClient) { BaseUrl = _url };

            await LoopAsync(client, period, _csv, cancellationToken);
        }

        private static async Task LoopAsync(Client client, int period, string csvPath, CancellationToken cancellationToken)
        {
            var configuration = new CsvConfiguration(CultureInfo.InvariantCulture)
            {
                Delimiter = ";",
                ShouldQuote = (s, context) => true,
                IgnoreBlankLines = true,
                NewLine = NewLine.CRLF
            };

            try
            {
                var delay = TimeSpan.FromMinutes(period);
                using (var stream = File.OpenWrite(csvPath))
                using (var writer = new StreamWriter(stream, Encoding.UTF8))
                using (var csvWriter = new CsvWriter(writer, configuration))
                {
                    csvWriter.WriteHeader<QueuedBuildStatus>();
                    while (true)
                    {
                        var now = DateTime.UtcNow;
                        var result = await client.GetBuildsAsync(null, null, cancellationToken);

                        await Task.WhenAll(WriteRecordsAsync(result, csvWriter, writer, now),
                            Task.Delay(delay, cancellationToken));
                    }
                }
            }
            catch (OperationCanceledException)
            {
                // Ignore
            }
        }

        private static async Task WriteRecordsAsync(Builds result, IWriter csvWriter, TextWriter writer, DateTime now)
        {
            await Task.Yield();
            _ = Task.Run(() => Console.WriteLine($"Number of builds {result.Count} at {now}"));
            foreach (var build in result.Build)
            {
                csvWriter.NextRecord();
                csvWriter.WriteRecord(new QueuedBuildStatus { NumberOfBuilds = result.Count ?? 0, Timestamp = now, Id = build.Id?.ToString(), Branch = build.BranchName, Type = build.BuildTypeId });
            }
            await csvWriter.FlushAsync();
            await writer.FlushAsync();
        }
    }
}
