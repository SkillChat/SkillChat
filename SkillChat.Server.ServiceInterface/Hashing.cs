using Microsoft.AspNetCore.Cryptography.KeyDerivation;
using System;
using System.Security.Cryptography;

namespace SkillChat.Server.ServiceInterface
{
    public static class Hashing
    {
        private const int SaltSizeBytes = 128 / 8;
        private const int Pbkdf2Iterations = 10000;
        private const int HashSizeBytes = 256 / 8;

        public static byte[] CreateSalt()
        {
            // generate a 128-bit salt using a secure PRNG
            byte[] salt = new byte[SaltSizeBytes];
            using (var rng = RandomNumberGenerator.Create())
            {
                rng.GetBytes(salt);
            }

            return salt;
        }

        public static string CreateHashPassword(string password, byte[] salt)
        {

            // derive a 256-bit subkey (use HMACSHA1 with 10,000 iterations)
            string hashedPassword = Convert.ToBase64String(KeyDerivation.Pbkdf2(
                password: password,
                salt: salt,
                prf: KeyDerivationPrf.HMACSHA1,
                iterationCount: Pbkdf2Iterations,
                numBytesRequested: HashSizeBytes));

            return hashedPassword;
        }

    }
}