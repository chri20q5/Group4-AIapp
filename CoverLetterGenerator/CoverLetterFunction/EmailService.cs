using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using SendGrid;
using SendGrid.Helpers.Mail;

namespace CoverLetterFunction.Services
{
    public interface IEmailService
    {
        Task<bool> SendEmailAsync(string toEmail, string recipientName, string coverLetterContent, string? jobTitle = null, string? companyName = null);
    }

    public class EmailService : IEmailService
    {
        private readonly ILogger<EmailService> _logger;
        private readonly IConfiguration _configuration;
        private readonly HttpClient _httpClient;
        private readonly string _provider;

        public EmailService(IConfiguration configuration, ILogger<EmailService> logger, HttpClient httpClient)
        {
            _configuration = configuration;
            _logger = logger;
            _httpClient = httpClient;
            _provider = _configuration["Email:Provider"] ?? "Mailgun";
        }

        public async Task<bool> SendEmailAsync(string toEmail, string recipientName, string coverLetterContent, string? jobTitle = null, string? companyName = null)
        {
            _logger.LogInformation("Sending email to {Email} using {Provider}", toEmail, _provider);

            try
            {
                return _provider.ToLower() switch
                {
                    "mailgun" => await SendViaMailgunAsync(toEmail, recipientName, coverLetterContent, jobTitle, companyName),
                    "sendgrid" => await SendViaSendGridAsync(toEmail, recipientName, coverLetterContent, jobTitle, companyName),
                    "simulate" => await SimulateEmailAsync(toEmail, recipientName, coverLetterContent, jobTitle, companyName),
                    _ => throw new NotSupportedException($"Email provider '{_provider}' is not supported")
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sending email to {Email} using {Provider}", toEmail, _provider);
                return false;
            }
        }

        private string CleanCoverLetterContent(string coverLetterContent)
        {
            if (string.IsNullOrEmpty(coverLetterContent))
                return coverLetterContent;

            // First, remove any bracketed placeholders completely
            var result = System.Text.RegularExpressions.Regex.Replace(coverLetterContent, @"\[.*?\]", "");
            
            // Remove "Dear Hiring Manager," from the beginning since it's already in the email template
            result = System.Text.RegularExpressions.Regex.Replace(result, @"^\s*Dear\s+Hiring\s+Manager,?\s*\n?", "", System.Text.RegularExpressions.RegexOptions.IgnoreCase);
            
            // Remove fake company names and locations that AI might invent
            var fakeCompanyPatterns = new[]
            {
                @"NPL Construction \(S4\)",
                @"at [A-Z][a-zA-Z\s&]+ in [A-Z][a-zA-Z\s,]+",
                @"Berlin, Connecticut",
                @"Mr\.\s+Christopher\s+McGee",
                @"position at [A-Z][a-zA-Z\s&]+\s+in\s+[A-Z][a-zA-Z\s,]+"
            };
            
            foreach (var pattern in fakeCompanyPatterns)
            {
                result = System.Text.RegularExpressions.Regex.Replace(result, pattern, "the position", System.Text.RegularExpressions.RegexOptions.IgnoreCase);
            }
            
            // Remove common AI-generated formatting instructions and template text
            var linesToRemove = new[]
            {
                "here's a draft of a cover letter",
                "draft of a cover letter tailored to",
                "incorporating his profile",
                "incorporating her profile", 
                "aiming for a formal and professional tone",
                "---",
                "your address - optional",
                "date",
                "hiring manager name",
                "company name",
                "company address",
                "mr./ms./mx. hiring manager",
                "if known, otherwise use",
                "as advertised",
                "where you saw the job posting",
                "platform where you saw",
                "e.g., linkedin",
                "e.g., matlab",
                "e.g., simul"
            };

            var lines = result.Split('\n');
            var cleanedLines = new List<string>();

            foreach (var line in lines)
            {
                var cleanLine = line.Trim();
                var lowerLine = cleanLine.ToLower();
                
                // Skip empty lines at the beginning
                if (cleanedLines.Count == 0 && string.IsNullOrWhiteSpace(cleanLine))
                    continue;
                
                // Skip lines that contain formatting instructions
                bool shouldSkip = linesToRemove.Any(removeText => lowerLine.Contains(removeText));
                
                // Skip lines that are just dashes or formatting
                if (cleanLine == "---" || cleanLine.All(c => c == '-' || c == ' '))
                    shouldSkip = true;
                
                // Skip lines that still contain brackets or template text
                if (cleanLine.Contains("[") || cleanLine.Contains("]"))
                    shouldSkip = true;
                
                if (!shouldSkip)
                {
                    cleanedLines.Add(line); // Keep original formatting
                }
            }

            // Join lines back together and ensure proper spacing
            result = string.Join("\n", cleanedLines);
            
            // Remove multiple consecutive newlines
            while (result.Contains("\n\n\n"))
            {
                result = result.Replace("\n\n\n", "\n\n");
            }
            
            // Final cleanup: remove any remaining bracketed text
            result = System.Text.RegularExpressions.Regex.Replace(result, @"\[.*?\]", "");
            
            // Clean up any double spaces left by bracket removal
            while (result.Contains("  "))
            {
                result = result.Replace("  ", " ");
            }
            
            // If the content seems incomplete (ends abruptly), add a professional closing
            if (!string.IsNullOrEmpty(result) && !result.TrimEnd().EndsWith(".") && !result.TrimEnd().EndsWith("!"))
            {
                result = result.TrimEnd() + ".";
            }
            
            return result.Trim();
        }

        private async Task<bool> SendViaMailgunAsync(string toEmail, string recipientName, string coverLetterContent, string? jobTitle = null, string? companyName = null)
        {
            const int maxRetries = 3;
            const int baseDelayMs = 1000;

            string? apiKey = _configuration["Mailgun:ApiKey"];
            string? domain = _configuration["Mailgun:Domain"];
            string? fromEmail = _configuration["Mailgun:FromEmail"];
            string? fromName = _configuration["Mailgun:FromName"];
            string? baseUrl = _configuration["Mailgun:BaseUrl"] ?? "https://api.mailgun.net";

            if (string.IsNullOrEmpty(apiKey) || apiKey == "SIMULATE_LOCALLY")
            {
                return await SimulateEmailAsync(toEmail, recipientName, coverLetterContent, jobTitle, companyName);
            }

            if (string.IsNullOrEmpty(domain))
            {
                _logger.LogError("Mailgun domain is not configured");
                return false;
            }

            fromEmail ??= $"noreply@{domain}";
            fromName ??= "Cover Letter Service";

            string subject = _configuration["Email:Subject"] ?? "Your Generated Cover Letter";
            string messageTemplate = _configuration["Email:MessageTemplate"] ?? 
                "Dear {0},\n\nPlease find your generated cover letter below:\n\n{1}\n\nBest regards,\nThe Cover Letter Service Team";

            // Clean the cover letter content first
            string cleanedCoverLetter = CleanCoverLetterContent(coverLetterContent);

            // Format subject and body with job information
            string emailBody;
            if (!string.IsNullOrEmpty(jobTitle))
            {
                subject = string.Format(subject, recipientName, jobTitle);
                emailBody = string.Format(messageTemplate, recipientName, jobTitle, cleanedCoverLetter);
            }
            else
            {
                subject = string.Format(subject, recipientName, "Unknown Position");
                emailBody = string.Format(messageTemplate, recipientName, cleanedCoverLetter);
            }

            for (int attempt = 1; attempt <= maxRetries; attempt++)
            {
                try
                {
                    var formContent = new MultipartFormDataContent
                    {
                        { new StringContent(fromEmail), "from" },
                        { new StringContent(toEmail), "to" },
                        { new StringContent(subject), "subject" },
                        { new StringContent(emailBody), "text" }
                    };

                    string url = $"{baseUrl}/v3/{domain}/messages";
                    
                    var request = new HttpRequestMessage(HttpMethod.Post, url)
                    {
                        Content = formContent
                    };

                    string credentials = Convert.ToBase64String(Encoding.ASCII.GetBytes($"api:{apiKey}"));
                    request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Basic", credentials);

                    _logger.LogInformation("Sending Mailgun email (attempt {Attempt}/{MaxRetries}) to {Email}", attempt, maxRetries, toEmail);
                    
                    var response = await _httpClient.SendAsync(request);
                    
                    if (response.IsSuccessStatusCode)
                    {
                        var responseContent = await response.Content.ReadAsStringAsync();
                        _logger.LogInformation("Mailgun email sent successfully to {Email}: {Response}", toEmail, responseContent);
                        return true;
                    }
                    else
                    {
                        var errorContent = await response.Content.ReadAsStringAsync();
                        _logger.LogWarning("Mailgun attempt {Attempt} failed with status {StatusCode}: {ErrorContent}", 
                            attempt, response.StatusCode, errorContent);
                        
                        if (attempt == maxRetries)
                        {
                            _logger.LogError("Mailgun email failed after {MaxRetries} attempts to {Email}", maxRetries, toEmail);
                            return false;
                        }
                        
                        int delay = baseDelayMs * (int)Math.Pow(2, attempt - 1);
                        await Task.Delay(delay);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Mailgun attempt {Attempt} threw exception", attempt);
                    
                    if (attempt == maxRetries)
                    {
                        throw;
                    }
                    
                    int delay = baseDelayMs * (int)Math.Pow(2, attempt - 1);
                    await Task.Delay(delay);
                }
            }

            return false;
        }

        private async Task<bool> SendViaSendGridAsync(string toEmail, string recipientName, string coverLetterContent, string? jobTitle = null, string? companyName = null)
        {            string? apiKey = _configuration["SendGrid:ApiKey"];

            if (string.IsNullOrEmpty(apiKey) || apiKey == "SIMULATE_LOCALLY")
            {
                return await SimulateEmailAsync(toEmail, recipientName, coverLetterContent, jobTitle, companyName);
            }

            string? fromEmail = _configuration["SendGrid:FromEmail"];
            string? fromName = _configuration["SendGrid:FromName"] ?? "Cover Letter Service";

            if (string.IsNullOrEmpty(fromEmail))
            {
                _logger.LogError("SendGrid from email is not configured");
                return false;
            }

            string subject = _configuration["Email:Subject"] ?? "Your Generated Cover Letter";
            string messageTemplate = _configuration["Email:MessageTemplate"] ?? 
                "Dear {0},\n\nPlease find your generated cover letter below:\n\n{1}\n\nBest regards,\nThe Cover Letter Service Team";

            // Clean the cover letter content first
            string cleanedCoverLetter = CleanCoverLetterContent(coverLetterContent);

            // Format subject and body with job information
            string emailBody;
            if (!string.IsNullOrEmpty(jobTitle))
            {
                subject = string.Format(subject, recipientName, jobTitle);
                emailBody = string.Format(messageTemplate, recipientName, jobTitle, cleanedCoverLetter);
            }
            else
            {
                subject = string.Format(subject, recipientName, "Unknown Position");
                emailBody = string.Format(messageTemplate, recipientName, cleanedCoverLetter);
            }

            const int maxRetries = 3;
            const int baseDelayMs = 1000;

            for (int attempt = 1; attempt <= maxRetries; attempt++)
            {
                try
                {
                    var client = new SendGridClient(apiKey);
                    var from = new EmailAddress(fromEmail, fromName);
                    var to = new EmailAddress(toEmail, recipientName);
                    var msg = MailHelper.CreateSingleEmail(from, to, subject, emailBody, null);

                    _logger.LogInformation("Sending SendGrid email (attempt {Attempt}/{MaxRetries}) to {Email}", attempt, maxRetries, toEmail);
                    
                    var response = await client.SendEmailAsync(msg);
                    
                    if (response.StatusCode == HttpStatusCode.Accepted)
                    {
                        _logger.LogInformation("SendGrid email sent successfully to {Email}", toEmail);
                        return true;
                    }
                    else
                    {
                        var responseBody = await response.Body.ReadAsStringAsync();
                        _logger.LogWarning("SendGrid attempt {Attempt} failed with status {StatusCode}: {ResponseBody}", 
                            attempt, response.StatusCode, responseBody);
                        
                        if (attempt == maxRetries)
                        {
                            _logger.LogError("SendGrid email failed after {MaxRetries} attempts to {Email}", maxRetries, toEmail);
                            return false;
                        }
                        
                        int delay = baseDelayMs * (int)Math.Pow(2, attempt - 1);
                        await Task.Delay(delay);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "SendGrid attempt {Attempt} threw exception", attempt);
                    
                    if (attempt == maxRetries)
                    {
                        throw;
                    }
                    
                    int delay = baseDelayMs * (int)Math.Pow(2, attempt - 1);
                    await Task.Delay(delay);
                }
            }

            return false;
        }

        private async Task<bool> SimulateEmailAsync(string toEmail, string recipientName, string coverLetterContent, string? jobTitle = null, string? companyName = null)
        {
            _logger.LogInformation("🚀 SIMULATED EMAIL 🚀");
            _logger.LogInformation("To: {Email}", toEmail);
            _logger.LogInformation("Recipient: {Name}", recipientName);
            _logger.LogInformation("Cover Letter Content Preview: {Preview}...", 
                coverLetterContent.Length > 100 ? coverLetterContent.Substring(0, 100) : coverLetterContent);
            _logger.LogInformation("✅ Email simulation completed successfully");
            
            // Simulate some processing time
            await Task.Delay(500);
            return true;
        }
    }
}
