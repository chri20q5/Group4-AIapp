using System.Text.Json;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;
using jobsapp.Helperbcrypt;
using jobsapp.Helperdb;
using System.Security;

namespace jobsapp.signIn
{
    public class SignIn
    {
        private readonly ILogger<SignIn> _logger;

        public SignIn(ILogger<SignIn> logger)
        {
            _logger = logger;
        }

        [Function("SignIn")]
        public async Task<HttpResponseData> Run(
            [HttpTrigger(AuthorizationLevel.Anonymous, "post")] HttpRequestData req)
        {
            try
            {
                _logger.LogInformation("Sign-in function triggered.");

                string requestBody = await new StreamReader(req.Body).ReadToEndAsync();
                using var doc = JsonDocument.Parse(requestBody);
                var root = doc.RootElement;

                string email = root.GetProperty("email").GetString();
                string password = root.GetProperty("password").GetString();

                //database connection 
                string connectionString = Environment.GetEnvironmentVariable("SqlConnectionString");

                //checking email and password 
                if (string.IsNullOrEmpty(email) || string.IsNullOrEmpty(password))
                {
                    var badResponse = req.CreateResponse(System.Net.HttpStatusCode.BadRequest);
                    await badResponse.WriteStringAsync(" email, and password are required.");
                    return badResponse;
                }

                //find User and get hashed password
                string? hash = await DbHelper.GetUserByEmail(email, connectionString);
                if (hash == null)
                {
                    var notFound = req.CreateResponse(System.Net.HttpStatusCode.NotFound);
                    await notFound.WriteStringAsync("User not found.");
                    return notFound;
                }

                //using bcrypt to comapre hash to user input 
                bool verification = BcryptHelper.VerifyPassword(password, hash);


                var response = req.CreateResponse(verification
                    ? System.Net.HttpStatusCode.OK
                    : System.Net.HttpStatusCode.Unauthorized);

                await response.WriteStringAsync(verification
                    ? "User successfully logged in"
                    : "Incorrect password");

                return response;
            }
            catch (Exception e)
            {

                var error = req.CreateResponse(System.Net.HttpStatusCode.InternalServerError);
                await error.WriteStringAsync($"Error: {e.Message}");
                return error;

            }
        }
    }
}
