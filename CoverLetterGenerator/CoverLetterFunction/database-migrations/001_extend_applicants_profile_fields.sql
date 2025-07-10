-- Migration Script: Extend applicants table for profile page support
-- Run this script against the JobAppDB database

-- Add new columns to support profile page functionality
ALTER TABLE jobapp.applicants
ADD 
    job_title NVARCHAR(100) NULL,
    about_me NVARCHAR(2000) NULL,
    resume_file_url NVARCHAR(255) NULL,
    job_preferences NVARCHAR(1000) NULL,
    created_at DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
    updated_at DATETIME2 NOT NULL DEFAULT GETUTCDATE();
GO

-- Update existing records to have proper timestamps  
UPDATE jobapp.applicants 
SET 
    created_at = GETUTCDATE(),
    updated_at = GETUTCDATE()
WHERE created_at IS NULL OR updated_at IS NULL;
GO

-- Create an index on email for faster lookups (useful for authentication)
IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_applicants_email' AND object_id = OBJECT_ID('jobapp.applicants'))
BEGIN
    CREATE INDEX IX_applicants_email ON jobapp.applicants (email);
END
GO

-- Add a trigger to automatically update the updated_at timestamp
IF OBJECT_ID('jobapp.TR_applicants_UpdateTimestamp', 'TR') IS NOT NULL
    DROP TRIGGER jobapp.TR_applicants_UpdateTimestamp;
GO

CREATE TRIGGER jobapp.TR_applicants_UpdateTimestamp
ON jobapp.applicants
AFTER UPDATE
AS
BEGIN
    SET NOCOUNT ON;
    
    UPDATE jobapp.applicants
    SET updated_at = GETUTCDATE()
    FROM jobapp.applicants a
    INNER JOIN inserted i ON a.applicant_id = i.applicant_id;
END
GO

PRINT 'Migration completed successfully - applicants table extended for profile page support';
