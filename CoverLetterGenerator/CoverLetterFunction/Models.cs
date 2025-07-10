using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CoverLetterFunction.Models;

[Table("jobapp.applicants")]
public class Applicant
{
    [Key]
    [Column("applicant_id")]
    public int ApplicantId { get; set; }
    
    [Required]
    [Column("first_name")]
    [StringLength(50)]
    public string FirstName { get; set; } = string.Empty;
    
    [Required]
    [Column("last_name")]
    [StringLength(50)]
    public string LastName { get; set; } = string.Empty;
    
    [Column("location")]
    [StringLength(100)]
    public string? Location { get; set; }
    
    [Required]
    [Column("email")]
    [StringLength(100)]
    public string Email { get; set; } = string.Empty;
    
    [Required]
    [Column("password")]
    [StringLength(100)]
    public string Password { get; set; } = string.Empty;
    
    // New fields for profile page integration
    [Column("job_title")]
    [StringLength(100)]
    public string? JobTitle { get; set; }
    
    [Column("about_me")]
    [StringLength(2000)]
    public string? AboutMe { get; set; }
    
    [Column("resume_file_url")]
    [StringLength(255)]
    public string? ResumeFileUrl { get; set; }
    
    [Column("job_preferences")]
    [StringLength(1000)]
    public string? JobPreferences { get; set; }
    
    [Column("created_at")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    
    [Column("updated_at")]
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}

[Table("jobapp.joblist")]
public class Job
{
    [Key]
    [Column("JobID")]
    public int JobId { get; set; }
    
    [Required]
    [Column("Title")]
    [StringLength(100)]
    public string Title { get; set; } = string.Empty;
    
    [Column("Location")]
    [StringLength(100)]
    public string? Location { get; set; }
    
    [Column("Snippet")]
    public string? Snippet { get; set; }
    
    [Column("Salary")]
    [StringLength(100)]
    public string? Salary { get; set; }
    
    [Column("Source")]
    [StringLength(100)]
    public string? Source { get; set; }
    
    [Required]
    [Column("Link")]
    [StringLength(500)]
    public string Link { get; set; } = string.Empty;
    
    [Column("Updated")]
    [StringLength(100)]
    public string? Updated { get; set; }
    
    [Column("JobType")]
    [StringLength(50)]
    public string? JobType { get; set; }
}
