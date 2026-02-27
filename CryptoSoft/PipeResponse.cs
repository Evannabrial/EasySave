namespace CryptoSoft;

// Model for a response sent by the CryptoSoft server back to EasySave
public class PipeResponse
{
    public int ExitCode { get; set; }              // 0 = success, non-zero = error
    public string Output { get; set; } = "";       // Standard output (e.g. encryption time)
    public string Error { get; set; } = "";        // Error message if something went wrong
}
