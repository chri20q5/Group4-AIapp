# Cover Letter Generator - Azure Function

A secure, production-ready Azure Function that generates personalized cover letters using AI and integrates with Azure SQL Database and Blob Storage.

## 🚀 Features

- **AI-Powered Cover Letter Generation**: Uses Ollama API to generate personalized cover letters
- **Database Integration**: Connects to Azure SQL Database for job and applicant data
- **Secure Storage**: Saves cover letters to Azure Blob Storage with managed identity
- **Enterprise Security**: Function-level authentication, input validation, and Key Vault integration
- **Monitoring**: Comprehensive logging with Application Insights

## 🏗️ Architecture

```
┌─────────────────┐    ┌──────────────────┐    ┌─────────────────┐
│   Frontend      │────│  Azure Function  │────│   Ollama API    │
│   Application   │    │     (Secure)     │    │  (AI Service)   │
└─────────────────┘    └──────────────────┘    └─────────────────┘
                              │
                    ┌─────────┼─────────┐
                    │         │         │
            ┌───────▼───┐ ┌───▼────┐ ┌──▼──────┐
            │Azure SQL │ │ Key     │ │ Blob    │
            │Database  │ │ Vault   │ │ Storage │
            └──────────┘ └─────────┘ └─────────┘
```

## 📡 API Endpoints

All endpoints require Function-level authentication (`?code=FUNCTION_KEY`):

### Job Management
- `GET /api/GetJobs` - Retrieve all job listings
- `GET /api/jobs/{jobId}` - Get specific job details

### Applicant Management  
- `GET /api/GetApplicants` - Retrieve all applicants

### Cover Letter Generation
- `POST /api/GenerateCoverLetter` - Generate cover letter from job description and user profile
- `POST /api/GenerateCoverLetterFromJob` - Generate cover letter using database job and applicant IDs
- `POST /api/SaveCoverLetter` - Save generated cover letter to blob storage

## 🔒 Security Features

- **Authentication**: Function-level keys required for all endpoints
- **Input Validation**: XSS/injection protection with length limits
- **Secret Management**: All credentials stored in Azure Key Vault
- **Managed Identity**: Secure access to Azure resources without connection strings
- **Rate Limiting**: Protection against DoS attacks
- **Monitoring**: Secure logging with sensitive data sanitization

## 🛠️ Deployment

### Prerequisites
- Azure CLI
- Azure Functions Core Tools
- .NET 8 SDK

### Quick Deploy
```bash
# Build the function
dotnet publish --configuration Release

# Deploy to Azure
az functionapp deployment source config-zip \
  --resource-group YOUR_RESOURCE_GROUP \
  --name YOUR_FUNCTION_APP \
  --src bin/Release/net8.0/publish.zip
```

### Infrastructure as Code
Deploy complete infrastructure using Bicep templates in `/infra/` folder:

```bash
az deployment group create \
  --resource-group YOUR_RESOURCE_GROUP \
  --template-file infra/main.bicep \
  --parameters infra/main.parameters.json
```

## ⚙️ Configuration

### Azure Resources Required
- **Function App**: Hosts the serverless functions
- **Storage Account**: For function app storage and cover letter blob storage
- **Key Vault**: Secure storage for API keys and connection strings
- **SQL Database**: Job and applicant data storage
- **Application Insights**: Monitoring and logging
- **Managed Identity**: Secure authentication to Azure services

### Environment Variables
Set these in your Function App configuration:
```
KeyVaultUrl=https://your-keyvault.vault.azure.net/
BlobContainerName=coverletters
ApiUrl=https://your-ollama-instance.com/api/chat/completions
ModelName=gemma3:1b
APPLICATIONINSIGHTS_CONNECTION_STRING=your-app-insights-connection
```

### Key Vault Secrets
Store these secrets in Azure Key Vault:
- `ApiKey`: Ollama API authentication key
- `DatabaseConnectionString`: Azure SQL Database connection string

## 🧪 Testing

### Get Function Keys
```bash
# Get host-level function key (works for all functions)
az functionapp keys list --name YOUR_FUNCTION_APP --resource-group YOUR_RESOURCE_GROUP
```

### Example API Calls
```bash
# Set your function key
FUNCTION_KEY="your-function-key-here"

# Get all jobs
curl "https://your-function-app.azurewebsites.net/api/GetJobs?code=$FUNCTION_KEY"

# Generate cover letter
curl -X POST "https://your-function-app.azurewebsites.net/api/GenerateCoverLetter?code=$FUNCTION_KEY" \
  -H "Content-Type: application/json" \
  -d '{
    "jobDescription": "We are looking for a software engineer...",
    "userProfile": "I am an experienced developer..."
  }'
```

## 📊 Monitoring

- **Application Insights**: Performance and error monitoring
- **Health Checks**: Automatic failure detection
- **Rate Limiting**: Request throttling and concurrency control
- **Security Logging**: Audit trails without sensitive data exposure

## 🔧 Development

### Local Development
1. Clone the repository
2. Set up User Secrets for local development:
   ```bash
   dotnet user-secrets set "ApiKey" "your-local-api-key"
   dotnet user-secrets set "DatabaseConnectionString" "your-local-db-connection"
   ```
3. Run locally:
   ```bash
   func start
   ```

### Project Structure
```
├── infra/                          # Infrastructure as Code (Bicep)
├── GenerateCoverLetterFunction.cs   # Main HTTP trigger functions
├── CoverLetterService.cs           # Cover letter generation logic
├── BlobStorageService.cs           # Azure Blob Storage integration
├── DatabaseService.cs              # Azure SQL Database integration
├── SecurityHelper.cs               # Input validation and security utilities
├── Models.cs                       # Data models
├── host.json                       # Function host configuration
├── SECURITY.md                     # Security documentation
└── README.md                       # This file
```

## 📚 Documentation

- [SECURITY.md](SECURITY.md) - Comprehensive security guide and procedures
- [infra/](infra/) - Infrastructure deployment templates and parameters

## 🤝 Contributing

1. Ensure all security guidelines in SECURITY.md are followed
2. Run security validation before deployment
3. Update documentation for any new features
4. Test both authentication and input validation

## 📄 License

This project is licensed under the MIT License.

## 📞 Support

For security issues, see [SECURITY.md](SECURITY.md) for emergency procedures and contact information.
