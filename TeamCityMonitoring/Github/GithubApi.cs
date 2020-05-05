using JsonFx.Json;
using System;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;

namespace TeamCityMonitoring.Github
{
    public class GithubApi
    {
        private const string GithubApiUri = "https://api.github.com";

        static GithubApi()
        {
            ServicePointManager.SecurityProtocol |= SecurityProtocolType.Tls12;
        }

        public GithubApi(string userAgent, string userAgentVersion, string accessToken)
        {
            UserAgent = userAgent;
            UserAgentVersion = userAgentVersion;
            AccessToken = accessToken;
        }

        private string UserAgent { get; }
        private string UserAgentVersion { get; }
        private string AccessToken { get; }
        private string Repository => "shift-technology/shift";
        private string BaseUri => $"{GithubApiUri}/repos/{Repository}";

        public async Task<PullRequest> GetPullRequestAsync(int prNumber)
        {
            var pullRequest = await CallApiAsync(HttpMethod.Get, $"/pulls/{prNumber}")
                .ReadAs<PullRequest>();

            return pullRequest;
        }

        private async Task<string> CallApiAsync<T>(HttpMethod method, string endpoint, T body)
        {
            using (var client = new HttpClient())
            {
                var message = new HttpRequestMessage(method, $"{BaseUri}{endpoint}");
                message.Headers.Authorization = new AuthenticationHeaderValue("token", AccessToken);
                message.Headers.UserAgent.Add(new ProductInfoHeaderValue(UserAgent, UserAgentVersion));

                if (body != null)
                    message.Content =
                        new StringContent(new JsonWriter().Write(body), Encoding.UTF8, "application/json");

                var response = await client.SendAsync(message);

                if (!response.IsSuccessStatusCode)
                    return null;

                return await response.Content.ReadAsStringAsync();
            }
        }

        private async Task<string> CallApiAsync(HttpMethod method, string endpoint)
        {
            return await CallApiAsync<string>(method, endpoint, null);
        }
    }

    public static class JsonResponseReader
    {
        public static async Task<T> ReadAs<T>(this Task<string> resultTask)
        {
            return new JsonReader().Read<T>(await resultTask);
        }
    }
}
