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

namespace TeamCityMonitoring.Monitoring
{
    public class Monitor
    {
        public async Task RunAsync(string url, string token, string path, int period, CancellationToken cancellationToken)
        {
            var httpClient = new HttpClient();
            httpClient.DefaultRequestHeaders.Authorization =
                new AuthenticationHeaderValue("Bearer", token);
            httpClient.BaseAddress = new Uri(url);
            var client = new Client(httpClient) { BaseUrl = url };

            await LoopAsync(client, period, path, cancellationToken);
        }

        private async Task LoopAsync(Client client, int period, string csvPath, CancellationToken cancellationToken)
        {
            Console.WriteLine("Beginning of monitoring");
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

                        await Task.WhenAll(WriteRecordsAsync(result, csvWriter, writer, now), Task.Delay(delay, cancellationToken));
                    }
                }
            }
            catch (OperationCanceledException)
            {
                Console.WriteLine("End of monitoring");
            }
        }

        private async Task WriteRecordsAsync(Builds result, IWriter csvWriter, TextWriter writer, DateTime now)
        {
            Console.WriteLine($"Number of builds {result.Count} at {now}");
            foreach (var build in result.Build)
            {
                csvWriter.WriteRecord(new QueuedBuildStatus { NumberOfBuilds = result.Count ?? 0, Timestamp = now, Id = build.Id?.ToString(), Branch = build.BranchName, Type = build.BuildTypeId });
                csvWriter.NextRecord();
            }
            await csvWriter.FlushAsync();
            await writer.FlushAsync();
        }
    }
}
