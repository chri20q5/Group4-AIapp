using System.Text.Json;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;
using jobsapp.Helperbcrypt;
using jobsapp.Helperdb;

namespace jobsapp.signUp
{
    public class SignUp
    {
        private readonly ILogger<SignUp> _logger;

        public SignUp(ILogger<SignUp> logger)
        {
            _logger = logger;
        }

        [Function("SignUp")]
        public async Task<HttpResponseData> Run(
            [HttpTrigger(AuthorizationLevel.Anonymous, "post")] HttpRequestData req)
        {
            try
            {
                _logger.LogInformation("Sign-up function triggered.");

                string requestBody = await new StreamReader(req.Body).ReadToEndAsync();
                using var doc = JsonDocument.Parse(requestBody);
                var root = doc.RootElement;

                string firstName = root.GetProperty("firstName").GetString();
                string lastName = root.GetProperty("lastName").GetString();
                string email = root.GetProperty("email").GetString();
                string password = root.GetProperty("password").GetString();


                if (string.IsNullOrEmpty(email) || string.IsNullOrEmpty(password))
                {
                    var badResponse = req.CreateResponse(System.Net.HttpStatusCode.BadRequest);
                    await badResponse.WriteStringAsync(" email, and password are required.");
                    return badResponse;
                }
                //hash password
                string hashedPassword = BcryptHelper.HashPassword(password);
                //database connection 
                string connectionString = Environment.GetEnvironmentVariable("SqlConnectionString");

                //add user to database
                bool inserted = await DbHelper.InsertUser(firstName, lastName, email, hashedPassword, connectionString);

                var response = req.CreateResponse(inserted
                    ? System.Net.HttpStatusCode.OK
                    : System.Net.HttpStatusCode.Unauthorized);

                await response.WriteStringAsync(inserted
                    ? "User successfully registered."
                    : "Failed to register user.");

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
