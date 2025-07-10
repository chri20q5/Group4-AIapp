using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using Azure.Identity;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

public interface IBlobStorageService
{
    Task<string> UploadCoverLetterAsync(CoverLetterData coverLetterData);
    Task<IEnumerable<BlobItem>> ListBlobsAsync();
    Task<string> DownloadBlobContentAsync(string blobName);
    Task DeleteBlobAsync(string blobName);
    Task<bool> BlobExistsAsync(string blobName);
}

public class BlobStorageService : IBlobStorageService
{
    private readonly BlobContainerClient _containerClient;
    private readonly ILogger<BlobStorageService> _logger;

    public BlobStorageService(IConfiguration configuration, ILogger<BlobStorageService> logger)
    {
        _logger = logger;

        try
        {
            string containerName = configuration["BlobContainerName"] 
                ?? throw new ArgumentNullException("BlobContainerName is missing in configuration");

            string storageAccountName = configuration["StorageAccountName"] 
                ?? throw new ArgumentNullException("StorageAccountName is missing in configuration");

            // Use managed identity for authentication to blob storage
            // In local development, this will use the DefaultAzureCredential chain
            // In Azure Functions, this will use the managed identity
            var blobServiceUri = new Uri($"https://{storageAccountName}.blob.core.windows.net/");
            BlobServiceClient blobServiceClient = new BlobServiceClient(blobServiceUri, new DefaultAzureCredential());
            _containerClient = blobServiceClient.GetBlobContainerClient(containerName);
            
            // Create the container if it doesn't exist (will use managed identity permissions)
            _containerClient.CreateIfNotExists();
            
            _logger.LogInformation("BlobStorageService initialized with managed identity for container: {ContainerName}", containerName);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error initializing BlobStorageService");
            throw;
        }
    }

    public async Task<string> UploadCoverLetterAsync(CoverLetterData coverLetterData)
    {
        try
        {
            // Create a unique name for the blob
            string blobName = $"{Guid.NewGuid()}.json";
            
            // Serialize the cover letter data to JSON
            string jsonData = JsonSerializer.Serialize(coverLetterData);
            
            // Get a reference to the blob
            BlobClient blobClient = _containerClient.GetBlobClient(blobName);
            
            // Upload the JSON data to the blob
            using (MemoryStream stream = new MemoryStream(Encoding.UTF8.GetBytes(jsonData)))
            {
                await blobClient.UploadAsync(stream, true);
            }
            
            _logger.LogInformation("Cover letter uploaded to blob: {BlobName}", blobName);
            return blobName;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error uploading cover letter to blob storage");
            throw;
        }
    }

    public async Task<IEnumerable<BlobItem>> ListBlobsAsync()
    {
        try
        {
            var blobs = new List<BlobItem>();
            
            await foreach (var blobItem in _containerClient.GetBlobsAsync())
            {
                blobs.Add(blobItem);
            }
            
            _logger.LogInformation("Listed {Count} blobs from container", blobs.Count);
            return blobs;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error listing blobs from container");
            throw;
        }
    }

    public async Task<string> DownloadBlobContentAsync(string blobName)
    {
        try
        {
            BlobClient blobClient = _containerClient.GetBlobClient(blobName);
            
            if (!await blobClient.ExistsAsync())
            {
                _logger.LogWarning("Blob {BlobName} does not exist", blobName);
                return string.Empty;
            }
            
            var response = await blobClient.DownloadContentAsync();
            string content = response.Value.Content.ToString();
            
            _logger.LogInformation("Downloaded blob content: {BlobName}, size: {Size} characters", blobName, content.Length);
            return content;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error downloading blob content: {BlobName}", blobName);
            throw;
        }
    }

    public async Task DeleteBlobAsync(string blobName)
    {
        try
        {
            BlobClient blobClient = _containerClient.GetBlobClient(blobName);
            
            var response = await blobClient.DeleteIfExistsAsync();
            
            if (response.Value)
            {
                _logger.LogInformation("Successfully deleted blob: {BlobName}", blobName);
            }
            else
            {
                _logger.LogWarning("Blob {BlobName} was not found for deletion", blobName);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting blob: {BlobName}", blobName);
            throw;
        }
    }

    public async Task<bool> BlobExistsAsync(string blobName)
    {
        try
        {
            BlobClient blobClient = _containerClient.GetBlobClient(blobName);
            var response = await blobClient.ExistsAsync();
            
            _logger.LogDebug("Blob {BlobName} exists: {Exists}", blobName, response.Value);
            return response.Value;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking if blob exists: {BlobName}", blobName);
            throw;
        }
    }
}

public class CoverLetterData
{
    public string Email { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string CoverLetter { get; set; } = string.Empty;
    public string JobTitle { get; set; } = string.Empty;
    public string CompanyName { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
