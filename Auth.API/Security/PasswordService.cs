using System.Security.Cryptography;

namespace Auth.API.Security
{
    // Security/PasswordService.cs
    namespace Auth.API.Security
    {
        public static class PasswordService
        {
            public static (string Hash, string Salt) Hash(string password)
            {
                var saltBytes = RandomNumberGenerator.GetBytes(32);
                var salt = Convert.ToBase64String(saltBytes);

                using var pbkdf2 = new Rfc2898DeriveBytes(
                    password,
                    saltBytes,
                    100_000,
                    HashAlgorithmName.SHA256);

                var hash = Convert.ToBase64String(pbkdf2.GetBytes(32));
                return (hash, salt);
            }

            public static bool Verify(string password, string storedHash, string storedSalt)
            {
                if (string.IsNullOrEmpty(storedHash) || string.IsNullOrEmpty(storedSalt))
                    return false;

                byte[] saltBytes;
                byte[] expectedHashBytes;
                try
                {
                    saltBytes = Convert.FromBase64String(storedSalt);
                    expectedHashBytes = Convert.FromBase64String(storedHash);
                }
                catch
                {
                    return false;
                }

                using var pbkdf2 = new Rfc2898DeriveBytes(
                    password,
                    saltBytes,
                    100_000,
                    HashAlgorithmName.SHA256);

                var computedHashBytes = pbkdf2.GetBytes(32);
                return CryptographicOperations.FixedTimeEquals(expectedHashBytes, computedHashBytes);
            }
        }
    }

}
