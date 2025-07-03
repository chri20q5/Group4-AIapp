using System.IO;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

namespace GetJobsFunction
{
    public class GetJobsFront
    {
        private readonly HttpClient _httpClient = new HttpClient();

        [Function("GetJobs")]
        public async Task<HttpResponseData> Run(
            [HttpTrigger(AuthorizationLevel.Anonymous, "post")] HttpRequestData req,
            FunctionContext context)
        {
            var logger = context.GetLogger("GetJobsFront");
            logger.LogInformation("Processing job request.");

            string requestBody = await new StreamReader(req.Body).ReadToEndAsync();
            dynamic data = JsonConvert.DeserializeObject(requestBody);
            string keywords = data?.keywords;
            string location = data?.location;

            if (string.IsNullOrEmpty(keywords) || string.IsNullOrEmpty(location))
            {
                var badResponse = req.CreateResponse(System.Net.HttpStatusCode.BadRequest);
                await badResponse.WriteStringAsync("Missing 'keywords' or 'location'");
                return badResponse;
            }

            var joobleBody = new
            {
                keywords,
                location
            };

            var content = new StringContent(JsonConvert.SerializeObject(joobleBody), Encoding.UTF8, "application/json");

            string apiKey = "b9057e64-ba51-449b-8229-d4ac82ebe0b1";
            var joobleResponse = await _httpClient.PostAsync($"https://jooble.org/api/{apiKey}", content);
            var resultJson = await joobleResponse.Content.ReadAsStringAsync();

            var okResponse = req.CreateResponse(System.Net.HttpStatusCode.OK);
            okResponse.Headers.Add("Content-Type", "application/json");
            await okResponse.WriteStringAsync(resultJson);

            return okResponse;
        }
    }
}
