// AutoJob Frontend - Azure Functions Integra    // ====== API URL BUILDING WITH FUNCTION KEYS ======ects the frontend to the Cover Letter Generator Azure Functions

class AutoJobApp {
    constructor() {
        // Initialize configuration
        this.config = new AutoJobConfig();
        this.apiBaseUrl = this.config.apiBaseUrl;
        
        this.config.log('AutoJob app starting...', {
            environment: this.config.environment,
            apiBaseUrl: this.apiBaseUrl
        });
        
        // Current user will be loaded from AuthManager when needed
        this.currentJob = null;
        
        // Pagination state for job listings
        this.allJobs = [];
        this.displayedJobsCount = 0;
        this.jobsPerPage = 5;
        
        this.init();
    }

    init() {
        // Initialize based on current page
        const path = window.location.pathname;
        const page = path.substring(path.lastIndexOf('/') + 1);
        
        switch(page) {
            case 'dashboard.html':
            case '':
            case 'index.html':
                this.initDashboard();
                break;
            case 'preview.html':
                this.initPreview();
                break;
            case 'profile.html':
                this.initProfile();
                break;
        }
    }

    // ====== API URL BUILDING WITH FUNCTION KEYS ======
    buildApiUrl(endpoint) {
        const baseUrl = `${this.apiBaseUrl}/${endpoint}`;
        const functionKey = this.config.config.functionKey;
        const requiresKey = this.config.config.requiresFunctionKey !== false; // Default to true for backward compatibility
        
        if (functionKey && requiresKey && this.config.environment === 'production') {
            return `${baseUrl}?code=${functionKey}`;
        }
        return baseUrl;
    }

    // ====== API CALLS TO AZURE FUNCTIONS ======
    async fetchJobs() {
        try {
            const url = this.buildApiUrl('GetJobs');
            this.config.log('Fetching jobs from:', url);
            const response = await fetch(url);
            
            if (!response.ok) {
                throw new Error(`HTTP error! status: ${response.status}`);
            }
            
            const jobs = await response.json();
            this.config.log('Jobs fetched:', jobs);
            return jobs;
        } catch (error) {
            this.config.error('Error fetching jobs:', error);
            
            if (this.config.features.useFallbackData) {
                this.config.log('Using fallback data for development');
                return this.getFallbackJobs();
            } else {
                throw error;
            }
        }
    }

    async fetchJobById(jobId) {
        try {
            console.log('Fetching job by ID:', jobId);
            
            // First try to find in cached jobs
            const job = this.allJobs.find(j => j.JobId === parseInt(jobId));
            if (job) {
                console.log('Job found in cache:', job);
                return job;
            }
            
            // If not in cache, fetch all jobs first
            console.log('Job not in cache, fetching all jobs...');
            const jobs = await this.fetchJobs();
            
            // Update our cache
            this.allJobs = jobs;
            
            // Now find the job
            const foundJob = jobs.find(j => j.JobId === parseInt(jobId));
            console.log('Job fetched from API:', foundJob);
            
            if (!foundJob) {
                console.error('Job not found with ID:', jobId);
                console.log('Available jobs:', jobs.map(j => ({ id: j.JobId, title: j.Title })));
            }
            
            return foundJob;
        } catch (error) {
            console.error('Error fetching job:', error);
            return null;
        }
    }

    async generateCoverLetter(jobId, userProfile = null) {
        try {
            console.log('Generating cover letter for job:', jobId);
            console.log('Auth token:', AuthManager.getToken() ? 'Present' : 'Missing');
            console.log('User info:', AuthManager.getUserInfo());
            
            const response = await AuthManager.apiCall(API_ENDPOINTS.generateCoverLetter, {
                method: 'POST',
                headers: {
                    'Content-Type': 'application/json',
                },
                body: JSON.stringify({
                    jobId: parseInt(jobId)
                })
            });

            console.log('Response received:', response);

            if (!response) {
                throw new Error('No response received (likely authentication issue)');
            }

            if (!response.ok) {
                const errorText = await response.text();
                console.error('API Error Response:', errorText);
                throw new Error(`HTTP error! status: ${response.status}, body: ${errorText}`);
            }

            const result = await response.json();
            console.log('Cover letter generated successfully:', result);
            return result;
        } catch (error) {
            console.error('Error generating cover letter:', error);
            throw error;
        }
    }

    // ====== DASHBOARD PAGE ======
    async initDashboard() {
        console.log('Initializing dashboard...');
        await this.loadJobListings();
    }

    async loadJobListings() {
        const jobsContainer = document.querySelector('.card h2');
        if (!jobsContainer || jobsContainer.textContent !== 'Job Listings') return;

        const card = jobsContainer.parentElement;
        const jobListingsDiv = document.getElementById('job-listings');
        
        // Clear the loading message and any existing content
        if (jobListingsDiv) {
            jobListingsDiv.innerHTML = '';
        }
        
        // Show loading state
        const existingJobs = card.querySelectorAll('.job-card');
        existingJobs.forEach(job => job.style.opacity = '0.5');

        try {
            const jobs = await this.fetchJobs();
            
            // Clear existing static jobs and load more button
            existingJobs.forEach(job => job.remove());
            const existingLoadMoreBtn = card.querySelector('.load-more-btn');
            if (existingLoadMoreBtn) existingLoadMoreBtn.remove();
            
            // Store all jobs and reset pagination
            this.allJobs = jobs;
            this.displayedJobsCount = 0;
            
            // Display initial jobs
            this.displayMoreJobs(card);
            
        } catch (error) {
            console.error('Failed to load jobs:', error);
            this.showError('Failed to load job listings. Please try again later.');
        }
    }

    displayMoreJobs(card) {
        const jobsToShow = this.allJobs.slice(this.displayedJobsCount, this.displayedJobsCount + this.jobsPerPage);
        const jobListingsDiv = document.getElementById('job-listings');
        
        // Add job cards to the job-listings div
        jobsToShow.forEach(job => {
            const jobCard = this.createJobCard(job);
            if (jobListingsDiv) {
                jobListingsDiv.appendChild(jobCard);
            } else {
                // Fallback: add to card if job-listings div not found
                card.appendChild(jobCard);
            }
        });
        
        this.displayedJobsCount += jobsToShow.length;
        
        // Add or update "Load More" button in the job-listings div
        this.updateLoadMoreButton(jobListingsDiv || card);
    }

    updateLoadMoreButton(card) {
        // Remove existing load more button
        const existingBtn = card.querySelector('.load-more-btn');
        if (existingBtn) existingBtn.remove();
        
        // Add load more button if there are more jobs to show
        if (this.displayedJobsCount < this.allJobs.length) {
            const loadMoreBtn = document.createElement('div');
            loadMoreBtn.className = 'load-more-btn';
            loadMoreBtn.innerHTML = `
                <button onclick="autoJob.loadMoreJobs()" class="load-more-button">
                    Load More Jobs (${this.allJobs.length - this.displayedJobsCount} remaining)
                </button>
            `;
            card.appendChild(loadMoreBtn);
        }
    }

    async loadMoreJobs() {
        const jobsContainer = document.querySelector('.card h2');
        if (!jobsContainer || jobsContainer.textContent !== 'Job Listings') return;
        
        const card = jobsContainer.parentElement;
        this.displayMoreJobs(card);
    }

    createJobCard(job) {
        const jobCard = document.createElement('div');
        jobCard.className = 'job-card';
        jobCard.innerHTML = `
            <div>
                <strong>${job.Title}</strong><br>
                <span class="job-meta">${job.Location} · ${job.Type || 'Full-Time'}</span>
                <p>${job.Snippet ? job.Snippet.substring(0, 150) + '...' : 'No description available'}</p>
                <button onclick="applyToJob(${job.JobId})" class="apply-btn">Apply</button>
            </div>
        `;
        return jobCard;
    }

    // ====== JOB APPLICATION FLOW ======
    async applyToJob(jobId) {
        console.log('=== APPLY TO JOB DEBUG ===');
        console.log('Applying to job:', jobId, 'Type:', typeof jobId);
        console.log('Current URL:', window.location.href);
        console.log('LocalStorage before storing:', Object.keys(localStorage));
        
        try {
            // Store job ID and redirect to preview
            localStorage.setItem('applyingJobId', jobId.toString());
            console.log('Stored job ID in localStorage:', localStorage.getItem('applyingJobId'));
            console.log('LocalStorage after storing:', Object.keys(localStorage));
            console.log('About to redirect to preview.html');
            window.location.href = 'preview.html';
        } catch (error) {
            console.error('Error applying to job:', error);
            this.showError('Failed to apply to job. Please try again.');
        }
    }

    // ====== PREVIEW PAGE ======
    async initPreview() {
        console.log('=== PREVIEW PAGE DEBUG ===');
        console.log('Initializing preview page...');
        console.log('Current URL:', window.location.href);
        console.log('All localStorage items:', Object.keys(localStorage));
        
        // Check for job ID in localStorage first
        let jobId = localStorage.getItem('applyingJobId');
        console.log('Job ID from localStorage:', jobId);
        
        // If not in localStorage, check URL parameters
        if (!jobId) {
            const urlParams = new URLSearchParams(window.location.search);
            jobId = urlParams.get('jobId');
            console.log('Job ID from URL params:', jobId);
            
            // If we got it from URL, store it in localStorage for consistency
            if (jobId) {
                localStorage.setItem('applyingJobId', jobId);
                console.log('Stored job ID from URL to localStorage:', jobId);
            }
        }
        
        if (!jobId) {
            console.error('No job ID found in localStorage or URL parameters');
            console.log('Available localStorage keys:', Object.keys(localStorage));
            console.log('URL search params:', window.location.search);
            this.showError('No job selected. Redirecting to dashboard...');
            setTimeout(() => window.location.href = 'dashboard.html', 2000);
            return;
        }

        await this.loadJobPreview(jobId);
    }

    async loadJobPreview(jobId) {
        const textarea = document.querySelector('textarea');
        const sendButton = document.querySelector('.button[href="confirmation.html"]');
        
        if (!textarea) return;
        
        // Show loading state
        textarea.value = 'Generating your personalized cover letter... This may take a few moments.';
        textarea.style.background = '#f9f9f9';
        
        if (sendButton) {
            sendButton.style.opacity = '0.5';
            sendButton.style.pointerEvents = 'none';
        }

        try {
            // Fetch job details
            const job = await this.fetchJobById(jobId);
            if (!job) {
                throw new Error('Job not found');
            }

            // Update page title with job info
            const h2 = document.querySelector('h2');
            if (h2) {
                h2.textContent = `Cover Letter for ${job.Title}`;
            }

            // Generate cover letter
            const result = await this.generateCoverLetter(jobId);
            
            // Display cover letter
            textarea.value = result.coverLetter || 'Cover letter generated successfully!';
            textarea.style.background = 'white';
            
            // Store for confirmation page - using the format expected by sendApplication
            localStorage.setItem('generatedCoverLetter', JSON.stringify({
                coverLetter: result.coverLetter,
                userName: result.userName,
                userEmail: result.userEmail,
                jobTitle: result.jobTitle,
                companyName: result.companyName,
                job: job, // Keep for backward compatibility
                timestamp: new Date().toISOString()
            }));
            
            // Enable send button
            if (sendButton) {
                sendButton.style.opacity = '1';
                sendButton.style.pointerEvents = 'auto';
            }
            
        } catch (error) {
            console.error('Error loading job preview:', error);
            textarea.value = 'Failed to generate cover letter. Please try again or contact support.';
            textarea.style.background = '#ffe6e6';
        }
    }

    async sendApplication() {
        console.log('Sending application...');
        
        try {
            // Get the stored cover letter data
            const storedData = localStorage.getItem('generatedCoverLetter');
            if (!storedData) {
                this.showError('No cover letter found. Please generate a cover letter first.');
                return;
            }

            const coverLetterData = JSON.parse(storedData);
            if (!coverLetterData.coverLetter) {
                this.showError('Invalid cover letter data. Please regenerate the cover letter.');
                return;
            }

            // Show loading state
            const sendButton = document.querySelector('.button[onclick*="sendApplication"]');
            if (sendButton) {
                sendButton.textContent = 'Sending...';
                sendButton.style.opacity = '0.5';
                sendButton.style.pointerEvents = 'none';
            }

            // Prepare email data - send TO the hiring manager (you)
            const emailData = {
                Email: "chri20q5@protonmail.com", // Always send to you as the hiring manager
                Name: coverLetterData.userName, // Applicant name from the authenticated user
                CoverLetter: coverLetterData.coverLetter,
                JobTitle: coverLetterData.jobTitle,
                CompanyName: coverLetterData.companyName || "Company"
            };

            // Send email
            const url = this.buildApiUrl('SendEmail');
            console.log('Sending email to:', url);
            
            const response = await fetch(url, {
                method: 'POST',
                headers: {
                    'Content-Type': 'application/json'
                },
                body: JSON.stringify(emailData)
            });

            if (!response.ok) {
                const errorText = await response.text();
                throw new Error(`Failed to send email: ${response.status} ${errorText}`);
            }

            const result = await response.json();
            console.log('Email sent successfully:', result);

            // Success - redirect to confirmation page
            window.location.href = 'confirmation.html';

        } catch (error) {
            console.error('Error sending application:', error);
            this.showError(`Failed to send application: ${error.message}`);
            
            // Restore send button
            const sendButton = document.querySelector('.button[onclick*="sendApplication"]');
            if (sendButton) {
                sendButton.textContent = 'Send Application';
                sendButton.style.opacity = '1';
                sendButton.style.pointerEvents = 'auto';
            }
        }
    }

    // ====== UTILITY METHODS ======
    showError(message) {
        // Create or update error message
        let errorDiv = document.querySelector('.error-message');
        if (!errorDiv) {
            errorDiv = document.createElement('div');
            errorDiv.className = 'error-message';
            errorDiv.style.cssText = `
                background: #ffe6e6;
                color: #d8000c;
                padding: 1rem;
                border-radius: 8px;
                margin: 1rem 0;
                border: 1px solid #d8000c;
            `;
            document.querySelector('.container').prepend(errorDiv);
        }
        errorDiv.textContent = message;
        
        // Auto-hide after 5 seconds
        setTimeout(() => {
            if (errorDiv) errorDiv.remove();
        }, 5000);
    }

    // Fallback jobs for local development when API is not available
    getFallbackJobs() {
        return [
            {
                JobId: 999,
                Title: "Frontend Developer (Demo)",
                Location: "Berlin",
                Type: "Full-Time",
                CompanyName: "Demo Company",
                Snippet: "This is a demo job for testing the frontend integration. Build beautiful, responsive web interfaces using React and modern tools."
            },
            {
                JobId: 998,
                Title: "Cloud Engineer (Demo)",
                Location: "Remote",
                Type: "Contract", 
                CompanyName: "Demo Cloud Corp",
                Snippet: "This is a demo job for testing. Design, deploy, and maintain scalable cloud infrastructures on Azure and AWS for global projects."
            }
        ];
    }
}

// Initialize the app when DOM is loaded
document.addEventListener('DOMContentLoaded', () => {
    window.autoJob = new AutoJobApp();
    console.log('AutoJob app initialized');
});

// Global functions for inline event handlers
window.applyToJob = (jobId) => {
    console.log('Global applyToJob called with:', jobId);
    if (window.autoJob) {
        return window.autoJob.applyToJob(jobId);
    } else {
        console.error('AutoJob not initialized yet');
        alert('Application is still loading. Please try again in a moment.');
    }
};
window.sendApplication = () => {
    if (window.autoJob) {
        return window.autoJob.sendApplication();
    } else {
        console.error('AutoJob not initialized yet');
        alert('Application is still loading. Please try again in a moment.');
    }
};
window.loadMoreJobs = () => {
    if (window.autoJob) {
        return window.autoJob.loadMoreJobs();
    } else {
        console.error('AutoJob not initialized yet');
        alert('Application is still loading. Please try again in a moment.');
    }
};
