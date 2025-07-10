using CoverLetterFunction.Models;

namespace CoverLetterFunction.Services;

public interface IDatabaseService
{
    Task<IEnumerable<Job>> GetJobsAsync();
    Task<Job?> GetJobByIdAsync(int jobId);
    Task<Applicant?> GetApplicantByEmailAsync(string email);
    Task<Applicant?> GetApplicantByIdAsync(int applicantId);
    Task<IEnumerable<Applicant>> GetApplicantsAsync();
    Task<bool> UpdateApplicantProfileAsync(int applicantId, Applicant updatedApplicant);
    Task<int> CreateApplicantAsync(Applicant newApplicant);
}
