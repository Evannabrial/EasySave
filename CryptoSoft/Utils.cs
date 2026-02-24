using System.Diagnostics;
using System.Security.Cryptography;
using System.Text;

namespace CryptoSoft;

// Utility class for cryptographic operations: IV generation, key derivation from salt, and execution timing.
public static class Utils
{
    // Generates a random 16-byte Initialization Vector (IV) for AES encryption.
    // The IV ensures that encrypting the same plaintext twice produces different ciphertexts.
    // Returns: A 16-byte random IV.
    public static byte[] GenerateIv()
    {
        // AES block size is 128 bits = 16 bytes, so the IV must be 16 bytes
        byte[] iv = new byte[16];
        // Use a cryptographically secure random number generator
        using var rng = RandomNumberGenerator.Create();
        rng.GetBytes(iv); // Fill the array with random bytes
        return iv;
    }

    // Generates a random 16-byte salt for key derivation.
    // The salt prevents dictionary/rainbow-table attacks on the password.
    // Returns: A 16-byte random salt.
    public static byte[] GenerateSalt()
    {
        byte[] salt = new byte[16];
        using var rng = RandomNumberGenerator.Create();
        rng.GetBytes(salt); // Fill with cryptographically secure random bytes
        return salt;
    }

    // Derives a 256-bit AES key from a password and salt using PBKDF2.
    // PBKDF2 applies SHA-256 hashing 100,000 times to make brute-force attacks slow.
    // Params:
    //   password: The user-supplied password / secret key.
    //   salt: The salt bytes (unique per encryption).
    // Returns: A 32-byte derived key suitable for AES-256.
    public static byte[] DeriveKey(string password, byte[] salt)
    {
        // Pbkdf2 static method: password + salt + iterations + hash algo + output length in bytes
        // 32 bytes = 256 bits for AES-256
        return Rfc2898DeriveBytes.Pbkdf2(password, salt, 100_000, HashAlgorithmName.SHA256, 32);
    }

    // Measures the execution time of an action and returns its duration in milliseconds.
    // Used to report encryption/decryption performance.
    // Params:
    //   action: The action to time.
    // Returns: Elapsed time in milliseconds.
    public static double MeasureExecutionTime(Action action)
    {
        Stopwatch sw = Stopwatch.StartNew(); // Start high-resolution timer
        action();                            // Execute the operation
        sw.Stop();                           // Stop the timer
        return sw.Elapsed.TotalMilliseconds; // Return duration in ms
    }
}
