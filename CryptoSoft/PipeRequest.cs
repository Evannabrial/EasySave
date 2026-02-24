namespace CryptoSoft;

// Model for a request sent by EasySave to the CryptoSoft server
public class PipeRequest
{
    public string Action { get; set; } = "";      // "encrypt" or "decrypt"
    public string Source { get; set; } = "";       // File or directory path to process
    public string Key { get; set; } = "";          // Encryption/decryption key (Base64)
    public string? Extensions { get; set; }        // Optional: comma-separated list of file extensions to filter
}
