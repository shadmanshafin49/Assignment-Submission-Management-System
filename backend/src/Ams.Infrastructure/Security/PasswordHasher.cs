using System.Security.Cryptography;
using Ams.Application.Abstractions;
using Microsoft.AspNetCore.Cryptography.KeyDerivation;

namespace Ams.Infrastructure.Security;

/// <summary>
/// PBKDF2-HMAC-SHA256 password hashing. Each password gets a fresh 128-bit salt, and the
/// iteration count is stored alongside the hash so it can be raised later without
/// invalidating existing credentials.
/// </summary>
public class PasswordHasher : IPasswordHasher
{
    private const int SaltSize = 16;      // 128-bit
    private const int SubkeySize = 32;    // 256-bit
    private const int Iterations = 210_000;

    public string Hash(string password)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(password);

        var salt = RandomNumberGenerator.GetBytes(SaltSize);
        var subkey = KeyDerivation.Pbkdf2(
            password, salt, KeyDerivationPrf.HMACSHA256, Iterations, SubkeySize);

        return $"{Iterations}.{Convert.ToBase64String(salt)}.{Convert.ToBase64String(subkey)}";
    }

    public bool Verify(string password, string passwordHash)
    {
        if (string.IsNullOrWhiteSpace(password) || string.IsNullOrWhiteSpace(passwordHash))
            return false;

        var parts = passwordHash.Split('.', 3);
        if (parts.Length != 3 || !int.TryParse(parts[0], out var iterations))
            return false;

        byte[] salt, expected;
        try
        {
            salt = Convert.FromBase64String(parts[1]);
            expected = Convert.FromBase64String(parts[2]);
        }
        catch (FormatException)
        {
            return false;
        }

        var actual = KeyDerivation.Pbkdf2(
            password, salt, KeyDerivationPrf.HMACSHA256, iterations, expected.Length);

        // Fixed-time comparison — a naive == would leak how much of the hash matched.
        return CryptographicOperations.FixedTimeEquals(actual, expected);
    }
}
