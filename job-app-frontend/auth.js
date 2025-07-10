// Authentication utility functions
class AuthManager {
    static getToken() {
        return localStorage.getItem('authToken');
    }
    
    static getUserInfo() {
        const userInfo = localStorage.getItem('userInfo');
        try {
            return userInfo ? JSON.parse(userInfo) : null;
        } catch (error) {
            console.error('Error parsing user info:', error);
            return null;
        }
    }
    
    static setAuthData(token, userInfo) {
        localStorage.setItem('authToken', token);
        localStorage.setItem('userInfo', JSON.stringify(userInfo));
    }
    
    static clearAuthData() {
        localStorage.removeItem('authToken');
        localStorage.removeItem('userInfo');
    }
    
    static isAuthenticated() {
        return !!(this.getToken() && this.getUserInfo());
    }
    
    static redirectToLogin() {
        window.location.href = 'login.html';
    }
    
    static logout() {
        this.clearAuthData();
        window.location.href = 'home.html';
    }
    
    // Make authenticated API calls
    static async apiCall(url, options = {}) {
        const token = this.getToken();
        
        if (!token) {
            this.redirectToLogin();
            return null;
        }
        
        const headers = {
            'Content-Type': 'application/json',
            'Authorization': `Bearer ${token}`,
            ...options.headers
        };
        
        try {
            const response = await fetch(url, {
                ...options,
                headers
            });
            
            if (response.status === 401) {
                // Token is invalid
                this.logout();
                return null;
            }
            
            return response;
        } catch (error) {
            console.error('API call error:', error);
            throw error;
        }
    }
}

// API endpoints configuration
const API_BASE_URL = 'https://testhtw.azurewebsites.net/api';

const API_ENDPOINTS = {
    register: `${API_BASE_URL}/register`,
    login: `${API_BASE_URL}/login`,
    profile: `${API_BASE_URL}/getuserprofile`,
    updateProfile: `${API_BASE_URL}/updateuserprofile`,
    jobs: `${API_BASE_URL}/getjobs`,
    jobById: (id) => `${API_BASE_URL}/jobs/${id}`,
    generateCoverLetter: `${API_BASE_URL}/generatecoverletterfromjob`
};
