using System.Diagnostics;

namespace EasySaveLibrary;

public class CryptoManager
{
    private string _cryptoSoftPath;
    private string _encryptionKey;
    private string _extensionsToEncrypt;

    public string CryptoSoftPath
    {
        get => _cryptoSoftPath;
        set => _cryptoSoftPath = value ?? throw new ArgumentNullException(nameof(value));
    }

    public string EncryptionKey
    {
        get => _encryptionKey;
        set => _encryptionKey = value ?? throw new ArgumentNullException(nameof(value));
    }

    public string ExtensionsToEncrypt
    {
        get => _extensionsToEncrypt;
        set => _extensionsToEncrypt = value ?? throw new ArgumentNullException(nameof(value));
    }

    public CryptoManager(string cryptoSoftPath, string encryptionKey, string extensionsToEncrypt = ".txt,.docx,.pdf")
    {
        CryptoSoftPath = cryptoSoftPath;
        EncryptionKey = encryptionKey;
        ExtensionsToEncrypt = extensionsToEncrypt;
    }

    /// <summary>
    /// Encrypts a single file using CryptoSoft
    /// </summary>
    /// <param name="sourceFile">Source file path</param>
    /// <param name="destinationFile">Destination file path</param>
    /// <returns>
    /// 0 => OK
    /// 1 => CryptoSoft not found
    /// 2 => Encryption failed
    /// 3 => Extension not allowed
    /// </returns>
    public (int status, double execTime) EncryptFile(string sourceFile, string destinationFile)
    {
        if (!File.Exists(CryptoSoftPath))
        {
            return (1, 0);
        }

        try
        {
            ProcessStartInfo startInfo = new ProcessStartInfo
            {
                FileName = CryptoSoftPath,
                Arguments = $"encrypt \"{sourceFile}\" \"{destinationFile}\" \"{EncryptionKey}\" \"{ExtensionsToEncrypt}\"",
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            using Process process = new Process { StartInfo = startInfo };
            process.Start();

            string output = process.StandardOutput.ReadToEnd();
            string error = process.StandardError.ReadToEnd();
            process.WaitForExit();

            if (process.ExitCode == 2) // Extension not allowed
            {
                return (3, 0);
            }

            if (process.ExitCode != 0)
            {
                return (2, 0);
            }

            // Parse execution time from last line
            string[] lines = output.Split(Environment.NewLine, StringSplitOptions.RemoveEmptyEntries);
            if (lines.Length > 0 && double.TryParse(lines[^1], out double execTime))
            {
                return (0, execTime);
            }

            return (0, 0);
        }
        catch (Exception)
        {
            return (2, 0);
        }
    }

    /// <summary>
    /// Checks if a file extension should be encrypted
    /// </summary>
    /// <param name="filePath">File path to check</param>
    /// <returns>True if the file should be encrypted</returns>
    public bool ShouldEncrypt(string filePath)
    {
        string ext = Path.GetExtension(filePath);
        var allowed = ExtensionsToEncrypt
            .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Select(e => e.StartsWith('.') ? e : "." + e)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        return allowed.Contains(ext);
    }
}
