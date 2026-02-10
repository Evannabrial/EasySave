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

// Validate minimum number of arguments (action + at least 2 params)
if (args.Length < 3)
{
    PrintUsage();
    return 1; // Exit code 1 = invalid arguments
}

// Parse the action (first argument), case-insensitive
string action = args[0].ToLowerInvariant();

// ===== DIRECTORY MODE =====
// Handle encrypt-dir / decrypt-dir: encrypts/decrypts all matching files in a folder recursively
if (action == "encrypt-dir" || action == "decrypt-dir")
{
    // Directory mode requires: action + directory + key + extensions = 4 args
    if (args.Length < 4)
    {
        PrintUsage();
        return 1;
    }

    string dir = args[1];            // Path to the directory
    string dirKey = args[2];         // Encryption/decryption password
    string dirExtensions = args[3];  // Comma-separated extensions (e.g. ".txt,.pdf")

    try
    {
        if (action == "encrypt-dir")
        {
            // Encrypt all matching files in the directory, returns (total time, file count)
            var (totalMs, count) = Encryptor.EncryptDirectory(dir, dirKey, dirExtensions);
            Console.WriteLine($"\nEncrypted {count} file(s) in {totalMs:F2} ms total");
            Console.WriteLine(totalMs.ToString("F2")); // Machine-readable time on last line
        }
        else
        {
            // Decrypt all matching files in the directory
            var (totalMs, count) = Decryptor.DecryptDirectory(dir, dirKey, dirExtensions);
            Console.WriteLine($"\nDecrypted {count} file(s) in {totalMs:F2} ms total");
            Console.WriteLine(totalMs.ToString("F2"));
        }
        return 0; // Exit code 0 = success
    }
    catch (Exception ex)
    {
        Console.Error.WriteLine($"Error: {ex.Message}");
        return 3; // Exit code 3 = runtime error
    }
}

// ===== SINGLE FILE MODE =====
// For encrypt/decrypt a single file: action + source + destination + key = 4 args minimum
if (args.Length < 4)
{
    PrintUsage();
    return 1;
}

string source = args[1];       // Path to the input file
string destination = args[2];  // Path to the output file
string key = args[3];          // Encryption/decryption password

// Optional 5th argument: comma-separated list of allowed extensions
// If provided, only files with matching extensions will be encrypted
string? allowedExtensions = args.Length >= 5 ? args[4] : null;

try
{
    switch (action)
    {
        case "encrypt":
            // If an extension filter was provided, check if the file extension is allowed
            if (allowedExtensions != null)
            {
                // Get the file extension (e.g. ".txt")
                string ext = Path.GetExtension(source);

                // Build a HashSet of allowed extensions for fast case-insensitive lookup
                var allowed = allowedExtensions
                    .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                    .Select(e => e.StartsWith('.') ? e : "." + e)
                    .ToHashSet(StringComparer.OrdinalIgnoreCase);

                // If the file extension is not in the allowed list, skip it
                if (!allowed.Contains(ext))
                {
                    Console.Error.WriteLine($"Extension '{ext}' is not in the allowed list. File skipped.");
                    return 2; // Exit code 2 = extension not allowed
                }
            }

            // Encrypt the file and display the elapsed time
            double encryptTime = Encryptor.EncryptFile(source, destination, key);
            Console.WriteLine($"Encryption completed in {encryptTime:F2} ms");
            Console.WriteLine(encryptTime.ToString("F2")); // Machine-readable line for EasySave to parse
            return 0;

        case "decrypt":
            // Decrypt the file and display the elapsed time
            double decryptTime = Decryptor.DecryptFile(source, destination, key);
            Console.WriteLine($"Decryption completed in {decryptTime:F2} ms");
            Console.WriteLine(decryptTime.ToString("F2")); // Machine-readable line for EasySave to parse
            return 0;

        default:
            // Unknown action provided
            Console.Error.WriteLine($"Unknown action: {action}");
            PrintUsage();
            return 1;
    }
}
catch (Exception ex)
{
    // Catch any runtime error (file not found, wrong password, etc.)
    Console.Error.WriteLine($"Error: {ex.Message}");
    return 3; // Exit code 3 = runtime error
}

/// <summary>
/// Displays the usage instructions for the CryptoSoft CLI tool.
/// </summary>
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