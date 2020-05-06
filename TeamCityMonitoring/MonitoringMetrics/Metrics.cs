using System;
using CsvHelper.Configuration.Attributes;

namespace TeamCityMonitoring.MonitoringMetrics
{
    public class QueuedBuildStatus
    {
        [Name("Number of builds")]
        public int NumberOfBuilds { get; set; }
        [Name("Timestamp UTC")]
        public string Timestamp { get; set; }
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
        public string Timestamp { get; set; }
        [Name("Build Id")]
        public string Id { get; set; }
    }

    public class AllAgentsStatus
    {
        [Name("Timestamp UTC")]
        public string Timestamp { get; set; }
        [Name("Number of disabled")]
        public int Disabled { get; set; }
        [Name("Number of unauthorized")]
        public int Unauthorized { get; set; }
        [Name("Total")]
        public int Total { get; set; }
        [Name("Idle percentage")]
        public double Idle { get; set; }
        [Name("List of idle agents")]
        public string IdleAgents { get; set; }
    }

    public class BranchStatus
    {
        [Name("Branch")]
        public string Branch { get; set; }
        [Name("State")]
        public string State { get; set; }
        [Name("StatusDate")]
        public string StatusDate { get; set; }
        [Name("CreatedDate")]
        public string CreatedDate { get; set; }
        [Name("ClosedDate")]
        public string ClosedDate { get; set; }
        [Name("MergedDate")]
        public string MergedDate { get; set; }
        [Name("Url")]
        public string Url { get; set; }
        [Name("Title")]
        public string Title { get; set; }
        [Name("WIP")]
        public bool IsWip { get; set; }
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
        public string QueueDate { get; set; }
        [Name("Start time")]
        public string StartDate { get; set; }
        [Name("Finished time")]
        public string FinishedDate { get; set; }
        [Name("Agent")]
        public string Agent { get; set; }
        [Name("Trigger")]
        public string Trigger { get; set; }
        [Name("Trigger type")]
        public string TriggerType { get; set; }
        [Name("Trigger time")]
        public string TriggerTime { get; set; }
    }
}
