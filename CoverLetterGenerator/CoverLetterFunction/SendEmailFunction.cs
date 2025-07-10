using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;
using System.Net;
using System.Text.Json;
using CoverLetterFunction.Services;

namespace CoverLetterFunction
{
    public class SendEmailFunction
    {
        private readonly ILogger<SendEmailFunction> _logger;
        private readonly IEmailService _emailService;

        public SendEmailFunction(ILogger<SendEmailFunction> logger, IEmailService emailService)
        {
            _logger = logger;
            _emailService = emailService;
        }

        [Function("SendEmail")]
        public async Task<HttpResponseData> SendEmail(
            [HttpTrigger(AuthorizationLevel.Anonymous, "post")] HttpRequestData req)
        {
            _logger.LogInformation("SendEmail function triggered");

            try
            {
                // Read request body
                string requestBody = await new StreamReader(req.Body).ReadToEndAsync();
                
                if (string.IsNullOrEmpty(requestBody))
                {
                    var badRequestResponse = req.CreateResponse(HttpStatusCode.BadRequest);
                    await badRequestResponse.WriteStringAsync("Request body is required");
                    return badRequestResponse;
                }

                // Parse the email request
                var emailRequest = JsonSerializer.Deserialize<EmailRequest>(requestBody);
                
                if (emailRequest == null)
                {
                    var badRequestResponse = req.CreateResponse(HttpStatusCode.BadRequest);
                    await badRequestResponse.WriteStringAsync("Invalid request format");
                    return badRequestResponse;
                }

                // Validate required fields
                if (string.IsNullOrEmpty(emailRequest.Email) || 
                    string.IsNullOrEmpty(emailRequest.Name) || 
                    string.IsNullOrEmpty(emailRequest.CoverLetter))
                {
                    var badRequestResponse = req.CreateResponse(HttpStatusCode.BadRequest);
                    await badRequestResponse.WriteStringAsync("Email, Name, and CoverLetter are required fields");
                    return badRequestResponse;
                }

                // Send the email
                _logger.LogInformation("Sending email to {Email} for {Name}", emailRequest.Email, emailRequest.Name);
                bool success = await _emailService.SendEmailAsync(
                    emailRequest.Email,
                    emailRequest.Name,
                    emailRequest.CoverLetter,
                    emailRequest.JobTitle,
                    emailRequest.CompanyName);

                // Create response
                var response = req.CreateResponse(success ? HttpStatusCode.OK : HttpStatusCode.InternalServerError);
                
                var result = new
                {
                    success = success,
                    message = success ? "Email sent successfully" : "Failed to send email",
                    recipient = emailRequest.Email,
                    timestamp = DateTime.UtcNow
                };

                await response.WriteStringAsync(JsonSerializer.Serialize(result));
                response.Headers.Add("Content-Type", "application/json");

                return response;
            }
            catch (JsonException ex)
            {
                _logger.LogError(ex, "Error parsing request JSON");
                var errorResponse = req.CreateResponse(HttpStatusCode.BadRequest);
                await errorResponse.WriteStringAsync("Invalid JSON format");
                return errorResponse;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sending email");
                var errorResponse = req.CreateResponse(HttpStatusCode.InternalServerError);
                await errorResponse.WriteStringAsync($"Internal server error: {ex.Message}");
                return errorResponse;
            }
        }
    }

    public class EmailRequest
    {
        public string Email { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string CoverLetter { get; set; } = string.Empty;
        public string? JobTitle { get; set; }
        public string? CompanyName { get; set; }
    }
}
