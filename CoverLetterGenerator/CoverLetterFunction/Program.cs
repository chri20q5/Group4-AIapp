using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using CoverLetterFunction.Services;

var host = new HostBuilder()
    .ConfigureFunctionsWorkerDefaults()
    .ConfigureServices(services =>
    {
        services.AddHttpClient();
        
        // Use real Azure Blob Storage for production deployment
        // Comment out the line below and uncomment the next line to use file-based mock for local dev
        // services.AddSingleton<IBlobStorageService, FileSavingMockBlobStorageService>();
        services.AddSingleton<IBlobStorageService, BlobStorageService>();
        
        services.AddSingleton<ICoverLetterService, CoverLetterService>();
        services.AddSingleton<IDatabaseService, DatabaseService>();
        
        // Register email service for automated email sending
        services.AddSingleton<IEmailService, EmailService>();
        
        // Register authentication service for secure login/registration
        services.AddSingleton<IAuthenticationService, AuthenticationService>();
    })
    .Build();

host.Run();
