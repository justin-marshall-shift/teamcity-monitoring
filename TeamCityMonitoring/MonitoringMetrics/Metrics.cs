using System;
using CsvHelper.Configuration.Attributes;

namespace TeamCityMonitoring.MonitoringMetrics
{
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

    public class BuildInQueue
    {
        [Name("Timestamp UTC")]
        public DateTime Timestamp { get; set; }
        [Name("Build Id")]
        public string Id { get; set; }
    }

    public class BuildDetails
    {
        [Name("Build Id")]
        public string Id { get; set; }
        [Name("Build Type Id")]
        public string Type { get; set; }
        [Name("Branch")]
        public string Branch { get; set; }
        [Name("Status")]
        public string Status { get; set; }
        [Name("State")]
        public string State { get; set; }
        [Name("Queued time")]
        public DateTime QueueDate { get; set; }
        [Name("Start time")]
        public DateTime StartDate { get; set; }
        [Name("Finished time")]
        public DateTime FinishedDate { get; set; }
        [Name("Agent")]
        public string Agent { get; set; }
        [Name("Trigger")]
        public string Trigger { get; set; }
        [Name("Trigger type")]
        public string TriggerType { get; set; }
        [Name("Trigger time")]
        public DateTime TriggerTime { get; set; }
    }
}
