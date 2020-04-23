using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
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
    public class DeepMonitor
    {
        private readonly string _url;
        private readonly string _token;
        private readonly string _folder;

        public DeepMonitor(string url, string token, string folder)
        {
            _url = url;
            _token = token;
            _folder = folder;
        }

        public async Task RunAsync(int period, CancellationToken cancellationToken)
        {
            var httpClient = new HttpClient();
            httpClient.DefaultRequestHeaders.Authorization =
                new AuthenticationHeaderValue("Bearer", _token);
            httpClient.BaseAddress = new Uri(_url);
            var client = new Client(httpClient) { BaseUrl = _url };

            try
            {
                await LoopAsync(client, period, cancellationToken);
            }
            catch (OperationCanceledException)
            {
                // Ignore
            }
        }

        private async Task LoopAsync(Client client, int period, CancellationToken cancellationToken)
        {
            var delay = TimeSpan.FromMinutes(period);
            var now = DateTime.UtcNow;
            var currentMonitoringTime = now;

            var buildIds = new HashSet<long>();
            var buildDumpIds = new HashSet<long>();

            var (queueCsvPath, buildsCsvPath, agentsCsvPath) = GetPaths(currentMonitoringTime);
            var queueOutput = GetAndInitializeWriter<BuildInQueue>(queueCsvPath);
            var buildsOutput = GetAndInitializeWriter<BuildDetails>(buildsCsvPath);
            var agentsOutput = GetAndInitializeWriter<AllAgentsStatus>(agentsCsvPath);

            try
            {
                while (true)
                {
                    if (now.Date != currentMonitoringTime.Date)
                    {
                        await FlushAndDisposeOutput(queueOutput);
                        await FlushAndDisposeOutput(buildsOutput);
                        await FlushAndDisposeOutput(agentsOutput);
                        buildIds.Clear();
                        buildDumpIds.Clear();
                        (queueCsvPath, buildsCsvPath, agentsCsvPath) = GetPaths(now);
                        queueOutput = GetAndInitializeWriter<BuildInQueue>(queueCsvPath);
                        buildsOutput = GetAndInitializeWriter<BuildDetails>(buildsCsvPath);
                        agentsOutput = GetAndInitializeWriter<AllAgentsStatus>(agentsCsvPath);
                        currentMonitoringTime = now;
                    }

                    var queue = await client.GetBuildsAsync(null, null, cancellationToken);

                    var agents = await client.ServeAgentsAsync(null, null, null, "count,agent(id,name,enabled,authorized,build)");
                    var agentsTask = WriteAgentsAsync(agents, agentsOutput, now, buildIds, buildDumpIds);

                    var queueTask = WriteQueueAsync(queue, queueOutput, now);

                    foreach (var build in queue.Build)
                    {
                        if (build.Id.HasValue && !buildDumpIds.Contains(build.Id.Value))
                            buildIds.Add(build.Id.Value);
                    }

                    var buildsTask = WriteBuildsAsync(client, buildIds, buildDumpIds, buildsOutput, force: false, cancellationToken:cancellationToken);

                    await Task.WhenAll(queueTask, buildsTask, Task.Delay(delay, cancellationToken));

                    now = DateTime.UtcNow;
                }
            }
            finally
            {
                await WriteBuildsAsync(client, buildIds, buildDumpIds, buildsOutput, force: true, cancellationToken: cancellationToken);
                await FlushAndDisposeOutput(queueOutput);
                await FlushAndDisposeOutput(buildsOutput);
                await FlushAndDisposeOutput(agentsOutput);
            }
        }

        private static async Task WriteBuildsAsync(Client client, HashSet<long> buildIds, HashSet<long> buildDumpIds,
            (Stream stream, TextWriter writer, CsvWriter csvWriter) output, bool force, CancellationToken cancellationToken)
        {
            var builds = new List<Build>();

            foreach (var buildId in buildIds)
            {
                var build = await client.ServeBuildAsync($"id:{buildId}", null, cancellationToken);

                if((force || !string.IsNullOrEmpty(build.FinishDate)) && !buildDumpIds.Contains(buildId))
                    builds.Add(build);
            }

            // ReSharper disable once StringLiteralTypo
            const string teamCityDateFormat = "yyyyMMddTHHmmsszzz";
            var (_, writer, csvWriter) = output;
            await csvWriter.WriteRecordsAsync(builds.Select(build =>
                {
                    buildDumpIds.Add(build.Id.Value);
                    return new BuildDetails
                    {
                        Id = build.Id?.ToString(),
                        Type = build.BuildTypeId,
                        Agent = build.Agent?.Name,
                        Branch = build.BranchName,
                        FinishedDate = !string.IsNullOrEmpty(build.FinishDate)
                            ? DateTime.ParseExact(build.FinishDate, teamCityDateFormat, CultureInfo.InvariantCulture,
                                DateTimeStyles.None).ToUniversalTime().ToString("o")
                            : DateTime.MinValue.ToString("o"),
                        QueueDate = !string.IsNullOrEmpty(build.QueuedDate)
                            ? DateTime.ParseExact(build.QueuedDate, teamCityDateFormat, CultureInfo.InvariantCulture,
                                DateTimeStyles.None).ToUniversalTime().ToString("o")
                            : DateTime.MinValue.ToString("o"),
                        StartDate = !string.IsNullOrEmpty(build.StartDate)
                            ? DateTime.ParseExact(build.StartDate, teamCityDateFormat, CultureInfo.InvariantCulture,
                                DateTimeStyles.None).ToUniversalTime().ToString("o")
                            : DateTime.MinValue.ToString("o"),
                        State = build.State,
                        Status = build.Status,
                        Trigger = build.Triggered?.User?.Username,
                        TriggerTime = !string.IsNullOrEmpty(build.Triggered?.Date)
                            ? DateTime.ParseExact(build.Triggered.Date, teamCityDateFormat,
                                CultureInfo.InvariantCulture, DateTimeStyles.None).ToUniversalTime().ToString("o")
                            : DateTime.MinValue.ToString("o"),
                        TriggerType = build.Triggered?.Type
                    };
                })
            );

            await csvWriter.FlushAsync();
            await writer.FlushAsync();
        }

        private static async Task FlushAndDisposeOutput((Stream stream, TextWriter writer, CsvWriter csvWriter) output)
        {
            var (stream, writer, csvWriter) = output;
            if (stream == null || writer == null || csvWriter == null)
                return;

            await csvWriter.FlushAsync();
            await writer.FlushAsync();

            csvWriter.Dispose();
            writer.Dispose();
            stream.Dispose();
        }

        private static (Stream stream, TextWriter writer, CsvWriter csvWriter) GetAndInitializeWriter<T>(string queueCsvPath)
        {
            var configuration = new CsvConfiguration(CultureInfo.InvariantCulture)
            {
                Delimiter = ";",
                ShouldQuote = (s, context) => true,
                IgnoreBlankLines = true,
                NewLine = NewLine.CRLF
            };

            var stream = File.OpenWrite(queueCsvPath);
            var writer = new StreamWriter(stream, Encoding.UTF8);
            var csvWriter = new CsvWriter(writer, configuration);

            csvWriter.WriteHeader<T>();
            csvWriter.NextRecord();

            return (stream, writer, csvWriter);
        }

        private (string queueCsvPath, string buildsCsvPath, string agentsCsvPath) GetPaths(DateTime time)
        {
            var queueCsvPath = Path.Combine(_folder, $"queue_{time:yyyyMMdd}.csv");
            var buildsCsvPath = Path.Combine(_folder, $"builds_{time:yyyyMMdd}.csv");
            var agentsCsvPath = Path.Combine(_folder, $"agents_{time:yyyyMMdd}.csv");
            return (queueCsvPath, buildsCsvPath, agentsCsvPath);
        }

        private static async Task WriteQueueAsync(Builds result, (Stream stream, TextWriter writer, CsvWriter csvWriter) output, DateTime now)
        {
            await Task.Yield();
            // ReSharper disable once AssignmentIsFullyDiscarded
            _ = Task.Run(() => Console.WriteLine($"Number of builds {result.Count} at {now}"));
            var builds = result.Build;

            var (_, writer, csvWriter) = output;
            await csvWriter.WriteRecordsAsync(builds.Select(build =>
                new BuildInQueue
                {
                    Timestamp = now.ToString("o"),
                    Id = build.Id?.ToString()
                })
            );
            await csvWriter.FlushAsync();
            await writer.FlushAsync();
        }

        private static async Task WriteAgentsAsync(Agents result, (Stream stream, TextWriter writer, CsvWriter csvWriter) output, DateTime now, HashSet<long> buildIds, HashSet<long> buildDumpIds)
        {
            await Task.Yield();

            var agents = result.Agent.ToArray();

            var (_, writer, csvWriter) = output;
            var idleAgents = agents.Where(a => a.Build == null).Select(a => a.Name).ToArray();

            foreach(var build in agents.Where(a => a.Build != null).Select(a => a.Build))
            {
                if (build.Id.HasValue && !buildDumpIds.Contains(build.Id.Value))
                    buildIds.Add(build.Id.Value);
            }

            csvWriter.WriteRecord(new AllAgentsStatus {
                Disabled = agents.Where(a => a.Enabled == false).Count(),
                Total = result.Count ?? 0,
                Idle = (double)idleAgents.Length * 100 / (result.Count ?? 0),
                Timestamp = now.ToString("o"),
                Unauthorized = agents.Where(a => a.Authorized == false).Count(),
                IdleAgents = string.Join(",", idleAgents)
            });
            csvWriter.NextRecord();
            await csvWriter.FlushAsync();
            await writer.FlushAsync();
        }
    }
}
