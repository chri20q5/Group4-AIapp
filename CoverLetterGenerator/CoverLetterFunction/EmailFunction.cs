using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using System.Text.Json;
using System.Threading.Tasks;
using CoverLetterFunction.Services;

namespace CoverLetterFunction
{
    public class EmailFunction
    {
        private readonly ILogger<EmailFunction> _logger;
        private readonly IEmailService _emailService;
        private readonly IBlobStorageService _blobStorageService;

        public EmailFunction(ILogger<EmailFunction> logger, IEmailService emailService, IBlobStorageService blobStorageService)
        {
            _logger = logger;
            _emailService = emailService;
            _blobStorageService = blobStorageService;
        }

        [Function("ProcessCoverLetterEmail")]
        public async Task ProcessCoverLetterEmail(
            [BlobTrigger("coverletters/{name}", Connection = "AzureWebJobsStorage")] byte[] blobContent,
            string name)
        {
            _logger.LogInformation("Processing cover letter blob: {BlobName}", name);

            try
            {
                // Convert blob content to string
                string jsonContent = System.Text.Encoding.UTF8.GetString(blobContent);
                
                if (string.IsNullOrEmpty(jsonContent))
                {
                    _logger.LogWarning("Blob {BlobName} is empty, skipping", name);
                    return;
                }

                // Parse the blob content to get the cover letter data
                var coverLetterData = JsonSerializer.Deserialize<CoverLetterData>(jsonContent);
                
                if (coverLetterData == null)
                {
                    _logger.LogWarning("Could not parse cover letter data from {BlobName}", name);
                    return;
                }

                // Validate required fields
                if (string.IsNullOrEmpty(coverLetterData.Email) || 
                    string.IsNullOrEmpty(coverLetterData.Name) || 
                    string.IsNullOrEmpty(coverLetterData.CoverLetter))
                {
                    _logger.LogWarning("Cover letter data is incomplete in {BlobName}. Email: {Email}, Name: {Name}, CoverLetter length: {Length}", 
                        name, coverLetterData.Email, coverLetterData.Name, coverLetterData.CoverLetter?.Length ?? 0);
                    return;
                }

                // Send the email
                _logger.LogInformation("Sending cover letter email to {Email} for {Name}", coverLetterData.Email, coverLetterData.Name);
                bool emailSent = await _emailService.SendEmailAsync(
                    coverLetterData.Email,
                    coverLetterData.Name,
                    coverLetterData.CoverLetter,
                    coverLetterData.JobTitle,
                    coverLetterData.CompanyName);
                
                if (emailSent)
                {
                    // Delete the blob after successful processing
                    await _blobStorageService.DeleteBlobAsync(name);
                    _logger.LogInformation("Successfully processed and deleted blob: {BlobName}", name);
                }
                else
                {
                    _logger.LogWarning("Email sending failed for {BlobName}, keeping the blob for retry", name);
                }
            }
            catch (JsonException ex)
            {
                _logger.LogError(ex, "Error parsing JSON from blob {BlobName}", name);
            }
            catch (System.Exception ex)
            {
                _logger.LogError(ex, "Error processing blob {BlobName}", name);
                // Don't delete the blob on error so it can be retried
            }
        }
    }

    public class CoverLetterData
    {
        public string Email { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string CoverLetter { get; set; } = string.Empty;
        public string? JobTitle { get; set; }
        public string? CompanyName { get; set; }
        public DateTime? CreatedAt { get; set; }
    }
}
