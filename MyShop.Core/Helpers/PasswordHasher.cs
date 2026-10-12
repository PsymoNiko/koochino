using System;
using System.Security.Cryptography;

namespace MyShop.Core.Helpers
{
    public static class PasswordHasher
    {
        private const int SaltSize = 16;
        private const int KeySize = 32;
        private const int Iterations = 100_000;

        public static (string hash, string salt) HashPassword(string password)
        {
            byte[] saltBytes = new byte[SaltSize];
            RandomNumberGenerator.Fill(saltBytes);

            string salt = Convert.ToBase64String(saltBytes);

            using var pbkdf2 = new Rfc2898DeriveBytes(
                password,
                saltBytes,
                Iterations,
                HashAlgorithmName.SHA256);

            string hash = Convert.ToBase64String(pbkdf2.GetBytes(KeySize));

            return (hash, salt);
        }

        public static bool Verify(string password, string storedHash, string storedSalt)
            => VerifyPassword(password, storedHash, storedSalt);

        public static bool VerifyPassword(string password, string storedHash, string storedSalt)
        {
            if (string.IsNullOrEmpty(password) ||
                string.IsNullOrEmpty(storedHash) ||
                string.IsNullOrEmpty(storedSalt))
                return false;

            byte[] saltBytes;
            byte[] expected;
            try
            {
                saltBytes = Convert.FromBase64String(storedSalt);
                expected = Convert.FromBase64String(storedHash);
            }
            catch
            {
                return false;
            }

            using var pbkdf2 = new Rfc2898DeriveBytes(
                password,
                saltBytes,
                Iterations,
                HashAlgorithmName.SHA256);

            byte[] actual = pbkdf2.GetBytes(KeySize);

            return actual.Length == expected.Length &&
                   CryptographicOperations.FixedTimeEquals(actual, expected);
        }
    }
}
