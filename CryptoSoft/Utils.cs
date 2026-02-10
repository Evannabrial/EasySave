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
    /// The IV ensures that encrypting the same plaintext twice produces different ciphertexts.
    /// </summary>
    /// <returns>A 16-byte random IV.</returns>
    public static byte[] GenerateIv()
    {
        // AES block size is 128 bits = 16 bytes, so the IV must be 16 bytes
        byte[] iv = new byte[16];
        // Use a cryptographically secure random number generator
        using var rng = RandomNumberGenerator.Create();
        rng.GetBytes(iv); // Fill the array with random bytes
        return iv;
    }

    /// <summary>
    /// Generates a random 16-byte salt for key derivation.
    /// The salt prevents dictionary/rainbow-table attacks on the password.
    /// </summary>
    /// <returns>A 16-byte random salt.</returns>
    public static byte[] GenerateSalt()
    {
        byte[] salt = new byte[16];
        using var rng = RandomNumberGenerator.Create();
        rng.GetBytes(salt); // Fill with cryptographically secure random bytes
        return salt;
    }

    /// <summary>
    /// Derives a 256-bit AES key from a password and salt using PBKDF2.
    /// PBKDF2 applies SHA-256 hashing 100,000 times to make brute-force attacks slow.
    /// </summary>
    /// <param name="password">The user-supplied password / secret key.</param>
    /// <param name="salt">The salt bytes (unique per encryption).</param>
    /// <returns>A 32-byte derived key suitable for AES-256.</returns>
    public static byte[] DeriveKey(string password, byte[] salt)
    {
        // Pbkdf2 static method: password + salt + iterations + hash algo + output length in bytes
        // 32 bytes = 256 bits for AES-256
        return Rfc2898DeriveBytes.Pbkdf2(password, salt, 100_000, HashAlgorithmName.SHA256, 32);
    }

    /// <summary>
    /// Measures the execution time of an action and returns its duration in milliseconds.
    /// Used to report encryption/decryption performance.
    /// </summary>
    /// <param name="action">The action to time.</param>
    /// <returns>Elapsed time in milliseconds.</returns>
    public static double MeasureExecutionTime(Action action)
    {
        Stopwatch sw = Stopwatch.StartNew(); // Start high-resolution timer
        action();                            // Execute the operation
        sw.Stop();                           // Stop the timer
        return sw.Elapsed.TotalMilliseconds; // Return duration in ms
    }
}
