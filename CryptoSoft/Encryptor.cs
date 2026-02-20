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
        // Validate that the source file exists before proceeding
        if (!File.Exists(sourceFilePath))
            throw new FileNotFoundException("Source file not found.", sourceFilePath);

        // Create the destination directory if it doesn't exist yet
        string? dir = Path.GetDirectoryName(destinationFilePath);
        if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
            Directory.CreateDirectory(dir);

        double elapsed = 0;

        // Measure the total encryption time (key derivation + encryption)
        elapsed = Utils.MeasureExecutionTime(() =>
        {
            // Generate a unique salt and IV for this encryption
            // Each file gets its own salt+IV so the same plaintext produces different ciphertext
            byte[] salt = Utils.GenerateSalt();
            byte[] iv = Utils.GenerateIv();

            // Derive a 256-bit key from the user password + salt
            byte[] derivedKey = Utils.DeriveKey(key, salt);

            // Configure AES-256 in CBC mode with PKCS7 padding
            using var aes = Aes.Create();
            aes.KeySize = 256;             // 256-bit key length
            aes.Mode = CipherMode.CBC;     // Cipher Block Chaining mode
            aes.Padding = PaddingMode.PKCS7; // Standard padding for block ciphers
            aes.Key = derivedKey;
            aes.IV = iv;

            // Open the output file for writing
            using var fsOut = new FileStream(destinationFilePath, FileMode.Create, FileAccess.Write);

            // Write the salt (16 bytes) and IV (16 bytes) as a header
            // These are needed for decryption and are NOT secret
            fsOut.Write(salt, 0, salt.Length);
            fsOut.Write(iv, 0, iv.Length);

            // Create the AES encryptor transform and wrap the output in a CryptoStream
            using var encryptor = aes.CreateEncryptor();
            using var cs = new CryptoStream(fsOut, encryptor, CryptoStreamMode.Write);

            // Read the source file and write encrypted data to the CryptoStream
            using var fsIn = new FileStream(sourceFilePath, FileMode.Open, FileAccess.Read);
            fsIn.CopyTo(cs); // Encrypts data as it streams through
        });

        return elapsed; // Return encryption time in milliseconds
    }

    /// <summary>
    /// Encrypts all matching files in a directory (recursively) in-place.
    /// If extensions is specified, only files with those extensions are encrypted.
    /// If extensions is null or empty, all files are encrypted.
    /// </summary>
    /// <param name="directoryPath">Root directory to scan.</param>
    /// <param name="key">Secret key / password.</param>
    /// <param name="extensions">Optional: Comma-separated extensions (e.g. ".txt,.docx,.pdf"). If null, encrypts all files.</param>
    /// <returns>Total encryption time in milliseconds and count of encrypted files.</returns>
    public static (double totalTimeMs, int fileCount) EncryptDirectory(string directoryPath, string key, string? extensions)
    {
        // Check that the directory exists
        if (!Directory.Exists(directoryPath))
            throw new DirectoryNotFoundException($"Directory not found: {directoryPath}");

        // Parse the comma-separated extensions into a HashSet for fast lookup
        // If extensions is null or empty, allow all files
        // Normalize each extension to start with '.' (e.g. "txt" -> ".txt")
        // Case-insensitive comparison (e.g. ".PDF" matches ".pdf")
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
        // we delete originals and create .enc files during the loop, which
        // mutates the directory and causes the lazy enumerator to skip files.
        string[] files = Directory.GetFiles(directoryPath, "*", SearchOption.AllDirectories);
        foreach (string filePath in files)
        {
            // If extensions filter is specified, skip files that don't match
            if (allowed.Count > 0)
            {
                string ext = Path.GetExtension(filePath);
                if (!allowed.Contains(ext))
                    continue;
            }

            // Use a temporary file for encryption output to avoid corrupting the original
            string tempFile = filePath + ".enc.tmp";
            string encryptedFile = filePath + ".enc";
            try
            {
                // Encrypt the file to a temp file
                double elapsed = EncryptFile(filePath, tempFile, key);

                // Rename temp to .enc format
                File.Move(tempFile, encryptedFile, overwrite: true);
                
                // Delete the original plaintext file
                File.Delete(filePath);

                totalTime += elapsed;
                count++;
                Console.WriteLine($"  Encrypted: {filePath} → {encryptedFile} ({elapsed:F2} ms)");
            }
            catch (Exception ex)
            {
                // On error, clean up the temp file and report the failure
                Console.Error.WriteLine($"  Failed: {filePath} — {ex.Message}");
                if (File.Exists(tempFile))
                    File.Delete(tempFile);
            }
        }

        return (totalTime, count); // Return total time and number of files encrypted
    }
}
