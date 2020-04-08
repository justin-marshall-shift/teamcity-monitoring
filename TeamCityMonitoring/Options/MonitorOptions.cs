using CommandLine;

namespace TeamCityMonitoring.Options
{
    [Verb("monitor", HelpText = "Monitor the TeamCity server build queue.")]
    public class MonitorOptions
    {
        [Option('u', "url", Required = true, HelpText = "TeamCity server url.")]
        public string Url { get; set; }

        [Option('t', "token", Required = true, HelpText = "TeamCity REST API token.")]
        public string Token { get; set; }

        [Option('c', "csv", Required = true, HelpText = "Path to the file where statistics will be dumped.")]
        public string Csv { get; set; }

        [Option('d', "duration", Required = true, HelpText = "Define the duration of your monitoring in hours.")]
        public int Duration { get; set; }

        [Option('p', "period", Required = true, HelpText = "Define the time between two samplings in minutes.")]
        public int Period { get; set; }
    }

    [Verb("deep", HelpText = "Deep monitoring of the TeamCity server build queue + build duration.")]
    public class DeepMonitorOptions
    {
        [Option('u', "url", Required = true, HelpText = "TeamCity server url.")]
        public string Url { get; set; }

        [Option('t', "token", Required = true, HelpText = "TeamCity REST API token.")]
        public string Token { get; set; }

        [Option('f', "folder", Required = true, HelpText = "Path to the folder where statistics will be dumped.")]
        public string Folder { get; set; }

        [Option('d', "duration", Required = false, HelpText = "Define the duration of your monitoring in hours.")]
        public int Duration { get; set; }

        [Option('p', "period", Required = true, HelpText = "Define the time between two samplings in minutes.")]
        public int Period { get; set; }
    }

    [Verb("graph", HelpText = "Create metrics graphs from your monitoring file.")]
    public class GraphOptions
    {
        [Option('c', "csv", Required = true, HelpText = "Path to the file where statistics have been dumped.")]
        public string Csv { get; set; }

        [Option('e', "excel", Required = true, HelpText = "Path where your excel file will be dumped.")]
        public string Excel { get; set; }
    }
}
