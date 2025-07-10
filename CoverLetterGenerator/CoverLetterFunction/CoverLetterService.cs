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
                new { role = "system", content = "You are an expert in professional writing. Generate a formal and tailored cover letter based on the user's profile and the job description." },
                new { role = "user", content = $"My profile: {userProfile}\n\nJob description: {jobDescription}\n\nPlease write a personalized cover letter in 1 or 2 sentences." }
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
