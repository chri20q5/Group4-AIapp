# AutoJob - AI-Powered Job Application Portal

A complete job portal solution with AI-generated cover letters, built on Azure cloud infrastructure.

## 🚀 Project Overview

AutoJob is a modern web application that helps job seekers find opportunities and automatically generates personalized cover letters using AI. The application consists of a responsive frontend and a secure Azure Functions backend.

### 🌟 Key Features

- **User Authentication**: Secure registration and login with JWT tokens
- **Job Listings**: Browse and search through job opportunities
- **AI Cover Letter Generation**: Automatically create personalized cover letters using AI
- **Email Integration**: Send applications directly to hiring managers
- **Profile Management**: Manage user profiles and job preferences
- **Responsive Design**: Modern, mobile-friendly user interface

## 🏗️ Architecture

### Backend (Azure Functions)
- **Language**: C# (.NET 8)
- **Authentication**: JWT with BCrypt password hashing
- **Database**: Azure SQL Database
- **Storage**: Azure Blob Storage
- **Email**: Mailgun integration
- **Security**: Azure Key Vault for secrets management

### Frontend (Azure App Service)
- **Language**: HTML, CSS, JavaScript (Vanilla)
- **Authentication**: JWT token management
- **Design**: Modern purple-themed responsive UI
- **API Integration**: RESTful communication with backend

### Infrastructure (Azure)
- **Compute**: Azure Functions (Consumption Plan)
- **Database**: Azure SQL Database
- **Storage**: Azure Storage Account
- **Security**: Azure Key Vault, Managed Identity
- **Monitoring**: Application Insights
- **Deployment**: Bicep Infrastructure as Code

## 🔒 Security Features

- **Authentication**: JWT tokens with 7-day expiration
- **Password Security**: BCrypt hashing with salt rounds of 12
- **Input Validation**: Comprehensive sanitization and validation
- **Secret Management**: Azure Key Vault integration
- **Database Security**: Parameterized queries preventing SQL injection
- **CORS**: Configured for secure cross-origin requests
- **Monitoring**: Application Insights for security monitoring

## 📦 Project Structure

```
AIAgentTest/
├── CoverLetterGenerator/
│   └── CoverLetterFunction/           # Azure Functions backend
│       ├── GenerateCoverLetterFunction.cs
│       ├── AuthenticationService.cs
│       ├── DatabaseService.cs
│       ├── SecurityHelper.cs
│       └── Models.cs
├── job-app-frontend/                  # Frontend application
│   ├── dashboard.html
│   ├── login.html
│   ├── register.html
│   ├── profile.html
│   ├── app.js
│   ├── auth.js
│   └── config.js
├── infra/                             # Infrastructure as Code
│   ├── main.bicep
│   └── main.parameters.json
└── azure.yaml                        # Azure Developer CLI configuration
```

## 🚀 Deployment

### Prerequisites
- Azure account with active subscription
- Azure CLI installed
- Azure Developer CLI (azd) installed
- .NET 8 SDK

### Quick Deployment

1. **Clone the repository**
   ```bash
   git clone https://github.com/chri20q5/Group4-AIapp.git
   cd Group4-AIapp
   git checkout finished
   ```

2. **Deploy with Azure Developer CLI**
   ```bash
   azd up
   ```

3. **Configure secrets in Azure Key Vault**
   - Database connection string
   - JWT secret key
   - Mailgun API credentials
   - AI service API key

### Manual Deployment

#### Backend (Azure Functions)
```bash
cd CoverLetterGenerator/CoverLetterFunction
func azure functionapp publish <YOUR-FUNCTION-APP-NAME>
```

#### Frontend (Azure App Service)
```bash
cd job-app-frontend
zip -r frontend.zip . -x "*.zip" "*.sln" 
az webapp deploy --resource-group <RESOURCE-GROUP> --name <APP-NAME> --src-path frontend.zip --type zip
```

## 🔧 Configuration

### Environment Variables
- `DatabaseConnectionString`: Azure SQL Database connection
- `JwtSecret`: Secret key for JWT token signing
- `JwtIssuer`: JWT token issuer
- `ApiKey`: AI service API key
- `ApiUrl`: AI service endpoint URL
- `MailgunApiKey`: Mailgun API key
- `MailgunDomain`: Mailgun domain

### Frontend Configuration
Update `config.js` with your Azure Functions URL:
```javascript
const config = {
    apiBaseUrl: 'https://your-function-app.azurewebsites.net/api',
    environment: 'production'
};
```

## 🧪 Testing

### Backend Testing
```bash
cd CoverLetterGenerator/CoverLetterFunction.Tests
dotnet test
```

### API Endpoints
- `POST /api/Register` - User registration
- `POST /api/Login` - User authentication
- `GET /api/GetUserProfile` - Get user profile
- `PUT /api/UpdateUserProfile` - Update user profile
- `GET /api/GetJobs` - Get job listings
- `POST /api/GenerateCoverLetter` - Generate cover letter
- `POST /api/SendEmail` - Send application email

## 👥 Team

**Group 4 - Cloud IT**
- Development and deployment by the Cloud IT team
- AI integration and security implementation
- Azure infrastructure design and implementation

## 📄 License

This project is developed for educational purposes as part of the Cloud IT program.

## 🤝 Contributing

1. Fork the repository
2. Create a feature branch (`git checkout -b feature/AmazingFeature`)
3. Commit your changes (`git commit -m 'Add some AmazingFeature'`)
4. Push to the branch (`git push origin feature/AmazingFeature`)
5. Open a Pull Request

## 📞 Support

For support and questions, please contact the development team or create an issue in the GitHub repository.

---

Built with ❤️ by Group 4 - Cloud IT Team

## 🔄 System Workflow

1. **Job Data Collection**: `JobDataIngestion` fetches job listings from Jooble API
2. **Database Population**: `JobDatabasePopulator` stores job data in Azure SQL Database
3. **Cover Letter Generation**: `CoverLetterFunction` generates personalized cover letters
4. **Email Delivery**: `CoverLetterEmailSender` sends cover letters via Mailgun
5. **Database Management**: `DatabaseInspector` helps manage and inspect the database

## 🚀 Components

### 📧 CoverLetterEmailSender
- **Purpose**: Processes cover letters from blob storage and sends them via email
- **Features**: 
  - Mailgun integration (EU region)
  - Blob storage processing
  - Email template customization
  - Automatic cleanup after sending

### 📝 CoverLetterGenerator
- **CoverLetterApp**: Standalone console application for generating cover letters
- **CoverLetterFunction**: Azure Function that generates cover letters and saves to blob storage
- **CoverLetterFunction.Tests**: Unit tests for the Azure Function

### 🔍 JobDataIngestion
- **Purpose**: Azure Function that acts as a proxy to Jooble API
- **Features**: 
  - HTTP trigger endpoint
  - Job search by keywords and location
  - Returns job listings in JSON format

### 💾 JobDatabasePopulator
- **Purpose**: Console application that populates Azure SQL Database with job data
- **Features**: 
  - Azure Key Vault integration for secure database credentials
  - Jooble API integration
  - Duplicate prevention
  - Batch job insertion

### 🛠️ DatabaseInspector
- **Purpose**: Utility tool for database management and inspection
- **Features**: 
  - Database connection testing
  - Data inspection and querying
  - Database schema validation

### 🧪 Testing
- **TestClient.cs**: HTTP client for testing Azure Functions
- **create-test-blob.sh**: Script for creating test blob data
- **setup-local.sh**: Local development setup script

### 📚 Documentation
- **DATABASE_INTEGRATION.md**: Database integration guide
- **LOCAL_TESTING_GUIDE.md**: Local development and testing guide
- **README.md**: Main project documentation

## 🔧 Configuration

Each component has its own configuration:
- **appsettings.json**: Main configuration
- **appsettings.Development.json**: Development-specific settings
- **local.settings.json**: Azure Functions local settings
- **azure.yaml**: Azure Developer CLI configuration

## 🌐 Azure Resources

- **Azure SQL Database**: Job listings and applicant data
- **Azure Blob Storage**: Cover letter temporary storage
- **Azure Functions**: Serverless compute for cover letter generation
- **Azure Key Vault**: Secure credential storage
- **Mailgun**: Email delivery service

## 🚀 Getting Started

1. **Set up Azure resources** using the infrastructure templates in `infra/`
2. **Configure settings** in each component's appsettings files
3. **Run JobDatabasePopulator** to populate the database with job data
4. **Deploy CoverLetterFunction** to Azure or run locally
5. **Run CoverLetterEmailSender** to process and send cover letters

## 📝 Notes

- The system uses Mailgun for email delivery with EU region support
- Database credentials are securely stored in Azure Key Vault
- The system prevents duplicate job entries and email sending
- All components include comprehensive logging and error handling
