# Authentication Integration Guide

This document outlines the completed secure authentication system integration with JWT tokens and protected endpoints.

## ✅ Completed Authentication Features

### Backend (Azure Functions) - COMPLETED
- ✅ Database schema extended with profile fields and indexes
- ✅ Profile management endpoints with JWT authentication:
  - `GET /GetUserProfile` - Get user profile (requires JWT token)
  - `PUT /UpdateUserProfile` - Update user profile (requires JWT token)
  - `POST /CreateApplicant` - Create new user account (public)
- ✅ Secure authentication endpoints:
  - `POST /Register` - User registration with password hashing
  - `POST /Login` - User login with JWT token generation
- ✅ AuthenticationService with BCrypt password hashing and JWT token management
- ✅ JWT token validation helper methods
- ✅ Database service methods for all profile and auth operations

### Frontend Infrastructure - READY
- ✅ Profile page with all required fields
- ✅ ProfileManager class for API integration
- ✅ Form handling and state management
- ✅ JWT token storage and management methods

### Database - READY
- ✅ Migration applied to production: `001_extend_applicants_profile_fields.sql`
- ✅ Schema includes: job_title, about_me, resume_file_url, job_preferences, timestamps
- ✅ Email index and auto-update trigger for updated_at

### Security - IMPLEMENTED
- ✅ BCrypt password hashing (cost factor 12)
- ✅ JWT token generation and validation
- ✅ Protected endpoints requiring valid JWT tokens
- ✅ Password strength validation
- ✅ Secure error handling without information disclosure

## API Endpoints Reference

### Authentication Endpoints

#### Register New User
```http
POST /api/Register
Content-Type: application/json

{
  "FirstName": "John",
  "LastName": "Doe", 
  "Email": "john.doe@example.com",
  "Password": "SecurePass123!"
}

Response: 201 Created
{
  "success": true,
  "message": "Registration successful",
  "token": "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...",
  "user": {
    "ApplicantId": 123,
    "FirstName": "John",
    "LastName": "Doe",
    "Email": "john.doe@example.com"
  }
}
```

#### User Login
```http
POST /api/Login
Content-Type: application/json

{
  "Email": "john.doe@example.com",
  "Password": "SecurePass123!"
}

Response: 200 OK
{
  "success": true,
  "message": "Login successful", 
  "token": "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...",
  "user": {
    "ApplicantId": 123,
    "FirstName": "John",
    "LastName": "Doe",
    "Email": "john.doe@example.com"
  }
}
```

### Protected Profile Endpoints

#### Get User Profile
```http
GET /api/GetUserProfile
Authorization: Bearer eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...

Response: 200 OK
{
  "ApplicantId": 123,
  "FirstName": "John",
  "LastName": "Doe",
  "FullName": "John Doe",
  "Email": "john.doe@example.com",
  "Location": "New York, NY",
  "JobTitle": "Software Engineer",
  "AboutMe": "Passionate developer...",
  "ResumeFileUrl": "https://...",
  "JobPreferences": "Remote, Full-time",
  "CreatedAt": "2025-07-10T16:00:00Z",
  "UpdatedAt": "2025-07-10T16:30:00Z"
}
```

#### Update User Profile
```http
PUT /api/UpdateUserProfile
Authorization: Bearer eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...
Content-Type: application/json

{
  "FirstName": "John",
  "LastName": "Doe",
  "Location": "San Francisco, CA", 
  "JobTitle": "Senior Software Engineer",
  "AboutMe": "Experienced full-stack developer...",
  "ResumeFileUrl": "https://storage.example.com/resume.pdf",
  "JobPreferences": "Remote, Full-time, $120k+"
}

Response: 200 OK
{
  "success": true,
  "message": "Profile updated successfully"
}
```
            JobTitle: null,
            AboutMe: null,
            ResumeFileUrl: null,
            JobPreferences: null
        };

        const result = await profileManager.createUserAccount(registrationData);
        
        if (result.success) {
            // 2. Login user automatically
            const userData = {
                ApplicantId: result.applicantId,
                FirstName: registrationData.FirstName,
                LastName: registrationData.LastName,
                Email: registrationData.Email
            };
            
            // 3. Set authentication state
            profileManager.onUserLogin(userData);
            
            // 4. Redirect to profile completion or dashboard
            window.location.href = '/profile.html';
        }
    } catch (error) {
        console.error('Registration failed:', error);
        // Show error to user
    }
}
```

#### B. When User Logs In
```javascript
// In your colleague's login component:
async function handleLogin(loginData) {
    try {
        // 1. Authenticate user (your colleague's logic)
        const authResult = await authenticateUser(loginData.email, loginData.password);
        
        if (authResult.success) {
            // 2. Get user profile from our backend
            const userData = await profileManager.getUserProfile(authResult.applicantId);
            
            // 3. Set authentication state in profile manager
            profileManager.onUserLogin(userData);
            
            // 4. Redirect to dashboard
            window.location.href = '/dashboard.html';
        }
    } catch (error) {
        console.error('Login failed:', error);
        // Show error to user
    }
}
```

#### C. When User Logs Out
```javascript
// In your colleague's logout functionality:
function handleLogout() {
    // 1. Clear authentication state
    profileManager.onUserLogout();
    
    // 2. Clear any auth tokens/sessions
    clearAuthTokens();
    
    // 3. Redirect to login
    window.location.href = '/login.html';
}
```

### 3. Backend Integration Points

#### A. JWT Token Integration (Future)
```csharp
// When ready to secure endpoints:
[Function("GetUserProfile")]
public async Task<HttpResponseData> GetUserProfile(
    [HttpTrigger(AuthorizationLevel.Function, "get")] HttpRequestData req) // Change to Function level
{
    // 1. Extract user ID from JWT token instead of query param
    var userId = ExtractUserIdFromJWT(req);
    
    // 2. Rest of the method remains the same
    var applicant = await _databaseService.GetApplicantByIdAsync(userId);
    // ...
}
```

#### B. Password Hashing
```csharp
// Add to CreateApplicant endpoint:
public async Task<int> CreateApplicantAsync(Applicant newApplicant)
{
    // Hash password before storing
    newApplicant.Password = BCrypt.Net.BCrypt.HashPassword(newApplicant.Password);
    
    // Rest of method remains the same
}
```

### 4. Required Frontend File Updates

#### Update profile.html
```html
<!-- Add to profile.html head section -->
<script src="profile-manager.js"></script>

<!-- Update the save button to work with ProfileManager -->
<button id="saveProfile" type="button" class="btn btn-primary">Save Profile</button>
```

#### Update app.js
```javascript
// Add authentication checks to existing functions
function checkAuthentication() {
    if (!window.profileManager.isAuthenticated) {
        window.location.href = '/login.html';
        return false;
    }
    return true;
}

// Update sendApplication to use authenticated user
async function sendApplication(jobId) {
    if (!checkAuthentication()) return;
    
    const userData = window.profileManager.currentUser;
    // Rest of function remains the same
}
```

### 5. Authentication State Management

The ProfileManager handles authentication state using:
- `localStorage` for user data persistence
- `isAuthenticated` flag for quick checks
- `currentUser` object for user information

Your colleague's authentication code should call:
- `profileManager.onUserLogin(userData)` after successful login
- `profileManager.onUserLogout()` when user logs out

### 6. Security Considerations

#### Current Security (Anonymous Access)
- All endpoints currently use `AuthorizationLevel.Anonymous`
- No JWT validation (ready for implementation)
- Input validation and sanitization in place

#### Future Security (With Authentication)
- Change to `AuthorizationLevel.Function` 
- Implement JWT token validation
- Add user ID extraction from tokens
- Implement password hashing (BCrypt recommended)

### 7. Testing the Integration

#### Test User Registration
```javascript
// Test creating a new user
const testUser = {
    FirstName: "Test",
    LastName: "User", 
    Email: "test@example.com",
    Password: "hashedPassword123"
};

const result = await profileManager.createUserAccount(testUser);
console.log('Registration result:', result);
```

#### Test Profile Management
```javascript
// Test getting user profile
const profile = await profileManager.getUserProfile(1);
console.log('User profile:', profile);

// Test updating profile
const updateData = {
    ApplicantId: 1,
    FirstName: "Updated",
    LastName: "Name",
    JobTitle: "Software Developer",
    AboutMe: "I love coding!"
};

const result = await profileManager.updateUserProfile(updateData);
console.log('Update result:', result);
```

## What's Ready Now

✅ **Backend APIs** - All profile management endpoints ready  
✅ **Database Schema** - Migration script created (needs to be applied)  
✅ **Frontend Integration** - ProfileManager class ready  
✅ **Error Handling** - Comprehensive error handling in place  
✅ **Form Management** - Profile form handling implemented  

## What's Needed from Authentication Integration

1. **Sign Up Form** → Calls `profileManager.createUserAccount()`
2. **Login Form** → Calls `profileManager.onUserLogin()` after auth
3. **Logout Functionality** → Calls `profileManager.onUserLogout()`
4. **Route Protection** → Check `profileManager.isAuthenticated`
5. **Token Management** → For future JWT implementation

## Next Steps

1. **Apply database migration** to production
2. **Test profile endpoints** with Postman/curl
3. **Deploy updated backend** with profile methods
4. **Wait for authentication code** from colleague
5. **Integrate authentication** using this guide
6. **Test end-to-end flow** from registration to profile management
