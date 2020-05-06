using CsvHelper;
using CsvHelper.Configuration;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Threading.Tasks;
using TeamCityMonitoring.MonitoringMetrics;

namespace TeamCityMonitoring.Monitoring
{
    public class PullRequestMonitor
    {
        private readonly string _token;
        private readonly string _folder;

        public PullRequestMonitor(string token, string folder)
        {
            _token = token;
            _folder = folder;
        }

        public async Task RunAsync(string builds)
        {
            var now = DateTime.UtcNow;
            var branchsCsvPath = GetPaths(now);
            var branchOutput = CommonHelper.GetAndInitializeWriter<BranchStatus>(branchsCsvPath);

            var branchesToCheck = GetBranches(builds);

            await CommonHelper.RetrieveBranchesStatus(branchOutput, branchesToCheck, now, _token);
            await CommonHelper.FlushAndDisposeOutput(branchOutput);
        }

        private HashSet<string> GetBranches(string builds)
        {
            var result = new HashSet<string>();

            var configuration = new CsvConfiguration(CultureInfo.InvariantCulture)
            {
                Delimiter = ";",
                ShouldQuote = (s, context) => true,
                IgnoreBlankLines = true,
                NewLine = NewLine.CRLF,
                BadDataFound = null,
                MissingFieldFound = null
        };

            foreach (var csvFile in Directory.GetFiles(builds, "*.csv"))
            {
                using (var reader = new StreamReader(csvFile))
                using (var csv = new CsvReader(reader, configuration))
                {
                    foreach(var record in csv.GetRecords<BuildDetails>())
                    {
                        if (!string.IsNullOrEmpty(record.Branch))
                            result.Add(record.Branch);
                    }                    
                }
            }

            return result;
        }

        private string GetPaths(DateTime time)
        {
            var branchsCsvPath = Path.Combine(_folder, $"branches_{time:yyyyMMdd}.csv");
            return branchsCsvPath;
        }
    }
}
