using Microsoft.Extensions.Logging;
using System;

namespace CoverLetterFunction.Services
{
    public static class SecurityHelper
    {
        private const int MAX_INPUT_LENGTH = 10000; // 10KB max input
        private const int MAX_JSON_DEPTH = 10;

        public static bool IsValidInput(string input, int maxLength = MAX_INPUT_LENGTH)
        {
            if (string.IsNullOrWhiteSpace(input))
                return false;

            if (input.Length > maxLength)
                return false;

            // Check for potential malicious patterns
            var suspiciousPatterns = new[]
            {
                "<script",
                "javascript:",
                "data:",
                "vbscript:",
                "onload=",
                "onerror=",
                "eval(",
                "setTimeout(",
                "setInterval("
            };

            var lowerInput = input.ToLowerInvariant();
            foreach (var pattern in suspiciousPatterns)
            {
                if (lowerInput.Contains(pattern))
                    return false;
            }

            return true;
        }

        public static string SanitizeLogMessage(string message, ILogger logger)
        {
            try
            {
                // Remove potential sensitive information
                var sanitized = message
                    .Replace("password", "[REDACTED]")
                    .Replace("key", "[REDACTED]")
                    .Replace("secret", "[REDACTED]")
                    .Replace("token", "[REDACTED]");

                // Limit length
                if (sanitized.Length > 500)
                {
                    sanitized = sanitized.Substring(0, 500) + "...";
                }

                return sanitized;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error sanitizing log message");
                return "[LOG_SANITIZATION_ERROR]";
            }
        }

        public static string GetGenericErrorMessage(string operation)
        {
            return $"An error occurred while {operation}. Please try again later or contact support if the issue persists.";
        }
    }
}
