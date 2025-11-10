using Clbio.Shared.Results;
using Microsoft.AspNetCore.Cryptography.KeyDerivation;
using System.Security.Cryptography;

namespace Clbio.Application.Extensions
{
    public static class PasswordManager
    {
        private const string Algorithm = "pbkdf2-hmac-sha256";
        private const int Iterations = 100_000;
        private const int SaltSize = 16;
        private const int KeySize = 32;

        public static Result<string> HashPassword(string password)
        {
            try
            {
                //generate 128 bit salt
                byte[] salt = RandomNumberGenerator.GetBytes(SaltSize);
                byte[] hash = KeyDerivation.Pbkdf2(
                    password: password,
                    salt: salt,
                    prf: KeyDerivationPrf.HMACSHA256,
                    iterationCount: Iterations,
                    numBytesRequested: KeySize
                );
                return Result<string>.Ok($"{Algorithm}${Iterations}${Convert.ToBase64String(salt)}${Convert.ToBase64String(hash)}");
            }
            catch (Exception)
            {
                return Result<string>.Fail("Password hashing failed.");
            }
        }

        public static Result<bool> VerifyPassword(string password, string encodedHash)
        {
            try
            {
                var parts = encodedHash.Split('$');
                if (parts.Length != 4)
                    throw new FormatException("Invalid Hash format");

                var algorithm = parts[0];
                bool intParseSuccess = int.TryParse(parts[1], out int iterations);
                var salt = Convert.FromBase64String(parts[2]);
                var hash = Convert.FromBase64String(parts[3]);

                if (algorithm != Algorithm)
                    throw new NotSupportedException($"Unsupported Algorithm: {algorithm}");

                var hashToCompare = KeyDerivation.Pbkdf2(
                    password: password,
                    salt: salt,
                    prf: KeyDerivationPrf.HMACSHA256,
                    iterationCount: iterations,
                    numBytesRequested: hash.Length
                );

                return Result<bool>.Ok(CryptographicOperations.FixedTimeEquals(hash, hashToCompare));
            }
            catch (Exception)
            {
                return Result<bool>.Fail("Password verification failed");
            }

        }
    }
}
