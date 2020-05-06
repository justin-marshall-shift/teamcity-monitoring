using CsvHelper;
using CsvHelper.Configuration;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using TeamCityMonitoring.MonitoringMetrics;

namespace TeamCityMonitoring.Monitoring
{
    public static class CommonHelper
    {
        public static async Task FlushAndDisposeOutput((Stream stream, TextWriter writer, CsvWriter csvWriter) output)
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

        public static (Stream stream, TextWriter writer, CsvWriter csvWriter) GetAndInitializeWriter<T>(string queueCsvPath)
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

        public static async Task RetrieveBranchesStatus((Stream stream, TextWriter writer, CsvWriter csvWriter) output, HashSet<string> branchesToCheck, DateTime now, string token)
        {
            var githubclient = new Github.GithubApi("Shift-TeamCity-Monitoring", "1.0", token);
            var (_, writer, csvWriter) = output;

            foreach (var branch in branchesToCheck)
            {
                var id = GetBranchId(branch);
                if (!id.HasValue)
                    continue;

                var pullrequest = await githubclient.GetPullRequestAsync(id.Value);

                if (pullrequest == null)
                    continue;

                csvWriter.WriteRecord(new BranchStatus
                {
                    Branch = branch,
                    ClosedDate = (string.IsNullOrEmpty(pullrequest.closed_at) ? DateTime.MinValue : DateTime.Parse(pullrequest.closed_at)).ToUniversalTime().ToString("o"),
                    CreatedDate = (string.IsNullOrEmpty(pullrequest.created_at) ? DateTime.MinValue : DateTime.Parse(pullrequest.created_at)).ToUniversalTime().ToString("o"),
                    MergedDate = (string.IsNullOrEmpty(pullrequest.merged_at) ? DateTime.MinValue : DateTime.Parse(pullrequest.merged_at)).ToUniversalTime().ToString("o"),
                    IsWip = pullrequest.title?.Contains("[WIP]") == true,
                    State = pullrequest.state,
                    StatusDate = now.Date.ToUniversalTime().ToString("o"),
                    Title = pullrequest.title,
                    Url = pullrequest.url
                });

                csvWriter.NextRecord();
                await csvWriter.FlushAsync();
                await writer.FlushAsync();
            }
        }

        public static int? GetBranchId(string origin)
        {
            var branch = origin;

            if (string.IsNullOrEmpty(branch))
                return null;

            if (!branch.StartsWith("refs/pull/") || !branch.EndsWith("/head"))
                return null;

            branch = branch.Substring("refs/pull/".Length, branch.Length - "refs/pull/".Length);
            branch = branch.Substring(0, branch.Length - "/head".Length);

            return int.Parse(branch);
        }


    }
}
