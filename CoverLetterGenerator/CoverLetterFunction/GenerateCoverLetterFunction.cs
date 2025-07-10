using System;
using System.Net;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;
using CoverLetterFunction.Services;
using CoverLetterFunction.Models;

public class GenerateCoverLetterFunction
{
    private readonly ILogger _logger;
    private readonly ICoverLetterService _coverLetterService;
    private readonly IDatabaseService _databaseService;
    private readonly IAuthenticationService _authenticationService;

    public GenerateCoverLetterFunction(
        ILoggerFactory loggerFactory, 
        ICoverLetterService coverLetterService, 
        IDatabaseService databaseService,
        IAuthenticationService authenticationService)
    {
        _logger = loggerFactory.CreateLogger<GenerateCoverLetterFunction>();
        _coverLetterService = coverLetterService;
        _databaseService = databaseService;
        _authenticationService = authenticationService;
    }

    [Function("GenerateCoverLetter")]
    public async Task<HttpResponseData> Run(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post")] HttpRequestData req)
    {
        _logger.LogInformation("C# HTTP trigger function processed a request to generate a cover letter.");

        try
        {
            string? requestBody = await req.ReadAsStringAsync();
            
            if (string.IsNullOrEmpty(requestBody))
            {
                var badRequestResponse = req.CreateResponse(HttpStatusCode.BadRequest);
                await badRequestResponse.WriteStringAsync("Request body is required and should include jobDescription and userProfile.");
                return badRequestResponse;
            }

            // Security: Validate input length and content
            if (!SecurityHelper.IsValidInput(requestBody, 50000)) // 50KB max for cover letter requests
            {
                var badRequestResponse = req.CreateResponse(HttpStatusCode.BadRequest);
                await badRequestResponse.WriteStringAsync("Invalid input: Request too large or contains suspicious content.");
                return badRequestResponse;
            }

            var data = JsonSerializer.Deserialize<CoverLetterRequest>(requestBody, new JsonSerializerOptions 
            { 
                PropertyNameCaseInsensitive = true,
                MaxDepth = 10 // Prevent JSON bomb attacks
            });

            if (data == null || string.IsNullOrEmpty(data.JobDescription) || string.IsNullOrEmpty(data.UserProfile))
            {
                var badRequestResponse = req.CreateResponse(HttpStatusCode.BadRequest);
                await badRequestResponse.WriteStringAsync("Both jobDescription and userProfile are required in the request body.");
                return badRequestResponse;
            }

            // Additional validation for individual fields
            if (!SecurityHelper.IsValidInput(data.JobDescription, 5000) || !SecurityHelper.IsValidInput(data.UserProfile, 5000))
            {
                var badRequestResponse = req.CreateResponse(HttpStatusCode.BadRequest);
                await badRequestResponse.WriteStringAsync("Job description or user profile contains invalid content or is too long.");
                return badRequestResponse;
            }

            string coverLetter = await _coverLetterService.GenerateCoverLetterAsync(data.JobDescription, data.UserProfile);

            var response = req.CreateResponse(HttpStatusCode.OK);
            await response.WriteAsJsonAsync(new { coverLetter });
            return response;
        }
        catch (JsonException jsonEx)
        {
            _logger.LogWarning("Invalid JSON in request: {Error}", SecurityHelper.SanitizeLogMessage(jsonEx.Message, _logger));
            var errorResponse = req.CreateResponse(HttpStatusCode.BadRequest);
            await errorResponse.WriteStringAsync("Invalid JSON format in request body.");
            return errorResponse;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating cover letter");
            var errorResponse = req.CreateResponse(HttpStatusCode.InternalServerError);
            await errorResponse.WriteStringAsync(SecurityHelper.GetGenericErrorMessage("generating cover letter"));
            return errorResponse;
        }
    }

    [Function("SaveCoverLetter")]
    public async Task<HttpResponseData> SaveCoverLetter(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post")] HttpRequestData req)
    {
        _logger.LogInformation("C# HTTP trigger function processed a request to save a cover letter.");

        try
        {
            string? requestBody = await req.ReadAsStringAsync();
            
            if (string.IsNullOrEmpty(requestBody))
            {
                var badRequestResponse = req.CreateResponse(HttpStatusCode.BadRequest);
                await badRequestResponse.WriteStringAsync("Request body is required.");
                return badRequestResponse;
            }

            var data = JsonSerializer.Deserialize<SaveCoverLetterRequest>(requestBody, new JsonSerializerOptions 
            { 
                PropertyNameCaseInsensitive = true 
            });

            if (data == null || 
                string.IsNullOrEmpty(data.Email) || 
                string.IsNullOrEmpty(data.CoverLetter))
            {
                var badRequestResponse = req.CreateResponse(HttpStatusCode.BadRequest);
                await badRequestResponse.WriteStringAsync("Email and coverLetter are required in the request body.");
                return badRequestResponse;
            }

            string blobName = await _coverLetterService.SaveCoverLetterToBlobAsync(
                data.CoverLetter,
                data.Email,
                data.Name ?? "User",
                data.JobTitle ?? "Not specified",
                data.CompanyName ?? "Not specified"
            );

            var response = req.CreateResponse(HttpStatusCode.OK);
            await response.WriteAsJsonAsync(new { blobName, message = "Cover letter saved successfully" });
            return response;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error saving cover letter");
            var errorResponse = req.CreateResponse(HttpStatusCode.InternalServerError);
            await errorResponse.WriteStringAsync($"Error saving cover letter: {ex.Message}");
            return errorResponse;
        }
    }

    [Function("GetJobs")]
    public async Task<HttpResponseData> GetJobs(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get")] HttpRequestData req)
    {
        _logger.LogInformation("C# HTTP trigger function processed a request to get jobs.");

        try
        {
            var jobs = await _databaseService.GetJobsAsync();
            
            var response = req.CreateResponse(HttpStatusCode.OK);
            await response.WriteAsJsonAsync(jobs);
            return response;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving jobs");
            var errorResponse = req.CreateResponse(HttpStatusCode.InternalServerError);
            await errorResponse.WriteStringAsync($"Error retrieving jobs: {ex.Message}");
            return errorResponse;
        }
    }

    [Function("GetJobById")]
    public async Task<HttpResponseData> GetJobById(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "jobs/{jobId:int}")] HttpRequestData req,
        int jobId)
    {
        _logger.LogInformation("C# HTTP trigger function processed a request to get job {JobId}.", jobId);

        try
        {
            var job = await _databaseService.GetJobByIdAsync(jobId);
            
            if (job == null)
            {
                var notFoundResponse = req.CreateResponse(HttpStatusCode.NotFound);
                await notFoundResponse.WriteStringAsync($"Job with ID {jobId} not found.");
                return notFoundResponse;
            }
            
            var response = req.CreateResponse(HttpStatusCode.OK);
            await response.WriteAsJsonAsync(job);
            return response;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving job {JobId}", jobId);
            var errorResponse = req.CreateResponse(HttpStatusCode.InternalServerError);
            await errorResponse.WriteStringAsync($"Error retrieving job: {ex.Message}");
            return errorResponse;
        }
    }

    [Function("GenerateCoverLetterFromJob")]
    public async Task<HttpResponseData> GenerateCoverLetterFromJob(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post")] HttpRequestData req)
    {
        _logger.LogInformation("C# HTTP trigger function processed a request to generate a cover letter from job.");

        try
        {
            string? requestBody = await req.ReadAsStringAsync();
            
            if (string.IsNullOrEmpty(requestBody))
            {
                var badRequestResponse = req.CreateResponse(HttpStatusCode.BadRequest);
                await badRequestResponse.WriteStringAsync("Request body is required and should include jobId and userEmail or applicantId.");
                return badRequestResponse;
            }

            var data = JsonSerializer.Deserialize<CoverLetterFromJobRequest>(requestBody, new JsonSerializerOptions 
            { 
                PropertyNameCaseInsensitive = true 
            });

            if (data == null || data.JobId <= 0)
            {
                var badRequestResponse = req.CreateResponse(HttpStatusCode.BadRequest);
                await badRequestResponse.WriteStringAsync("JobId is required and must be greater than 0.");
                return badRequestResponse;
            }

            // Get job details
            var job = await _databaseService.GetJobByIdAsync(data.JobId);
            if (job == null)
            {
                var notFoundResponse = req.CreateResponse(HttpStatusCode.NotFound);
                await notFoundResponse.WriteStringAsync($"Job with ID {data.JobId} not found.");
                return notFoundResponse;
            }

            // Get user profile
            string userProfile = "";
            if (!string.IsNullOrEmpty(data.UserEmail))
            {
                var applicant = await _databaseService.GetApplicantByEmailAsync(data.UserEmail);
                if (applicant != null)
                {
                    userProfile = $"Name: {applicant.FirstName} {applicant.LastName}, Email: {applicant.Email}";
                    if (!string.IsNullOrEmpty(applicant.Location))
                    {
                        userProfile += $", Location: {applicant.Location}";
                    }
                }
            }
            else if (data.ApplicantId.HasValue)
            {
                var applicant = await _databaseService.GetApplicantByIdAsync(data.ApplicantId.Value);
                if (applicant != null)
                {
                    userProfile = $"Name: {applicant.FirstName} {applicant.LastName}, Email: {applicant.Email}";
                    if (!string.IsNullOrEmpty(applicant.Location))
                    {
                        userProfile += $", Location: {applicant.Location}";
                    }
                }
            }

            if (string.IsNullOrEmpty(userProfile))
            {
                userProfile = data.CustomUserProfile ?? "Professional seeking opportunities";
            }

            // Create job description from job data
            string jobDescription = $"Job Title: {job.Title}";
            if (!string.IsNullOrEmpty(job.Location))
            {
                jobDescription += $", Location: {job.Location}";
            }
            if (!string.IsNullOrEmpty(job.Salary))
            {
                jobDescription += $", Salary: {job.Salary}";
            }
            if (!string.IsNullOrEmpty(job.Snippet))
            {
                jobDescription += $"\n\nJob Description: {job.Snippet}";
            }

            string coverLetter = await _coverLetterService.GenerateCoverLetterAsync(jobDescription, userProfile);

            var response = req.CreateResponse(HttpStatusCode.OK);
            await response.WriteAsJsonAsync(new { 
                coverLetter, 
                jobTitle = job.Title,
                jobLocation = job.Location,
                userProfile 
            });
            return response;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating cover letter from job");
            var errorResponse = req.CreateResponse(HttpStatusCode.InternalServerError);
            await errorResponse.WriteStringAsync($"Error generating cover letter: {ex.Message}");
            return errorResponse;
        }
    }

    [Function("GetUserProfile")]
    public async Task<HttpResponseData> GetUserProfile(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get")] HttpRequestData req)
    {
        _logger.LogInformation("Getting user profile");

        try
        {
            // Validate JWT token and extract user ID
            var userId = ExtractUserIdFromRequest(req);
            
            if (userId == null)
            {
                return await CreateUnauthorizedResponseAsync(req, "Valid authorization token required");
            }

            var applicant = await _databaseService.GetApplicantByIdAsync(userId.Value);
            
            if (applicant == null)
            {
                var notFoundResponse = req.CreateResponse(HttpStatusCode.NotFound);
                await notFoundResponse.WriteStringAsync("User not found");
                return notFoundResponse;
            }

            // Create profile response without password
            var userProfile = new
            {
                applicant.ApplicantId,
                applicant.FirstName,
                applicant.LastName,
                FullName = $"{applicant.FirstName} {applicant.LastName}",
                applicant.Email,
                applicant.Location,
                applicant.JobTitle,
                applicant.AboutMe,
                applicant.ResumeFileUrl,
                applicant.JobPreferences,
                applicant.CreatedAt,
                applicant.UpdatedAt
            };

            var response = req.CreateResponse(HttpStatusCode.OK);
            await response.WriteStringAsync(JsonSerializer.Serialize(userProfile));
            response.Headers.Add("Content-Type", "application/json");
            return response;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting user profile");
            var errorResponse = req.CreateResponse(HttpStatusCode.InternalServerError);
            await errorResponse.WriteStringAsync($"Internal server error: {ex.Message}");
            return errorResponse;
        }
    }

    [Function("UpdateUserProfile")]
    public async Task<HttpResponseData> UpdateUserProfile(
        [HttpTrigger(AuthorizationLevel.Anonymous, "put")] HttpRequestData req)
    {
        _logger.LogInformation("Updating user profile");

        try
        {
            // Validate JWT token and extract user ID
            var userId = ExtractUserIdFromRequest(req);
            
            if (userId == null)
            {
                return await CreateUnauthorizedResponseAsync(req, "Valid authorization token required");
            }

            string? requestBody = await req.ReadAsStringAsync();
            
            if (string.IsNullOrEmpty(requestBody))
            {
                var badRequestResponse = req.CreateResponse(HttpStatusCode.BadRequest);
                await badRequestResponse.WriteStringAsync("Request body is required");
                return badRequestResponse;
            }

            var profileData = JsonSerializer.Deserialize<UpdateProfileRequest>(requestBody, new JsonSerializerOptions 
            { 
                PropertyNameCaseInsensitive = true,
                MaxDepth = 10 // Prevent JSON bomb attacks
            });
            
            if (profileData == null)
            {
                var badRequestResponse = req.CreateResponse(HttpStatusCode.BadRequest);
                await badRequestResponse.WriteStringAsync("Invalid profile data");
                return badRequestResponse;
            }

            // Get existing user using JWT user ID
            var existingUser = await _databaseService.GetApplicantByIdAsync(userId.Value);
            if (existingUser == null)
            {
                var notFoundResponse = req.CreateResponse(HttpStatusCode.NotFound);
                await notFoundResponse.WriteStringAsync("User not found");
                return notFoundResponse;
            }

            // Create updated applicant object
            var updatedApplicant = new Applicant
            {
                ApplicantId = existingUser.ApplicantId,
                FirstName = profileData.FirstName ?? existingUser.FirstName,
                LastName = profileData.LastName ?? existingUser.LastName,
                Email = existingUser.Email, // Email shouldn't be updated via profile
                Password = existingUser.Password, // Password shouldn't be updated via profile
                Location = profileData.Location,
                JobTitle = profileData.JobTitle,
                AboutMe = profileData.AboutMe,
                ResumeFileUrl = profileData.ResumeFileUrl,
                JobPreferences = profileData.JobPreferences
            };

            // Update user profile
            bool success = await _databaseService.UpdateApplicantProfileAsync(userId.Value, updatedApplicant);
            
            if (success)
            {
                var response = req.CreateResponse(HttpStatusCode.OK);
                await response.WriteStringAsync(JsonSerializer.Serialize(new { success = true, message = "Profile updated successfully" }));
                response.Headers.Add("Content-Type", "application/json");
                return response;
            }
            else
            {
                var errorResponse = req.CreateResponse(HttpStatusCode.InternalServerError);
                await errorResponse.WriteStringAsync("Failed to update profile");
                return errorResponse;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating user profile");
            var errorResponse = req.CreateResponse(HttpStatusCode.InternalServerError);
            await errorResponse.WriteStringAsync($"Internal server error: {ex.Message}");
            return errorResponse;
        }
    }

    [Function("CreateApplicant")]
    public async Task<HttpResponseData> CreateApplicant(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post")] HttpRequestData req)
    {
        _logger.LogInformation("Creating new applicant");

        try
        {
            string? requestBody = await req.ReadAsStringAsync();
            
            if (string.IsNullOrEmpty(requestBody))
            {
                var badRequestResponse = req.CreateResponse(HttpStatusCode.BadRequest);
                await badRequestResponse.WriteStringAsync("Request body is required");
                return badRequestResponse;
            }

            var registrationData = JsonSerializer.Deserialize<CreateApplicantRequest>(requestBody);
            
            if (registrationData == null || 
                string.IsNullOrEmpty(registrationData.FirstName) ||
                string.IsNullOrEmpty(registrationData.LastName) ||
                string.IsNullOrEmpty(registrationData.Email) ||
                string.IsNullOrEmpty(registrationData.Password))
            {
                var badRequestResponse = req.CreateResponse(HttpStatusCode.BadRequest);
                await badRequestResponse.WriteStringAsync("FirstName, LastName, Email, and Password are required");
                return badRequestResponse;
            }

            // Check if email already exists
            var existingUser = await _databaseService.GetApplicantByEmailAsync(registrationData.Email);
            if (existingUser != null)
            {
                var conflictResponse = req.CreateResponse(HttpStatusCode.Conflict);
                await conflictResponse.WriteStringAsync("Email address already exists");
                return conflictResponse;
            }

            // Create new applicant model
            var newApplicant = new Applicant
            {
                FirstName = registrationData.FirstName,
                LastName = registrationData.LastName,
                Email = registrationData.Email,
                Password = registrationData.Password, // Note: Should be hashed by authentication service
                Location = registrationData.Location,
                JobTitle = registrationData.JobTitle,
                AboutMe = registrationData.AboutMe,
                ResumeFileUrl = registrationData.ResumeFileUrl,
                JobPreferences = registrationData.JobPreferences
            };

            // Create new applicant
            int newApplicantId = await _databaseService.CreateApplicantAsync(newApplicant);
            
            if (newApplicantId > 0)
            {
                var response = req.CreateResponse(HttpStatusCode.Created);
                await response.WriteStringAsync(JsonSerializer.Serialize(new { 
                    success = true, 
                    message = "Account created successfully",
                    applicantId = newApplicantId
                }));
                response.Headers.Add("Content-Type", "application/json");
                return response;
            }
            else
            {
                var errorResponse = req.CreateResponse(HttpStatusCode.InternalServerError);
                await errorResponse.WriteStringAsync("Failed to create account");
                return errorResponse;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating applicant");
            var errorResponse = req.CreateResponse(HttpStatusCode.InternalServerError);
            await errorResponse.WriteStringAsync($"Internal server error: {ex.Message}");
            return errorResponse;
        }
    }

    [Function("GetApplicants")]
    public async Task<HttpResponseData> GetApplicants(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get")] HttpRequestData req)
    {
        _logger.LogInformation("C# HTTP trigger function processed a request to get applicants.");

        try
        {
            var applicants = await _databaseService.GetApplicantsAsync();
            
            // Remove password field from response for security
            var safeApplicants = applicants.Select(a => new 
            {
                a.ApplicantId,
                a.FirstName,
                a.LastName,
                a.Location,
                a.Email
                // Password field excluded
            });
            
            var response = req.CreateResponse(HttpStatusCode.OK);
            await response.WriteAsJsonAsync(safeApplicants);
            return response;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving applicants");
            var errorResponse = req.CreateResponse(HttpStatusCode.InternalServerError);
            await errorResponse.WriteStringAsync($"Error retrieving applicants: {ex.Message}");
            return errorResponse;
        }
    }

    // === SECURE AUTHENTICATION ENDPOINTS ===

    [Function("Register")]
    public async Task<HttpResponseData> Register(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post")] HttpRequestData req)
    {
        _logger.LogInformation("User registration attempt");

        try
        {
            string? requestBody = await req.ReadAsStringAsync();
            
            if (string.IsNullOrEmpty(requestBody))
            {
                var badRequestResponse = req.CreateResponse(HttpStatusCode.BadRequest);
                await badRequestResponse.WriteStringAsync("Request body is required");
                return badRequestResponse;
            }

            var registerRequest = JsonSerializer.Deserialize<RegisterRequest>(requestBody, new JsonSerializerOptions 
            { 
                PropertyNameCaseInsensitive = true,
                MaxDepth = 10 // Prevent JSON bomb attacks
            });
            
            if (registerRequest == null)
            {
                var badRequestResponse = req.CreateResponse(HttpStatusCode.BadRequest);
                await badRequestResponse.WriteStringAsync("Invalid registration data");
                return badRequestResponse;
            }

            // Validate required fields
            if (string.IsNullOrWhiteSpace(registerRequest.FirstName) ||
                string.IsNullOrWhiteSpace(registerRequest.LastName) ||
                string.IsNullOrWhiteSpace(registerRequest.Email) ||
                string.IsNullOrWhiteSpace(registerRequest.Password))
            {
                var badRequestResponse = req.CreateResponse(HttpStatusCode.BadRequest);
                await badRequestResponse.WriteStringAsync("All fields are required");
                return badRequestResponse;
            }

            // Convert to AuthenticationService model
            var authRegisterRequest = new CoverLetterFunction.Services.RegisterRequest
            {
                FirstName = registerRequest.FirstName,
                LastName = registerRequest.LastName,
                Email = registerRequest.Email,
                Password = registerRequest.Password
            };

            var result = await _authenticationService.RegisterAsync(authRegisterRequest);
            
            if (result.Success)
            {
                var response = req.CreateResponse(HttpStatusCode.Created);
                await response.WriteStringAsync(JsonSerializer.Serialize(new
                {
                    success = true,
                    message = "Registration successful",
                    token = result.Token,
                    user = result.User
                }));
                response.Headers.Add("Content-Type", "application/json");
                return response;
            }
            else
            {
                var conflictResponse = req.CreateResponse(HttpStatusCode.Conflict);
                await conflictResponse.WriteStringAsync(JsonSerializer.Serialize(new
                {
                    success = false,
                    message = result.Message
                }));
                conflictResponse.Headers.Add("Content-Type", "application/json");
                return conflictResponse;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during registration");
            var errorResponse = req.CreateResponse(HttpStatusCode.InternalServerError);
            await errorResponse.WriteStringAsync("Registration failed");
            return errorResponse;
        }
    }

    [Function("Login")]
    public async Task<HttpResponseData> Login(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post")] HttpRequestData req)
    {
        _logger.LogInformation("User login attempt");

        try
        {
            string? requestBody = await req.ReadAsStringAsync();
            
            if (string.IsNullOrEmpty(requestBody))
            {
                var badRequestResponse = req.CreateResponse(HttpStatusCode.BadRequest);
                await badRequestResponse.WriteStringAsync("Request body is required");
                return badRequestResponse;
            }

            var loginRequest = JsonSerializer.Deserialize<CoverLetterFunction.Services.LoginRequest>(requestBody, new JsonSerializerOptions 
            { 
                PropertyNameCaseInsensitive = true,
                MaxDepth = 10 // Prevent JSON bomb attacks
            });
            
            if (loginRequest == null)
            {
                var badRequestResponse = req.CreateResponse(HttpStatusCode.BadRequest);
                await badRequestResponse.WriteStringAsync("Invalid login data");
                return badRequestResponse;
            }

            var result = await _authenticationService.LoginAsync(loginRequest);
            
            if (result.Success)
            {
                var response = req.CreateResponse(HttpStatusCode.OK);
                await response.WriteStringAsync(JsonSerializer.Serialize(new
                {
                    success = true,
                    message = "Login successful",
                    token = result.Token,
                    user = result.User
                }));
                response.Headers.Add("Content-Type", "application/json");
                return response;
            }
            else
            {
                var unauthorizedResponse = req.CreateResponse(HttpStatusCode.Unauthorized);
                await unauthorizedResponse.WriteStringAsync(JsonSerializer.Serialize(new
                {
                    success = false,
                    message = result.Message
                }));
                unauthorizedResponse.Headers.Add("Content-Type", "application/json");
                return unauthorizedResponse;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during login");
            var errorResponse = req.CreateResponse(HttpStatusCode.InternalServerError);
            await errorResponse.WriteStringAsync("Login failed");
            return errorResponse;
        }
    }

    // Helper method to extract user ID from JWT token
    private int? ExtractUserIdFromRequest(HttpRequestData req)
    {
        if (req.Headers.TryGetValues("Authorization", out var authHeaders))
        {
            var authHeader = authHeaders.FirstOrDefault();
            if (authHeader?.StartsWith("Bearer ") == true)
            {
                var token = authHeader.Substring(7);
                return _authenticationService.ExtractUserIdFromToken(token);
            }
        }
        
        return null;
    }

    // Helper method to validate authentication
    private async Task<bool> ValidateAuthenticationAsync(HttpRequestData req)
    {
        if (req.Headers.TryGetValues("Authorization", out var authHeaders))
        {
            var authHeader = authHeaders.FirstOrDefault();
            if (authHeader?.StartsWith("Bearer ") == true)
            {
                var token = authHeader.Substring(7);
                return await _authenticationService.ValidateTokenAsync(token);
            }
        }
        
        return false;
    }

    // Helper method to create unauthorized response
    private async Task<HttpResponseData> CreateUnauthorizedResponseAsync(HttpRequestData req, string message = "Unauthorized")
    {
        var response = req.CreateResponse(HttpStatusCode.Unauthorized);
        await response.WriteStringAsync(JsonSerializer.Serialize(new
        {
            success = false,
            message = message
        }));
        response.Headers.Add("Content-Type", "application/json");
        return response;
    }

    // Additional request/response models for authentication
    public class RegisterRequest
    {
        public string FirstName { get; set; } = "";
        public string LastName { get; set; } = "";
        public string Email { get; set; } = "";
        public string Password { get; set; } = "";
    }

    public class LoginRequest
    {
        public string Email { get; set; } = "";
        public string Password { get; set; } = "";
    }

    public class CoverLetterFromJobRequest
    {
        public int JobId { get; set; }
        public string? UserEmail { get; set; }
        public int? ApplicantId { get; set; }
        public string? CustomUserProfile { get; set; }
    }

    public class CoverLetterRequest
    {
        public string JobDescription { get; set; } = "";
        public string UserProfile { get; set; } = "";
    }
    
    public class SaveCoverLetterRequest
    {
        public string Email { get; set; } = "";
        public string Name { get; set; } = "";
        public string CoverLetter { get; set; } = "";
        public string JobTitle { get; set; } = "";
        public string CompanyName { get; set; } = "";
    }

    public class UpdateProfileRequest
    {
        public string? FirstName { get; set; }
        public string? LastName { get; set; }
        public string? Location { get; set; }
        public string? JobTitle { get; set; }
        public string? AboutMe { get; set; }
        public string? ResumeFileUrl { get; set; }
        public string? JobPreferences { get; set; }
    }

    public class CreateApplicantRequest
    {
        public string FirstName { get; set; } = "";
        public string LastName { get; set; } = "";
        public string Email { get; set; } = "";
        public string Password { get; set; } = "";
        public string? Location { get; set; }
        public string? JobTitle { get; set; }
        public string? AboutMe { get; set; }
        public string? ResumeFileUrl { get; set; }
        public string? JobPreferences { get; set; }
    }
}
