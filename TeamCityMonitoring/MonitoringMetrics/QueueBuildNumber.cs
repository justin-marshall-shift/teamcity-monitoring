using System;
using CsvHelper.Configuration.Attributes;

namespace TeamCityMonitoring.MonitoringMetrics
{
    public class QueueBuildNumber
    {
        [Name("Number of builds")]
        public int NumberOfBuilds { get; set; }
        [Name("Timestamp UTC")]
        public DateTime Timestamp { get; set; }
    }

    public class QueuedBuildStatus
    {
        [Name("Number of builds")]
        public int NumberOfBuilds { get; set; }
        [Name("Timestamp UTC")]
        public DateTime Timestamp { get; set; }
        [Name("Build Id")]
        public string Id { get; set; }
        [Name("Build Type Id")]
        public string Type { get; set; }
        [Name("Branch")]
        public string Branch { get; set; }

    }
}
