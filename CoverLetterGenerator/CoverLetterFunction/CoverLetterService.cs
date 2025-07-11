using System;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

public class CoverLetterService : ICoverLetterService
{
    private readonly HttpClient _httpClient;
    private readonly string _apiKey;
    private readonly string _model;
    private readonly string _apiUrl;
    private readonly IBlobStorageService _blobStorageService;
    private readonly ILogger<CoverLetterService> _logger;

    public CoverLetterService(
        IHttpClientFactory httpClientFactory, 
        IConfiguration configuration,
        IBlobStorageService blobStorageService,
        ILogger<CoverLetterService> logger)
    {
        _httpClient = httpClientFactory.CreateClient();
        _apiKey = configuration["ApiKey"] ?? throw new ArgumentNullException("ApiKey configuration is missing");
        _model = configuration["ModelName"] ?? "gemma3:1b";
        _apiUrl = configuration["ApiUrl"] ?? "https://ollama.chri20q5.org/api/chat/completions";
        _blobStorageService = blobStorageService;
        _logger = logger;
    }

    public async Task<string> GenerateCoverLetterAsync(string jobDescription, string userProfile)
    {
        _httpClient.DefaultRequestHeaders.Clear();
        _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _apiKey);

        var requestBody = new
        {
            model = _model,
            temperature = 0.7,
            messages = new[]
            {
                new { role = "system", content = "You are an expert career counselor and professional writer. You must write a complete, professional cover letter using ONLY the specific information provided. You must NEVER invent names, companies, locations, or other details that are not explicitly provided in the user profile and job description." },
                new { role = "user", content = $"User Profile: {userProfile}\n\nJob Description: {jobDescription}\n\nWrite a complete professional cover letter following these STRICT rules:\n1. EXACTLY 120-150 words (count carefully)\n2. Do NOT start with 'Dear Hiring Manager,' - start directly with the content\n3. Use ONLY the specific job title, company name, and location from the job description\n4. Use ONLY the specific user information provided (name, location, experience, etc.)\n5. If company name is not provided, use 'the company' or 'your organization'\n6. If location is not provided, do not mention location\n7. If user's name is not provided, do not mention specific names\n8. NEVER invent or guess: names, companies, locations, software, previous employers, etc.\n9. Keep the content general but professional if specific details are missing\n10. End with 'Sincerely,' followed by a new line\n11. Focus on transferable skills and enthusiasm for the role\n12. Must be complete and ready to send immediately\n\nCRITICAL RULES:\n- NO invented details anywhere in the letter\n- NO fake company names, locations, or software mentions\n- Use only what is explicitly provided in the inputs\n- If information is missing, write around it professionally\n- Keep it professional but general when specifics aren't available\n\nWrite the complete cover letter now:" }
            }
        };

        var json = JsonSerializer.Serialize(requestBody);
        var content = new StringContent(json, Encoding.UTF8, "application/json");

        var response = await _httpClient.PostAsync(_apiUrl, content);

        if (response.IsSuccessStatusCode)
        {
            var responseBody = await response.Content.ReadAsStringAsync();

            using var doc = JsonDocument.Parse(responseBody);
            var messageContent = doc
                .RootElement
                .GetProperty("choices")[0]
                .GetProperty("message")
                .GetProperty("content")
                .GetString();

            return messageContent ?? "[No content returned]";
        }
        else
        {
            throw new Exception($"Error {response.StatusCode}: {await response.Content.ReadAsStringAsync()}");
        }
    }
    
    public async Task<string> SaveCoverLetterToBlobAsync(
        string coverLetter, 
        string email, 
        string name, 
        string jobTitle, 
        string companyName)
    {
        try
        {
            _logger.LogInformation("Saving cover letter to blob storage for {Email}", email);
            
            var coverLetterData = new CoverLetterData
            {
                CoverLetter = coverLetter,
                Email = email,
                Name = name,
                JobTitle = jobTitle,
                CompanyName = companyName,
                CreatedAt = DateTime.UtcNow
            };
            
            string blobName = await _blobStorageService.UploadCoverLetterAsync(coverLetterData);
            
            _logger.LogInformation("Cover letter saved to blob: {BlobName}", blobName);
            return blobName;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error saving cover letter to blob storage");
            throw;
        }
    }
}
