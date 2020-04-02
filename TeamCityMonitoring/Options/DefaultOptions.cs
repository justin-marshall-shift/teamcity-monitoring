using CommandLine;

namespace TeamCityMonitoring.Options
{
    public class DefaultOptions
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
}
