using CryptoSoft;

// Minimum arguments: action + source + key
if (args.Length < 3)
{
    PrintUsage();
    return 1;
}

string action = args[0].ToLowerInvariant();
string source = args[1];
string key = args[2];
string? extensions = args.Length >= 4 ? args[3] : null;

if (action != "encrypt" && action != "decrypt")
{
    Console.Error.WriteLine($"Unknown action: {action}");
    PrintUsage();
    return 1;
}

// AUTOMATIC DETECTION: FILE OR DIRECTORY
if (Directory.Exists(source))
{
    // DIRECTORY MODE
    try
    {
        if (action == "encrypt")
        {
            var (totalMs, count) = Encryptor.EncryptDirectory(source, key, extensions);
            Console.WriteLine($"\nEncrypted {count} file(s) in {totalMs:F2} ms total");
            Console.WriteLine(totalMs.ToString("F2"));
        }
        else
        {
            var (totalMs, count) = Decryptor.DecryptDirectory(source, key, extensions);
            Console.WriteLine($"\nDecrypted {count} file(s) in {totalMs:F2} ms total");
            Console.WriteLine(totalMs.ToString("F2"));
        }
        return 0;
    }
    catch (Exception ex)
    {
        Console.Error.WriteLine($"Error: {ex.Message}");
        return 3;
    }
}
else if (File.Exists(source))
{
    // SINGLE FILE MODE
    try
    {
        if (action == "encrypt")
        {
            // Check extension filter if provided
            if (extensions != null)
            {
                string ext = Path.GetExtension(source);
                var allowed = extensions
                    .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                    .Select(e => e.StartsWith('.') ? e : "." + e)
                    .ToHashSet(StringComparer.OrdinalIgnoreCase);

                if (!allowed.Contains(ext))
                {
                    Console.Error.WriteLine($"Extension '{ext}' is not in the allowed list. File skipped.");
                    return 2;
                }
            }

            // Encrypt to .enc file
            string encryptedFile = source + ".enc";
            double encryptTime = Encryptor.EncryptFile(source, encryptedFile, key);
            
            // Delete original file
            File.Delete(source);
            
            Console.WriteLine($"Encryption completed in {encryptTime:F2} ms");
            Console.WriteLine(encryptTime.ToString("F2"));
            return 0;
        }
        else
        {
            // For decryption, check if source is .enc file
            string decryptedFile = source;
            string sourceFile = source;
            
            if (source.EndsWith(".enc"))
            {
                // Remove .enc extension for output
                decryptedFile = source.Substring(0, source.Length - 4);
            }
            
            double decryptTime = Decryptor.DecryptFile(sourceFile, decryptedFile, key);
            
            // Delete encrypted file
            File.Delete(sourceFile);
            
            Console.WriteLine($"Decryption completed in {decryptTime:F2} ms");
            Console.WriteLine(decryptTime.ToString("F2"));
            return 0;
        }
    }
    catch (Exception ex)
    {
        Console.Error.WriteLine($"Error: {ex.Message}");
        return 3;
    }
}
else
{
    Console.Error.WriteLine($"Path not found: {source}");
    return 1;
}

/// <summary>
/// Displays the usage instructions for the CryptoSoft CLI tool.
/// </summary>
static void PrintUsage()
{
    Console.WriteLine("CryptoSoft – AES-256 File Encryption Tool");
    Console.WriteLine();
    Console.WriteLine("Unified usage (in-place modification):");
    Console.WriteLine();
    Console.WriteLine("  CryptoSoft encrypt <source> <key> [extensions]");
    Console.WriteLine("  CryptoSoft decrypt <source> <key> [extensions]");
    Console.WriteLine();
    Console.WriteLine("Arguments:");
    Console.WriteLine("  <source>       File or directory to encrypt/decrypt (modified in-place)");
    Console.WriteLine("  <key>          Secret password for encryption / decryption");
    Console.WriteLine("  [extensions]   Optional: comma-separated extensions (e.g., .txt,.docx)");
    Console.WriteLine("                 Required for directory encryption");
    Console.WriteLine("                 Optional for decryption");
}