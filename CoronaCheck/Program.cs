using System;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;

namespace CoronaCheck
{
    class Program
    {
        // To get these values, do a manual check at https://rosenheim.corona-ergebnis.de/
        // and extract it from the developer console of the browser
        private static readonly string _labId = "";
        private static readonly string _hash = "";

        static async Task Main(string[] args)
        {
            var hasTestResult = false;
            while (!hasTestResult)
            {
                Console.WriteLine($"Checking at {DateTime.Now:HH:mm}");
                hasTestResult = await CheckIfTestResultIsAvailableAsync();
                if (!hasTestResult)
                {
                    Console.WriteLine("The test result is not yet available.");
                    await Task.Delay(5 * 60 * 1000); // 5 minutes
                }
            }

            Console.WriteLine("A test result is available!");
            Console.WriteLine("Press any key to exit.");
            Console.ReadKey();
        }

        private static async Task<bool> CheckIfTestResultIsAvailableAsync()
        {
            using var httpClient = new HttpClient();

            var requestVerificationToken = await GetRequestVerificationTokenAsync(httpClient);

            var request = GetRequest(requestVerificationToken);
            var response = await httpClient.SendAsync(request);

            var responseString = await response.Content.ReadAsStringAsync();

            return !responseString.Contains("(ER03)"); // If ER03 is present, the result is not yet available
        }

        private static async Task<string> GetRequestVerificationTokenAsync(HttpClient client)
        {
            var url = "https://rosenheim.corona-ergebnis.de/";
            var httpResponse = await client.GetAsync(url);
            var htmlResponse = await httpResponse.Content.ReadAsStringAsync();

            var htmlDoc = new HtmlAgilityPack.HtmlDocument();
            htmlDoc.LoadHtml(htmlResponse);

            var requestVerificationInputField = htmlDoc
                .DocumentNode
                .Descendants()
                .Single(d => d.Name == "input"
                    && d.Attributes.Any(a => a.Name == "name"
                        && a.Value == "__RequestVerificationToken"));
            var requestVerificationToken = requestVerificationInputField
                .GetAttributes()
                .Single(a => a.Name == "value")
                .Value;
            return requestVerificationToken;
        }

        private static HttpRequestMessage GetRequest(string requestVerificationToken)
        {
            var requestUrl = "https://rosenheim.corona-ergebnis.de/Home/Results";
            var request = new HttpRequestMessage(HttpMethod.Post, requestUrl);

            var requestContent = new MultipartFormDataContent();
            requestContent.Add(new StringContent(_labId), "labId");
            requestContent.Add(new StringContent(_hash), "Hash");
            requestContent.Add(new StringContent(requestVerificationToken), "__RequestVerificationToken");
            request.Content = requestContent;

            return request;
        }
    }
}
