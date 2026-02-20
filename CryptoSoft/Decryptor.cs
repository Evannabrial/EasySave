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
        // Validate that the encrypted source file exists
        if (!File.Exists(sourceFilePath))
            throw new FileNotFoundException("Encrypted source file not found.", sourceFilePath);

        // Create the destination directory if it doesn't exist yet
        string? dir = Path.GetDirectoryName(destinationFilePath);
        if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
            Directory.CreateDirectory(dir);

        double elapsed = 0;

        // Measure the total decryption time
        elapsed = Utils.MeasureExecutionTime(() =>
        {
            // Open the encrypted file for reading
            using var fsIn = new FileStream(sourceFilePath, FileMode.Open, FileAccess.Read);

            // Read the salt (first 16 bytes) and IV (next 16 bytes) from the file header
            // These were written by the Encryptor and are needed to reconstruct the key
            byte[] salt = new byte[16];
            byte[] iv = new byte[16];
            fsIn.ReadExactly(salt, 0, salt.Length);  // Read salt
            fsIn.ReadExactly(iv, 0, iv.Length);      // Read IV

            // Derive the same AES key using the password + the salt from the file
            // This will only produce the correct key if the password matches
            byte[] derivedKey = Utils.DeriveKey(key, salt);

            // Configure AES with the same settings used during encryption
            using var aes = Aes.Create();
            aes.KeySize = 256;
            aes.Mode = CipherMode.CBC;
            aes.Padding = PaddingMode.PKCS7;
            aes.Key = derivedKey;
            aes.IV = iv;

            // Create the AES decryptor and wrap the input stream in a CryptoStream
            using var decryptor = aes.CreateDecryptor();
            using var cs = new CryptoStream(fsIn, decryptor, CryptoStreamMode.Read);

            // Write the decrypted data to the output file
            using var fsOut = new FileStream(destinationFilePath, FileMode.Create, FileAccess.Write);
            cs.CopyTo(fsOut); // Decrypts data as it streams through
        });

        return elapsed; // Return decryption time in milliseconds
    }

    /// <summary>
    /// Decrypts all files in a directory (recursively) in-place.
    /// Only files matching extensions (if specified) are decrypted.
    /// </summary>
    /// <param name="directoryPath">Root directory to scan.</param>
    /// <param name="key">Secret key / password.</param>
    /// <param name="extensions">Optional: comma-separated extensions filter (e.g. ".txt,.docx").</param>
    /// <returns>Total decryption time in milliseconds and count of decrypted files.</returns>
    public static (double totalTimeMs, int fileCount) DecryptDirectory(string directoryPath, string key, string? extensions = null)
    {
        // Check that the directory exists
        if (!Directory.Exists(directoryPath))
            throw new DirectoryNotFoundException($"Directory not found: {directoryPath}");

        // Parse the extensions filter (optional)
        HashSet<string> allowed = new();
        if (!string.IsNullOrEmpty(extensions))
        {
            allowed = extensions
                .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .Select(e => e.StartsWith('.') ? e : "." + e)
                .ToHashSet(StringComparer.OrdinalIgnoreCase);
        }

        double totalTime = 0;
        int count = 0;

        // Snapshot the file list BEFORE iterating.
        // Using GetFiles() (eager) instead of EnumerateFiles() (lazy) because
        // we delete .enc files and create originals during the loop, which
        // mutates the directory and causes the lazy enumerator to skip files.
        string[] files = Directory.GetFiles(directoryPath, "*", SearchOption.AllDirectories);
        foreach (string filePath in files)
        {
            // Only decrypt .enc files
            if (!filePath.EndsWith(".enc"))
                continue;

            // Get the original filename (without .enc)
            string decryptedFile = filePath.Substring(0, filePath.Length - 4);

            // If extensions are specified, check if the file matches
            if (allowed.Count > 0)
            {
                string ext = Path.GetExtension(decryptedFile);
                if (!allowed.Contains(ext))
                    continue;
            }

            // Use a temporary file for decryption output to avoid corrupting the original
            string tempFile = filePath + ".dec.tmp";
            try
            {
                // Decrypt the file to the temp file
                double elapsed = DecryptFile(filePath, tempFile, key);

                // Rename temp to original name (without .enc)
                File.Move(tempFile, decryptedFile, overwrite: true);
                
                // Delete the encrypted .enc file
                File.Delete(filePath);

                totalTime += elapsed;
                count++;
            }
            catch (Exception ex)
            {
                // On error, clean up the temp file
                if (File.Exists(tempFile))
                    File.Delete(tempFile);
            }
        }

        return (totalTime, count); // Return total time and number of files decrypted
    }
}
