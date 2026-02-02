using PQC.MODULES.Documents.Application.Interfaces.PasswordHaser.PQC.INFRAESTRUCTURE.Security.Hashing.Interfaces;
using BCryptLib = BCrypt.Net.BCrypt;

namespace PQC.INFRAESTRUCTURE.Security.Hashing
{
    public class BcryptPasswordHasher : IPasswordHasher
    {
        private const int WorkFactor = 12; // ajuste conforme segurança desejada

        public string HashPassword(string password)
        {
            if (string.IsNullOrWhiteSpace(password))
                throw new ArgumentException("A senha não pode estar vazia", nameof(password));

            return BCryptLib.HashPassword(password, WorkFactor);
        }

        public bool VerifyPassword(string password, string hash)
        {
            if (string.IsNullOrWhiteSpace(password))
                return false;

            if (string.IsNullOrWhiteSpace(hash))
                return false;

            try
            {
                return BCryptLib.Verify(password, hash);
            }
            catch
            {
                // Se o hash estiver corrompido ou inválido
                return false;
            }
        }

        public bool NeedsRehash(string hash)
        {
            if (string.IsNullOrWhiteSpace(hash))
                return true;

            try
            {
                return BCryptLib.PasswordNeedsRehash(hash, WorkFactor);
            }
            catch
            {
                return true;
            }
        }
    }
}