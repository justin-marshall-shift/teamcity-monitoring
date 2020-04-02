using System;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading;
using System.Threading.Tasks;
using TeamCityMonitoring.TeamCityService;

namespace TeamCityMonitoring.Monitoring
{
    public class Monitor
    {
        public async Task RunAsync(string url, string token, string path, int period, CancellationToken cancellationToken)
        {
            var httpClient = new HttpClient();
            httpClient.DefaultRequestHeaders.Authorization =
                new AuthenticationHeaderValue("Bearer", token);
            httpClient.BaseAddress = new Uri(url);
            var client = new Client(httpClient) {BaseUrl = url};

            await LoopAsync(client, period, cancellationToken);
        }

        private async Task LoopAsync(Client client, int period, CancellationToken cancellationToken)
        {
            Console.WriteLine("Beginning of monitoring");

            try
            {
                var delay = TimeSpan.FromMinutes(period);
                while (true)
                {
                    var result = await client.GetBuildsAsync(null, null, cancellationToken);
                    Console.WriteLine($"Number of builds {result.Count} at {DateTime.Now}");
                    await Task.Delay(delay, cancellationToken);
                }
            }
            catch (OperationCanceledException)
            {
                Console.WriteLine("End of monitoring");
            }
        }
    }
}
