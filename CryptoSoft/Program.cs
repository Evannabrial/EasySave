using CryptoSoft;

/// <summary>
/// CryptoSoft – Console Application
/// 
/// Usage:
///   CryptoSoft encrypt &lt;sourceFile&gt; &lt;destFile&gt; &lt;key&gt; [extensions]
///   CryptoSoft decrypt &lt;sourceFile&gt; &lt;destFile&gt; &lt;key&gt;
///   CryptoSoft encrypt-dir &lt;directory&gt; &lt;key&gt; &lt;extensions&gt;
///   CryptoSoft decrypt-dir &lt;directory&gt; &lt;key&gt; &lt;extensions&gt;
///
/// The optional [extensions] argument is a comma-separated list of file
/// extensions (e.g. ".txt,.docx,.pdf"). When provided, only files whose
/// extension matches the list will be encrypted. If the source file's
/// extension is not in the list the program exits with code 2.
///
/// Exit codes:
///   0 – Success
///   1 – Invalid arguments / usage error
///   2 – File extension not in allowed list (skipped)
///   3 – Runtime error
/// 
/// File	Role
///Utils.cs	IV & salt generation (RandomNumberGenerator), key derivation (Pbkdf2 SHA-256 100k iterations), execution time measurement
///Encryptor.cs	AES-256-CBC encryption — writes [salt 16B][IV 16B][ciphertext] to the output file
///Decryptor.cs	AES-256-CBC decryption — reads the salt+IV header then decrypts
///Program.cs	CLI entry point with extension filtering
/// </summary>

if (args.Length < 3)
{
    PrintUsage();
    return 1;
}

string action = args[0].ToLowerInvariant();

// Handle directory modes first (encrypt-dir / decrypt-dir) — requires 3 args
if (action == "encrypt-dir" || action == "decrypt-dir")
{
    if (args.Length < 4)
    {
        PrintUsage();
        return 1;
    }

    string dir = args[1];
    string dirKey = args[2];
    string dirExtensions = args[3];

    try
    {
        if (action == "encrypt-dir")
        {
            var (totalMs, count) = Encryptor.EncryptDirectory(dir, dirKey, dirExtensions);
            Console.WriteLine($"\nEncrypted {count} file(s) in {totalMs:F2} ms total");
            Console.WriteLine(totalMs.ToString("F2"));
        }
        else
        {
            var (totalMs, count) = Decryptor.DecryptDirectory(dir, dirKey, dirExtensions);
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

// File mode — requires at least 4 args
if (args.Length < 4)
{
    PrintUsage();
    return 1;
}

string source = args[1];
string destination = args[2];
string key = args[3];

// Optional comma-separated list of extensions to encrypt (e.g. ".txt,.docx,.pdf")
string? allowedExtensions = args.Length >= 5 ? args[4] : null;

try
{
    switch (action)
    {
        case "encrypt":
            // Check extension filter
            if (allowedExtensions != null)
            {
                string ext = Path.GetExtension(source); // e.g. ".txt"
                var allowed = allowedExtensions
                    .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                    .Select(e => e.StartsWith('.') ? e : "." + e)
                    .ToHashSet(StringComparer.OrdinalIgnoreCase);

                if (!allowed.Contains(ext))
                {
                    Console.Error.WriteLine($"Extension '{ext}' is not in the allowed list. File skipped.");
                    return 2;
                }
            }

            double encryptTime = Encryptor.EncryptFile(source, destination, key);
            Console.WriteLine($"Encryption completed in {encryptTime:F2} ms");
            Console.WriteLine(encryptTime.ToString("F2")); // machine-readable line
            return 0;

        case "decrypt":
            double decryptTime = Decryptor.DecryptFile(source, destination, key);
            Console.WriteLine($"Decryption completed in {decryptTime:F2} ms");
            Console.WriteLine(decryptTime.ToString("F2"));
            return 0;

        default:
            Console.Error.WriteLine($"Unknown action: {action}");
            PrintUsage();
            return 1;
    }
}
catch (Exception ex)
{
    Console.Error.WriteLine($"Error: {ex.Message}");
    return 3;
}

static void PrintUsage()
{
    Console.WriteLine("CryptoSoft – AES-256 File Encryption Tool");
    Console.WriteLine();
    Console.WriteLine("Usage:");
    Console.WriteLine("  CryptoSoft encrypt     <source> <destination> <key> [extensions]");
    Console.WriteLine("  CryptoSoft decrypt     <source> <destination> <key>");
    Console.WriteLine("  CryptoSoft encrypt-dir <directory> <key> <extensions>");
    Console.WriteLine("  CryptoSoft decrypt-dir <directory> <key> <extensions>");
    Console.WriteLine();
    Console.WriteLine("Arguments:");
    Console.WriteLine("  <source>       Path to the input file");
    Console.WriteLine("  <destination>  Path to the output file");
    Console.WriteLine("  <directory>    Path to the directory to encrypt/decrypt recursively");
    Console.WriteLine("  <key>          Secret password for encryption / decryption");
    Console.WriteLine("  <extensions>   Comma-separated extensions, e.g. .txt,.docx,.pdf");
}