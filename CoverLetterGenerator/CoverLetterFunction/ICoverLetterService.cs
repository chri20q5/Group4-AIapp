using System.Threading.Tasks;

public interface ICoverLetterService
{
    Task<string> GenerateCoverLetterAsync(string jobDescription, string userProfile);
    Task<string> SaveCoverLetterToBlobAsync(string coverLetter, string email, string name, string jobTitle, string companyName);
}
