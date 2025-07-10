using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.IdentityModel.Tokens;
using BCrypt.Net;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration;
using CoverLetterFunction.Models;

namespace CoverLetterFunction.Services
{
    public interface IAuthenticationService
    {
        Task<AuthResult> RegisterAsync(RegisterRequest request);
        Task<AuthResult> LoginAsync(LoginRequest request);
        Task<bool> ValidateTokenAsync(string token);
        int? ExtractUserIdFromToken(string token);
    }

    public class AuthenticationService : IAuthenticationService
    {
        private readonly IDatabaseService _databaseService;
        private readonly ILogger<AuthenticationService> _logger;
        private readonly string _jwtSecret;
        private readonly string _jwtIssuer;

        public AuthenticationService(
            IDatabaseService databaseService,
            ILogger<AuthenticationService> logger,
            IConfiguration configuration)
        {
            _databaseService = databaseService;
            _logger = logger;
            _jwtSecret = configuration["JwtSecret"] ?? throw new ArgumentNullException("JwtSecret");
            _jwtIssuer = configuration["JwtIssuer"] ?? "JobPortalAPI";
        }

        public async Task<AuthResult> RegisterAsync(RegisterRequest request)
        {
            try
            {
                // Check if email already exists
                var existingUser = await _databaseService.GetApplicantByEmailAsync(request.Email);
                if (existingUser != null)
                {
                    return new AuthResult { Success = false, Message = "Email already registered" };
                }

                // Validate password strength
                if (!IsPasswordStrong(request.Password))
                {
                    return new AuthResult { Success = false, Message = "Password must be at least 8 characters with uppercase, lowercase, number, and special character" };
                }

                // Hash password
                var hashedPassword = BCrypt.Net.BCrypt.HashPassword(request.Password, 12);

                // Create new user
                var newUser = new Applicant
                {
                    FirstName = request.FirstName,
                    LastName = request.LastName,
                    Email = request.Email.ToLower().Trim(),
                    Password = hashedPassword
                };

                var userId = await _databaseService.CreateApplicantAsync(newUser);
                
                if (userId > 0)
                {
                    // Generate JWT token
                    var token = GenerateJwtToken(userId, request.Email, request.FirstName, request.LastName);
                    
                    return new AuthResult
                    {
                        Success = true,
                        Token = token,
                        User = new UserInfo
                        {
                            ApplicantId = userId,
                            FirstName = request.FirstName,
                            LastName = request.LastName,
                            Email = request.Email
                        }
                    };
                }

                return new AuthResult { Success = false, Message = "Failed to create account" };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during registration for email {Email}", request.Email);
                return new AuthResult { Success = false, Message = "Registration failed" };
            }
        }

        public async Task<AuthResult> LoginAsync(LoginRequest request)
        {
            try
            {
                // Get user by email
                var user = await _databaseService.GetApplicantByEmailAsync(request.Email.ToLower().Trim());
                if (user == null)
                {
                    return new AuthResult { Success = false, Message = "Invalid credentials" };
                }

                // Verify password
                if (!BCrypt.Net.BCrypt.Verify(request.Password, user.Password))
                {
                    return new AuthResult { Success = false, Message = "Invalid credentials" };
                }

                // Generate JWT token
                var token = GenerateJwtToken(user.ApplicantId, user.Email, user.FirstName, user.LastName);

                return new AuthResult
                {
                    Success = true,
                    Token = token,
                    User = new UserInfo
                    {
                        ApplicantId = user.ApplicantId,
                        FirstName = user.FirstName,
                        LastName = user.LastName,
                        Email = user.Email
                    }
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during login for email {Email}", request.Email);
                return new AuthResult { Success = false, Message = "Login failed" };
            }
        }

        public Task<bool> ValidateTokenAsync(string token)
        {
            try
            {
                var tokenHandler = new JwtSecurityTokenHandler();
                var key = Encoding.ASCII.GetBytes(_jwtSecret);

                tokenHandler.ValidateToken(token, new TokenValidationParameters
                {
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = new SymmetricSecurityKey(key),
                    ValidateIssuer = true,
                    ValidIssuer = _jwtIssuer,
                    ValidateAudience = false,
                    ClockSkew = TimeSpan.Zero
                }, out SecurityToken validatedToken);

                return Task.FromResult(true);
            }
            catch
            {
                return Task.FromResult(false);
            }
        }

        public int? ExtractUserIdFromToken(string token)
        {
            try
            {
                var tokenHandler = new JwtSecurityTokenHandler();
                var key = Encoding.ASCII.GetBytes(_jwtSecret);

                var principal = tokenHandler.ValidateToken(token, new TokenValidationParameters
                {
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = new SymmetricSecurityKey(key),
                    ValidateIssuer = true,
                    ValidIssuer = _jwtIssuer,
                    ValidateAudience = false,
                    ClockSkew = TimeSpan.Zero
                }, out SecurityToken validatedToken);

                var userIdClaim = principal.FindFirst("userId");
                if (userIdClaim != null && int.TryParse(userIdClaim.Value, out int userId))
                {
                    return userId;
                }

                return null;
            }
            catch
            {
                return null;
            }
        }

        private string GenerateJwtToken(int userId, string email, string firstName, string lastName)
        {
            var tokenHandler = new JwtSecurityTokenHandler();
            var key = Encoding.ASCII.GetBytes(_jwtSecret);

            var tokenDescriptor = new SecurityTokenDescriptor
            {
                Subject = new ClaimsIdentity(new[]
                {
                    new Claim("userId", userId.ToString()),
                    new Claim("email", email),
                    new Claim("firstName", firstName),
                    new Claim("lastName", lastName),
                    new Claim(ClaimTypes.Name, $"{firstName} {lastName}")
                }),
                Expires = DateTime.UtcNow.AddDays(7), // 7 day expiration
                Issuer = _jwtIssuer,
                SigningCredentials = new SigningCredentials(
                    new SymmetricSecurityKey(key),
                    SecurityAlgorithms.HmacSha256Signature)
            };

            var token = tokenHandler.CreateToken(tokenDescriptor);
            return tokenHandler.WriteToken(token);
        }

        private static bool IsPasswordStrong(string password)
        {
            if (string.IsNullOrEmpty(password) || password.Length < 8)
                return false;

            bool hasUpper = password.Any(char.IsUpper);
            bool hasLower = password.Any(char.IsLower);
            bool hasDigit = password.Any(char.IsDigit);
            bool hasSpecial = password.Any(ch => !char.IsLetterOrDigit(ch));

            return hasUpper && hasLower && hasDigit && hasSpecial;
        }
    }

    // Data models
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

    public class AuthResult
    {
        public bool Success { get; set; }
        public string Message { get; set; } = "";
        public string Token { get; set; } = "";
        public UserInfo? User { get; set; }
    }

    public class UserInfo
    {
        public int ApplicantId { get; set; }
        public string FirstName { get; set; } = "";
        public string LastName { get; set; } = "";
        public string Email { get; set; } = "";
    }
}
