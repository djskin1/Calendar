using System.Security.Cryptography;

namespace Calendar.Services
{
    public static class PasswordSecurity
    {
        private const int DefaultIterations = 210000;

        public static PasswordHashResult HashPassword(
            string password)
        {
            byte[] salt =
                RandomNumberGenerator.GetBytes(16);

            byte[] hash =
                Rfc2898DeriveBytes.Pbkdf2(
                    password,
                    salt,
                    DefaultIterations,
                    HashAlgorithmName.SHA256,
                    32);

            return new PasswordHashResult
            {
                Hash = Convert.ToBase64String(hash),
                Salt = Convert.ToBase64String(salt),
                Iterations = DefaultIterations
            };
        }

        public static bool VerifyPassword(
            string password,
            string storedHash,
            string storedSalt,
            int iterations)
        {
            byte[] salt =
                Convert.FromBase64String(storedSalt);

            byte[] expectedHash =
                Convert.FromBase64String(storedHash);

            byte[] actualHash =
                Rfc2898DeriveBytes.Pbkdf2(
                    password,
                    salt,
                    iterations,
                    HashAlgorithmName.SHA256,
                    32);

            return CryptographicOperations.FixedTimeEquals(
                actualHash,
                expectedHash);
        }
    }

    public class PasswordHashResult
    {
        public string Hash { get; set; } = "";

        public string Salt { get; set; } = "";

        public int Iterations { get; set; }
    }
}