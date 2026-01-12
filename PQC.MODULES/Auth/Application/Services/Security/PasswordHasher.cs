// PQC.MODULES/Users/Application/Services/Security/PasswordHasher.cs
using BCrypt.Net;

namespace PQC.MODULES.Auth.Application.Services.Security
{
    using BCryptLib = BCrypt.Net.BCrypt;
    public static class PasswordHasher
    {
        public static string HashPassword(string password)
        {

            return BCryptLib.HashPassword(password);
        }

        public static bool VerifyPassword(string password, string hash)
        {
            return BCryptLib.Verify(password, hash);
        }
    }
}