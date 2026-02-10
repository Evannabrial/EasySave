using System.Security.Cryptography;

namespace CryptoSoft;

/// <summary>
/// AES-256-CBC file encryptor.
/// Writes the output file with the following binary layout:
///   [salt 16 bytes] [IV 16 bytes] [cipher data …]
/// </summary>
public static class Encryptor
{
    /// <summary>
    /// Encrypts a single file using AES-256-CBC.
    /// </summary>
    /// <param name="sourceFilePath">Path to the plain-text file.</param>
    /// <param name="destinationFilePath">Path where the encrypted file will be written.</param>
    /// <param name="key">Secret key / password used to derive the AES key.</param>
    /// <returns>Encryption time in milliseconds.</returns>
    public static double EncryptFile(string sourceFilePath, string destinationFilePath, string key)
    {
        if (!File.Exists(sourceFilePath))
            throw new FileNotFoundException("Source file not found.", sourceFilePath);

        // Ensure destination directory exists
        string? dir = Path.GetDirectoryName(destinationFilePath);
        if (dir != null && !Directory.Exists(dir))
            Directory.CreateDirectory(dir);

        double elapsed = 0;

        elapsed = Utils.MeasureExecutionTime(() =>
        {
            byte[] salt = Utils.GenerateSalt();
            byte[] iv = Utils.GenerateIv();
            byte[] derivedKey = Utils.DeriveKey(key, salt);

            using var aes = Aes.Create();
            aes.KeySize = 256;
            aes.Mode = CipherMode.CBC;
            aes.Padding = PaddingMode.PKCS7;
            aes.Key = derivedKey;
            aes.IV = iv;

            using var fsOut = new FileStream(destinationFilePath, FileMode.Create, FileAccess.Write);
            // Write salt + IV header
            fsOut.Write(salt, 0, salt.Length);
            fsOut.Write(iv, 0, iv.Length);

            using var encryptor = aes.CreateEncryptor();
            using var cs = new CryptoStream(fsOut, encryptor, CryptoStreamMode.Write);
            using var fsIn = new FileStream(sourceFilePath, FileMode.Open, FileAccess.Read);
            fsIn.CopyTo(cs);
        });

        return elapsed;
    }

    /// <summary>
    /// Encrypts all matching files in a directory (recursively) in-place.
    /// Only files whose extension is in <paramref name="extensions"/> are encrypted.
    /// </summary>
    /// <param name="directoryPath">Root directory to scan.</param>
    /// <param name="key">Secret key / password.</param>
    /// <param name="extensions">Comma-separated extensions (e.g. ".txt,.docx,.pdf").</param>
    /// <returns>Total encryption time in milliseconds and count of encrypted files.</returns>
    public static (double totalTimeMs, int fileCount) EncryptDirectory(string directoryPath, string key, string extensions)
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

            string tempFile = filePath + ".enc.tmp";
            try
            {
                double elapsed = EncryptFile(filePath, tempFile, key);
                File.Delete(filePath);
                File.Move(tempFile, filePath);
                totalTime += elapsed;
                count++;
                Console.WriteLine($"  Encrypted: {filePath} ({elapsed:F2} ms)");
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
