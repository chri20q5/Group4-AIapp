// Profile management functionality for the job portal
// This module handles user profile operations including getting, updating, and displaying profile data

class ProfileManager {
    constructor() {
        this.currentUser = null;
        this.isAuthenticated = false;
    }

    // Initialize profile management
    async init() {
        // This will be connected to your colleague's authentication system
        await this.checkAuthenticationStatus();
        this.bindProfileFormEvents();
    }

    // Check if user is authenticated (placeholder for auth integration)
    async checkAuthenticationStatus() {
        // TODO: Integrate with authentication system from colleague
        // For now, check if user data exists in sessionStorage or localStorage
        const userData = localStorage.getItem('userData');
        if (userData) {
            try {
                this.currentUser = JSON.parse(userData);
                this.isAuthenticated = true;
                console.log('User authenticated:', this.currentUser);
            } catch (error) {
                console.error('Error parsing user data:', error);
                this.isAuthenticated = false;
            }
        } else {
            this.isAuthenticated = false;
        }
    }

    // Get user profile from backend
    async getUserProfile(userId) {
        try {
            const response = await fetch(`${window.appConfig.apiBaseUrl}/GetUserProfile?userId=${userId}`, {
                method: 'GET',
                headers: {
                    'Content-Type': 'application/json'
                }
            });

            if (!response.ok) {
                throw new Error(`Failed to get user profile: ${response.statusText}`);
            }

            const profileData = await response.json();
            return profileData;
        } catch (error) {
            console.error('Error getting user profile:', error);
            throw error;
        }
    }

    // Update user profile
    async updateUserProfile(profileData) {
        try {
            const response = await fetch(`${window.appConfig.apiBaseUrl}/UpdateUserProfile`, {
                method: 'PUT',
                headers: {
                    'Content-Type': 'application/json'
                },
                body: JSON.stringify(profileData)
            });

            if (!response.ok) {
                throw new Error(`Failed to update profile: ${response.statusText}`);
            }

            const result = await response.json();
            return result;
        } catch (error) {
            console.error('Error updating user profile:', error);
            throw error;
        }
    }

    // Create new user account (for integration with sign up)
    async createUserAccount(registrationData) {
        try {
            const response = await fetch(`${window.appConfig.apiBaseUrl}/CreateApplicant`, {
                method: 'POST',
                headers: {
                    'Content-Type': 'application/json'
                },
                body: JSON.stringify(registrationData)
            });

            if (!response.ok) {
                throw new Error(`Failed to create account: ${response.statusText}`);
            }

            const result = await response.json();
            return result;
        } catch (error) {
            console.error('Error creating user account:', error);
            throw error;
        }
    }

    // Load and display profile data in form
    async loadProfileData(userId) {
        try {
            const profileData = await this.getUserProfile(userId);
            this.populateProfileForm(profileData);
            return profileData;
        } catch (error) {
            console.error('Error loading profile data:', error);
            this.showProfileError('Failed to load profile data');
        }
    }

    // Populate profile form with user data
    populateProfileForm(profileData) {
        // Full Name
        const fullNameInput = document.getElementById('fullName');
        if (fullNameInput) {
            fullNameInput.value = profileData.FullName || `${profileData.FirstName || ''} ${profileData.LastName || ''}`.trim();
        }

        // Job Title
        const jobTitleInput = document.getElementById('jobTitle');
        if (jobTitleInput) {
            jobTitleInput.value = profileData.JobTitle || '';
        }

        // Location
        const locationInput = document.getElementById('location');
        if (locationInput) {
            locationInput.value = profileData.Location || '';
        }

        // About Me
        const aboutMeTextarea = document.getElementById('aboutMe');
        if (aboutMeTextarea) {
            aboutMeTextarea.value = profileData.AboutMe || '';
        }

        // Job Preferences
        const jobPreferencesTextarea = document.getElementById('jobPreferences');
        if (jobPreferencesTextarea) {
            jobPreferencesTextarea.value = profileData.JobPreferences || '';
        }

        // Resume URL (if exists)
        const resumeUrlInput = document.getElementById('resumeUrl');
        if (resumeUrlInput) {
            resumeUrlInput.value = profileData.ResumeFileUrl || '';
        }
    }

    // Bind profile form events
    bindProfileFormEvents() {
        // Save profile button
        const saveProfileBtn = document.getElementById('saveProfile');
        if (saveProfileBtn) {
            saveProfileBtn.addEventListener('click', (e) => this.handleSaveProfile(e));
        }

        // Upload resume button (placeholder for file upload)
        const uploadResumeBtn = document.getElementById('uploadResume');
        if (uploadResumeBtn) {
            uploadResumeBtn.addEventListener('click', (e) => this.handleResumeUpload(e));
        }

        // Auto-save on form field changes (debounced)
        const formFields = ['fullName', 'jobTitle', 'location', 'aboutMe', 'jobPreferences'];
        formFields.forEach(fieldId => {
            const field = document.getElementById(fieldId);
            if (field) {
                field.addEventListener('input', this.debounce(() => {
                    console.log(`Field ${fieldId} changed, preparing auto-save...`);
                    // Auto-save could be implemented here
                }, 2000));
            }
        });
    }

    // Handle save profile action
    async handleSaveProfile(event) {
        event.preventDefault();
        
        if (!this.isAuthenticated || !this.currentUser) {
            this.showProfileError('You must be logged in to save your profile');
            return;
        }

        const saveBtn = event.target;
        const originalText = saveBtn.textContent;
        
        try {
            // Show loading state
            saveBtn.textContent = 'Saving...';
            saveBtn.disabled = true;

            // Collect form data
            const formData = this.collectProfileFormData();
            
            // Add user ID to the data
            formData.ApplicantId = this.currentUser.ApplicantId || this.currentUser.applicantId;

            // Update profile
            const result = await this.updateUserProfile(formData);
            
            if (result.success) {
                this.showProfileSuccess('Profile updated successfully!');
                
                // Update local user data
                const updatedProfile = await this.getUserProfile(formData.ApplicantId);
                this.currentUser = { ...this.currentUser, ...updatedProfile };
                localStorage.setItem('userData', JSON.stringify(this.currentUser));
            } else {
                throw new Error(result.message || 'Failed to update profile');
            }
        } catch (error) {
            console.error('Error saving profile:', error);
            this.showProfileError('Failed to save profile: ' + error.message);
        } finally {
            // Restore button state
            saveBtn.textContent = originalText;
            saveBtn.disabled = false;
        }
    }

    // Collect data from profile form
    collectProfileFormData() {
        const fullName = document.getElementById('fullName')?.value || '';
        const [firstName, ...lastNameParts] = fullName.trim().split(' ');
        const lastName = lastNameParts.join(' ');

        return {
            FirstName: firstName || '',
            LastName: lastName || '',
            JobTitle: document.getElementById('jobTitle')?.value || '',
            Location: document.getElementById('location')?.value || '',
            AboutMe: document.getElementById('aboutMe')?.value || '',
            JobPreferences: document.getElementById('jobPreferences')?.value || '',
            ResumeFileUrl: document.getElementById('resumeUrl')?.value || ''
        };
    }

    // Handle resume upload (placeholder for file upload integration)
    async handleResumeUpload(event) {
        event.preventDefault();
        
        // This is a placeholder for file upload functionality
        // You'll need to integrate with a file upload service (Azure Blob Storage, etc.)
        console.log('Resume upload clicked - integration needed');
        
        // For now, show a file input dialog
        const input = document.createElement('input');
        input.type = 'file';
        input.accept = '.pdf,.doc,.docx';
        input.onchange = async (e) => {
            const file = e.target.files[0];
            if (file) {
                // TODO: Upload file to storage and get URL
                console.log('File selected:', file.name);
                this.showProfileSuccess(`Resume file "${file.name}" selected. File upload integration needed.`);
            }
        };
        input.click();
    }

    // Show profile success message
    showProfileSuccess(message) {
        this.showProfileMessage(message, 'success');
    }

    // Show profile error message
    showProfileError(message) {
        this.showProfileMessage(message, 'error');
    }

    // Show profile message
    showProfileMessage(message, type = 'info') {
        // Remove existing messages
        const existingMessages = document.querySelectorAll('.profile-message');
        existingMessages.forEach(msg => msg.remove());

        // Create message element
        const messageDiv = document.createElement('div');
        messageDiv.className = `profile-message profile-message-${type}`;
        messageDiv.textContent = message;
        
        // Style the message
        messageDiv.style.cssText = `
            position: fixed;
            top: 20px;
            right: 20px;
            padding: 12px 20px;
            border-radius: 6px;
            font-weight: 500;
            z-index: 1000;
            max-width: 300px;
            animation: slideIn 0.3s ease-out;
        `;

        if (type === 'success') {
            messageDiv.style.cssText += `
                background-color: #d1edff;
                color: #0c5460;
                border: 1px solid #a8d4e6;
            `;
        } else if (type === 'error') {
            messageDiv.style.cssText += `
                background-color: #ffd6d6;
                color: #721c24;
                border: 1px solid #f5a3a3;
            `;
        }

        // Add to document
        document.body.appendChild(messageDiv);

        // Auto-remove after 5 seconds
        setTimeout(() => {
            if (messageDiv.parentNode) {
                messageDiv.remove();
            }
        }, 5000);
    }

    // Utility: Debounce function
    debounce(func, wait) {
        let timeout;
        return function executedFunction(...args) {
            const later = () => {
                clearTimeout(timeout);
                func(...args);
            };
            clearTimeout(timeout);
            timeout = setTimeout(later, wait);
        };
    }

    // Method to be called when user logs in (for auth integration)
    onUserLogin(userData) {
        this.currentUser = userData;
        this.isAuthenticated = true;
        localStorage.setItem('userData', JSON.stringify(userData));
        
        // Load profile data for the logged-in user
        if (userData.ApplicantId || userData.applicantId) {
            this.loadProfileData(userData.ApplicantId || userData.applicantId);
        }
    }

    // Method to be called when user logs out (for auth integration)
    onUserLogout() {
        this.currentUser = null;
        this.isAuthenticated = false;
        localStorage.removeItem('userData');
        
        // Clear profile form
        const formFields = ['fullName', 'jobTitle', 'location', 'aboutMe', 'jobPreferences', 'resumeUrl'];
        formFields.forEach(fieldId => {
            const field = document.getElementById(fieldId);
            if (field) {
                field.value = '';
            }
        });
    }
}

// CSS Animation for messages
const style = document.createElement('style');
style.textContent = `
    @keyframes slideIn {
        from {
            transform: translateX(100%);
            opacity: 0;
        }
        to {
            transform: translateX(0);
            opacity: 1;
        }
    }
`;
document.head.appendChild(style);

// Global instance
window.profileManager = new ProfileManager();

// Auto-initialize when DOM is ready
if (document.readyState === 'loading') {
    document.addEventListener('DOMContentLoaded', () => window.profileManager.init());
} else {
    window.profileManager.init();
}
