using System.Security.Cryptography;

namespace CryptoSoft;

/// <summary>
/// AES-256-CBC file decryptor.
/// Reads files produced by <see cref="Encryptor"/>:
///   [salt 16 bytes] [IV 16 bytes] [cipher data …]
/// </summary>
public static class Decryptor
{
    /// <summary>
    /// Decrypts a single file that was encrypted with <see cref="Encryptor.EncryptFile"/>.
    /// </summary>
    /// <param name="sourceFilePath">Path to the encrypted file.</param>
    /// <param name="destinationFilePath">Path where the decrypted file will be written.</param>
    /// <param name="key">Secret key / password (must match the one used for encryption).</param>
    /// <returns>Decryption time in milliseconds.</returns>
    public static double DecryptFile(string sourceFilePath, string destinationFilePath, string key)
    {
        if (!File.Exists(sourceFilePath))
            throw new FileNotFoundException("Encrypted source file not found.", sourceFilePath);

        // Ensure destination directory exists
        string? dir = Path.GetDirectoryName(destinationFilePath);
        if (dir != null && !Directory.Exists(dir))
            Directory.CreateDirectory(dir);

        double elapsed = 0;

        elapsed = Utils.MeasureExecutionTime(() =>
        {
            using var fsIn = new FileStream(sourceFilePath, FileMode.Open, FileAccess.Read);

            // Read salt (16 bytes) and IV (16 bytes) from header
            byte[] salt = new byte[16];
            byte[] iv = new byte[16];
            fsIn.ReadExactly(salt, 0, salt.Length);
            fsIn.ReadExactly(iv, 0, iv.Length);

            byte[] derivedKey = Utils.DeriveKey(key, salt);

            using var aes = Aes.Create();
            aes.KeySize = 256;
            aes.Mode = CipherMode.CBC;
            aes.Padding = PaddingMode.PKCS7;
            aes.Key = derivedKey;
            aes.IV = iv;

            using var decryptor = aes.CreateDecryptor();
            using var cs = new CryptoStream(fsIn, decryptor, CryptoStreamMode.Read);
            using var fsOut = new FileStream(destinationFilePath, FileMode.Create, FileAccess.Write);
            cs.CopyTo(fsOut);
        });

        return elapsed;
    }

    /// <summary>
    /// Decrypts all files in a directory (recursively) in-place.
    /// Only files whose extension is in <paramref name="extensions"/> are decrypted.
    /// </summary>
    /// <param name="directoryPath">Root directory to scan.</param>
    /// <param name="key">Secret key / password.</param>
    /// <param name="extensions">Comma-separated extensions (e.g. ".txt,.docx,.pdf").</param>
    /// <returns>Total decryption time in milliseconds and count of decrypted files.</returns>
    public static (double totalTimeMs, int fileCount) DecryptDirectory(string directoryPath, string key, string extensions)
    {
        if (!Directory.Exists(directoryPath))
            throw new DirectoryNotFoundException($"Directory not found: {directoryPath}");

        var allowed = extensions
            .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Select(e => e.StartsWith('.') ? e : "." + e)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        double totalTime = 0;
        int count = 0;

        foreach (string filePath in Directory.EnumerateFiles(directoryPath, "*", SearchOption.AllDirectories))
        {
            string ext = Path.GetExtension(filePath);
            if (!allowed.Contains(ext))
                continue;

            string tempFile = filePath + ".dec.tmp";
            try
            {
                double elapsed = DecryptFile(filePath, tempFile, key);
                File.Delete(filePath);
                File.Move(tempFile, filePath);
                totalTime += elapsed;
                count++;
                Console.WriteLine($"  Decrypted: {filePath} ({elapsed:F2} ms)");
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"  Failed: {filePath} — {ex.Message}");
                if (File.Exists(tempFile))
                    File.Delete(tempFile);
            }
        }

        return (totalTime, count);
    }
}
