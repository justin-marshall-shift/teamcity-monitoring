namespace TeamCityMonitoring.Github
{
    public class PullRequest
    {
        public string title { get; set; }
        public string url { get; set; }
        public string state { get; set; }
        public string created_at { get; set; }
        public string closed_at { get; set; }
        public string merged_at { get; set; }
    }
}
