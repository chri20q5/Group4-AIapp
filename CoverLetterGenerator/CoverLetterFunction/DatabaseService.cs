using System.Data;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using CoverLetterFunction.Models;
using CoverLetterFunction.Services;

public class DatabaseService : IDatabaseService
{
    private readonly string _connectionString;
    private readonly ILogger<DatabaseService> _logger;

    public DatabaseService(IConfiguration configuration, ILogger<DatabaseService> logger)
    {
        _connectionString = configuration["DatabaseConnectionString"] 
            ?? throw new ArgumentNullException("DatabaseConnectionString configuration is missing");
        _logger = logger;
    }

    public async Task<IEnumerable<Job>> GetJobsAsync()
    {
        var jobs = new List<Job>();
        
        try
        {
            using var connection = new SqlConnection(_connectionString);
            await connection.OpenAsync();
            
            const string sql = @"
                SELECT JobID, Title, Location, Snippet, Salary, Source, Link, Updated, JobType
                FROM jobapp.joblist
                ORDER BY JobID DESC";
            
            using var command = new SqlCommand(sql, connection);
            using var reader = await command.ExecuteReaderAsync();
            
            while (await reader.ReadAsync())
            {
                jobs.Add(new Job
                {
                    JobId = reader.GetInt32("JobID"),
                    Title = reader.GetString("Title"),
                    Location = reader.IsDBNull("Location") ? null : reader.GetString("Location"),
                    Snippet = reader.IsDBNull("Snippet") ? null : reader.GetString("Snippet"),
                    Salary = reader.IsDBNull("Salary") ? null : reader.GetString("Salary"),
                    Source = reader.IsDBNull("Source") ? null : reader.GetString("Source"),
                    Link = reader.GetString("Link"),
                    Updated = reader.IsDBNull("Updated") ? null : reader.GetString("Updated"),
                    JobType = reader.IsDBNull("JobType") ? null : reader.GetString("JobType")
                });
            }
            
            _logger.LogInformation("Retrieved {Count} jobs from database", jobs.Count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving jobs from database");
            throw;
        }
        
        return jobs;
    }

    public async Task<Job?> GetJobByIdAsync(int jobId)
    {
        try
        {
            using var connection = new SqlConnection(_connectionString);
            await connection.OpenAsync();
            
            const string sql = @"
                SELECT JobID, Title, Location, Snippet, Salary, Source, Link, Updated, JobType
                FROM jobapp.joblist
                WHERE JobID = @JobId";
            
            using var command = new SqlCommand(sql, connection);
            command.Parameters.AddWithValue("@JobId", jobId);
            
            using var reader = await command.ExecuteReaderAsync();
            
            if (await reader.ReadAsync())
            {
                return new Job
                {
                    JobId = reader.GetInt32("JobID"),
                    Title = reader.GetString("Title"),
                    Location = reader.IsDBNull("Location") ? null : reader.GetString("Location"),
                    Snippet = reader.IsDBNull("Snippet") ? null : reader.GetString("Snippet"),
                    Salary = reader.IsDBNull("Salary") ? null : reader.GetString("Salary"),
                    Source = reader.IsDBNull("Source") ? null : reader.GetString("Source"),
                    Link = reader.GetString("Link"),
                    Updated = reader.IsDBNull("Updated") ? null : reader.GetString("Updated"),
                    JobType = reader.IsDBNull("JobType") ? null : reader.GetString("JobType")
                };
            }
            
            _logger.LogInformation("Job with ID {JobId} not found", jobId);
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving job {JobId} from database", jobId);
            throw;
        }
    }

    public async Task<IEnumerable<Applicant>> GetApplicantsAsync()
    {
        var applicants = new List<Applicant>();
        
        try
        {
            using var connection = new SqlConnection(_connectionString);
            await connection.OpenAsync();
            
            const string sql = @"
                SELECT applicant_id, first_name, last_name, location, email, password
                FROM jobapp.applicants
                ORDER BY applicant_id";
            
            using var command = new SqlCommand(sql, connection);
            using var reader = await command.ExecuteReaderAsync();
            
            while (await reader.ReadAsync())
            {
                applicants.Add(new Applicant
                {
                    ApplicantId = reader.GetInt32("applicant_id"),
                    FirstName = reader.GetString("first_name"),
                    LastName = reader.GetString("last_name"),
                    Location = reader.IsDBNull("location") ? null : reader.GetString("location"),
                    Email = reader.GetString("email"),
                    Password = reader.GetString("password")
                });
            }
            
            _logger.LogInformation("Retrieved {Count} applicants from database", applicants.Count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving applicants from database");
            throw;
        }
        
        return applicants;
    }

    public async Task<Applicant?> GetApplicantByIdAsync(int applicantId)
    {
        try
        {
            using var connection = new SqlConnection(_connectionString);
            await connection.OpenAsync();
            
            const string sql = @"
                SELECT applicant_id, first_name, last_name, location, email, password,
                       job_title, about_me, resume_file_url, job_preferences, 
                       created_at, updated_at
                FROM jobapp.applicants
                WHERE applicant_id = @ApplicantId";
            
            using var command = new SqlCommand(sql, connection);
            command.Parameters.AddWithValue("@ApplicantId", applicantId);
            
            using var reader = await command.ExecuteReaderAsync();
            
            if (await reader.ReadAsync())
            {
                return new Applicant
                {
                    ApplicantId = reader.GetInt32("applicant_id"),
                    FirstName = reader.GetString("first_name"),
                    LastName = reader.GetString("last_name"),
                    Location = reader.IsDBNull("location") ? null : reader.GetString("location"),
                    Email = reader.GetString("email"),
                    Password = reader.GetString("password"),
                    JobTitle = reader.IsDBNull("job_title") ? null : reader.GetString("job_title"),
                    AboutMe = reader.IsDBNull("about_me") ? null : reader.GetString("about_me"),
                    ResumeFileUrl = reader.IsDBNull("resume_file_url") ? null : reader.GetString("resume_file_url"),
                    JobPreferences = reader.IsDBNull("job_preferences") ? null : reader.GetString("job_preferences"),
                    CreatedAt = reader.IsDBNull("created_at") ? DateTime.UtcNow : reader.GetDateTime("created_at"),
                    UpdatedAt = reader.IsDBNull("updated_at") ? DateTime.UtcNow : reader.GetDateTime("updated_at")
                };
            }
            
            _logger.LogWarning("Applicant {ApplicantId} not found", applicantId);
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving applicant {ApplicantId} from database", applicantId);
            throw;
        }
    }

    public async Task<Applicant?> GetApplicantByEmailAsync(string email)
    {
        try
        {
            using var connection = new SqlConnection(_connectionString);
            await connection.OpenAsync();
            
            const string sql = @"
                SELECT applicant_id, first_name, last_name, location, email, password,
                       job_title, about_me, resume_file_url, job_preferences, 
                       created_at, updated_at
                FROM jobapp.applicants
                WHERE email = @Email";
            
            using var command = new SqlCommand(sql, connection);
            command.Parameters.AddWithValue("@Email", email);
            
            using var reader = await command.ExecuteReaderAsync();
            
            if (await reader.ReadAsync())
            {
                return new Applicant
                {
                    ApplicantId = reader.GetInt32("applicant_id"),
                    FirstName = reader.GetString("first_name"),
                    LastName = reader.GetString("last_name"),
                    Location = reader.IsDBNull("location") ? null : reader.GetString("location"),
                    Email = reader.GetString("email"),
                    Password = reader.GetString("password"),
                    JobTitle = reader.IsDBNull("job_title") ? null : reader.GetString("job_title"),
                    AboutMe = reader.IsDBNull("about_me") ? null : reader.GetString("about_me"),
                    ResumeFileUrl = reader.IsDBNull("resume_file_url") ? null : reader.GetString("resume_file_url"),
                    JobPreferences = reader.IsDBNull("job_preferences") ? null : reader.GetString("job_preferences"),
                    CreatedAt = reader.IsDBNull("created_at") ? DateTime.UtcNow : reader.GetDateTime("created_at"),
                    UpdatedAt = reader.IsDBNull("updated_at") ? DateTime.UtcNow : reader.GetDateTime("updated_at")
                };
            }
            
            _logger.LogInformation("Applicant with email {Email} not found", email);
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving applicant by email {Email} from database", email);
            throw;
        }
    }

    public async Task<bool> UpdateApplicantProfileAsync(int applicantId, Applicant updatedApplicant)
    {
        try
        {
            using var connection = new SqlConnection(_connectionString);
            await connection.OpenAsync();
            
            const string sql = @"
                UPDATE jobapp.applicants 
                SET first_name = @FirstName,
                    last_name = @LastName,
                    location = @Location,
                    job_title = @JobTitle,
                    about_me = @AboutMe,
                    resume_file_url = @ResumeFileUrl,
                    job_preferences = @JobPreferences,
                    updated_at = GETUTCDATE()
                WHERE applicant_id = @ApplicantId";
            
            using var command = new SqlCommand(sql, connection);
            command.Parameters.AddWithValue("@ApplicantId", applicantId);
            command.Parameters.AddWithValue("@FirstName", updatedApplicant.FirstName);
            command.Parameters.AddWithValue("@LastName", updatedApplicant.LastName);
            command.Parameters.AddWithValue("@Location", (object?)updatedApplicant.Location ?? DBNull.Value);
            command.Parameters.AddWithValue("@JobTitle", (object?)updatedApplicant.JobTitle ?? DBNull.Value);
            command.Parameters.AddWithValue("@AboutMe", (object?)updatedApplicant.AboutMe ?? DBNull.Value);
            command.Parameters.AddWithValue("@ResumeFileUrl", (object?)updatedApplicant.ResumeFileUrl ?? DBNull.Value);
            command.Parameters.AddWithValue("@JobPreferences", (object?)updatedApplicant.JobPreferences ?? DBNull.Value);
            
            var rowsAffected = await command.ExecuteNonQueryAsync();
            
            if (rowsAffected > 0)
            {
                _logger.LogInformation("Successfully updated profile for applicant {ApplicantId}", applicantId);
                return true;
            }
            else
            {
                _logger.LogWarning("No rows affected when updating applicant {ApplicantId} - applicant may not exist", applicantId);
                return false;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating applicant profile {ApplicantId} in database", applicantId);
            throw;
        }
    }

    public async Task<int> CreateApplicantAsync(Applicant newApplicant)
    {
        try
        {
            using var connection = new SqlConnection(_connectionString);
            await connection.OpenAsync();
            
            const string sql = @"
                INSERT INTO jobapp.applicants 
                (first_name, last_name, location, email, password, job_title, about_me, 
                 resume_file_url, job_preferences, created_at, updated_at)
                OUTPUT INSERTED.applicant_id
                VALUES 
                (@FirstName, @LastName, @Location, @Email, @Password, @JobTitle, @AboutMe,
                 @ResumeFileUrl, @JobPreferences, GETUTCDATE(), GETUTCDATE())";
            
            using var command = new SqlCommand(sql, connection);
            command.Parameters.AddWithValue("@FirstName", newApplicant.FirstName);
            command.Parameters.AddWithValue("@LastName", newApplicant.LastName);
            command.Parameters.AddWithValue("@Location", (object?)newApplicant.Location ?? DBNull.Value);
            command.Parameters.AddWithValue("@Email", newApplicant.Email);
            command.Parameters.AddWithValue("@Password", newApplicant.Password); // This should be hashed by authentication service
            command.Parameters.AddWithValue("@JobTitle", (object?)newApplicant.JobTitle ?? DBNull.Value);
            command.Parameters.AddWithValue("@AboutMe", (object?)newApplicant.AboutMe ?? DBNull.Value);
            command.Parameters.AddWithValue("@ResumeFileUrl", (object?)newApplicant.ResumeFileUrl ?? DBNull.Value);
            command.Parameters.AddWithValue("@JobPreferences", (object?)newApplicant.JobPreferences ?? DBNull.Value);
            
            var newApplicantId = await command.ExecuteScalarAsync();
            
            if (newApplicantId != null)
            {
                var applicantId = (int)newApplicantId;
                _logger.LogInformation("Successfully created new applicant with ID {ApplicantId}", applicantId);
                return applicantId;
            }
            else
            {
                _logger.LogError("Failed to create new applicant - no ID returned");
                throw new InvalidOperationException("Failed to create applicant");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating new applicant in database");
            throw;
        }
    }
}
