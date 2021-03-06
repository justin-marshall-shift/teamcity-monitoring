﻿using System;
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
        private readonly string _githubtoken;

        public DeepMonitor(string url, string token, string folder, string githubtoken)
        {
            _url = url;
            _token = token;
            _folder = folder;
            _githubtoken = githubtoken;
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

            var branchesToCheck = new HashSet<string>();

            var (queueCsvPath, buildsCsvPath, agentsCsvPath, branchsCsvPath) = GetPaths(currentMonitoringTime);
            var queueOutput = CommonHelper.GetAndInitializeWriter<BuildInQueue>(queueCsvPath);
            var buildsOutput = CommonHelper.GetAndInitializeWriter<BuildDetails>(buildsCsvPath); 
            var agentsOutput = CommonHelper.GetAndInitializeWriter<AllAgentsStatus>(agentsCsvPath);
            var branchOutput = CommonHelper.GetAndInitializeWriter<BranchStatus>(branchsCsvPath);

            try
            {
                while (true)
                {
                    if (now.Date != currentMonitoringTime.Date)
                    {
                        await CommonHelper.RetrieveBranchesStatus(branchOutput, branchesToCheck, now, _githubtoken);

                        await CommonHelper.FlushAndDisposeOutput(queueOutput);
                        await CommonHelper.FlushAndDisposeOutput(buildsOutput);
                        await CommonHelper.FlushAndDisposeOutput(agentsOutput);
                        await CommonHelper.FlushAndDisposeOutput(branchOutput);
                        buildIds.Clear();
                        buildDumpIds.Clear();
                        branchesToCheck.Clear();
                        (queueCsvPath, buildsCsvPath, agentsCsvPath, branchsCsvPath) = GetPaths(now);
                        queueOutput = CommonHelper.GetAndInitializeWriter<BuildInQueue>(queueCsvPath);
                        buildsOutput = CommonHelper.GetAndInitializeWriter<BuildDetails>(buildsCsvPath);
                        agentsOutput = CommonHelper.GetAndInitializeWriter<AllAgentsStatus>(agentsCsvPath);
                        branchOutput = CommonHelper.GetAndInitializeWriter<BranchStatus>(branchsCsvPath);
                        currentMonitoringTime = now;
                    }
                                        
                    var agents = await client.ServeAgentsAsync(null, null, null, "count,agent(id,name,enabled,authorized,build)");
                    var queue = await client.GetBuildsAsync(null, null, cancellationToken);

                    RetrieveBuildsToMonitor(buildIds, buildDumpIds,
                        agents.Agent.Where(a => a.Build != null).Select(a => a.Build),
                        queue.Build);

                    var agentsTask = WriteAgentsAsync(agents, agentsOutput, now);
                    var queueTask = WriteQueueAsync(queue, queueOutput, now);
                    var buildsTask = WriteBuildsAsync(client, buildIds, buildDumpIds, buildsOutput, branchesToCheck, force: false, cancellationToken:cancellationToken);

                    await Task.WhenAll(queueTask, buildsTask, agentsTask, Task.Delay(delay, cancellationToken));

                    now = DateTime.UtcNow;
                }
            }
            finally
            {
                await WriteBuildsAsync(client, buildIds, buildDumpIds, buildsOutput, branchesToCheck, force: true, cancellationToken: cancellationToken);
                await CommonHelper.FlushAndDisposeOutput(queueOutput);
                await CommonHelper.FlushAndDisposeOutput(buildsOutput);
                await CommonHelper.FlushAndDisposeOutput(agentsOutput);
                await CommonHelper.RetrieveBranchesStatus(branchOutput, branchesToCheck, now, _githubtoken);
                await CommonHelper.FlushAndDisposeOutput(branchOutput);
            }
        }

        private static void RetrieveBuildsToMonitor(HashSet<long> buildIds, HashSet<long> buildDumpIds, params IEnumerable<Build>[] builds)
        {
            foreach (var build in builds.SelectMany(b => b))
            {
                if (build.Id.HasValue && !buildDumpIds.Contains(build.Id.Value))
                    buildIds.Add(build.Id.Value);
            }
        }

        private static async Task WriteBuildsAsync(Client client, HashSet<long> buildIds, HashSet<long> buildDumpIds,
            (Stream stream, TextWriter writer, CsvWriter csvWriter) output, HashSet<string> branchesToCheck, bool force, CancellationToken cancellationToken)
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
                    branchesToCheck.Add(build.BranchName);
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

        private (string queueCsvPath, string buildsCsvPath, string agentsCsvPath, string branchsCsvPath) GetPaths(DateTime time)
        {
            var queueCsvPath = Path.Combine(_folder, $"queue_{time:yyyyMMdd}.csv");
            var buildsCsvPath = Path.Combine(_folder, $"builds_{time:yyyyMMdd}.csv");
            var agentsCsvPath = Path.Combine(_folder, $"agents_{time:yyyyMMdd}.csv");
            var branchsCsvPath = Path.Combine(_folder, $"branches_{time:yyyyMMdd}.csv");
            return (queueCsvPath, buildsCsvPath, agentsCsvPath, branchsCsvPath);
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

        private static async Task WriteAgentsAsync(Agents result, (Stream stream, TextWriter writer, CsvWriter csvWriter) output, DateTime now)
        {
            await Task.Yield();

            var agents = result.Agent.ToArray();

            var (_, writer, csvWriter) = output;
            var idleAgents = agents.Where(a => a.Build == null).Select(a => a.Name).ToArray();

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
