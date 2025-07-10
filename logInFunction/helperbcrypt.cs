using BCrypt.Net;

namespace jobsapp.Helperbcrypt
{
    public static class BcryptHelper
    {
    
        public static string HashPassword(string password)
        {
            return BCrypt.Net.BCrypt.HashPassword(password);
        }


        public static bool VerifyPassword(string password, string hash)
        {
            return BCrypt.Net.BCrypt.Verify(password, hash);
        }
    }
}