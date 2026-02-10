using System.Diagnostics;
using System.Security.Cryptography;
using System.Text;

namespace CryptoSoft;

/// <summary>
/// Utility class for cryptographic operations: IV generation, key derivation from salt, and execution timing.
/// </summary>
public static class Utils
{
    /// <summary>
    /// Generates a random 16-byte Initialization Vector (IV) for AES encryption.
    /// </summary>
    /// <returns>A 16-byte random IV.</returns>
    public static byte[] GenerateIv()
    {
        byte[] iv = new byte[16];
        using var rng = RandomNumberGenerator.Create();
        rng.GetBytes(iv);
        return iv;
    }

    /// <summary>
    /// Generates a random 16-byte salt for key derivation.
    /// </summary>
    /// <returns>A 16-byte random salt.</returns>
    public static byte[] GenerateSalt()
    {
        byte[] salt = new byte[16];
        using var rng = RandomNumberGenerator.Create();
        rng.GetBytes(salt);
        return salt;
    }

    /// <summary>
    /// Derives a 256-bit AES key from a password and salt using PBKDF2 (SHA256, 100 000 iterations).
    /// </summary>
    /// <param name="password">The user-supplied password / secret key.</param>
    /// <param name="salt">The salt bytes.</param>
    /// <returns>A 32-byte derived key.</returns>
    public static byte[] DeriveKey(string password, byte[] salt)
    {
        return Rfc2898DeriveBytes.Pbkdf2(password, salt, 100_000, HashAlgorithmName.SHA256, 32); // AES-256
    }

    /// <summary>
    /// Measures the execution time of an action and returns its duration in milliseconds.
    /// </summary>
    /// <param name="action">The action to time.</param>
    /// <returns>Elapsed time in milliseconds.</returns>
    public static double MeasureExecutionTime(Action action)
    {
        Stopwatch sw = Stopwatch.StartNew();
        action();
        sw.Stop();
        return sw.Elapsed.TotalMilliseconds;
    }
}
