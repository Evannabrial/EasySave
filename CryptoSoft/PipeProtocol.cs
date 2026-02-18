using System.Text.Json;

namespace CryptoSoft;

// Shared constants and helpers for Named Pipe communication
public static class PipeProtocol
{
    // Name of the Named Pipe (like an address for local communication)
    public const string PipeName = "CryptoSoftPipe";

    // How long the server waits for a new client before shutting down (ms)
    public const int ServerTimeoutMs = 5000;

    // How long a client waits to connect to the server (ms)
    public const int ClientTimeoutMs = 10000;

    // Sends a JSON message through a stream (pipe)
    public static void Send<T>(Stream stream, T message)
    {
        string json = JsonSerializer.Serialize(message);
        var writer = new StreamWriter(stream, leaveOpen: true);
        writer.WriteLine(json);
        writer.Flush();
    }

    // Reads a JSON message from a stream (pipe)
    public static T? Receive<T>(Stream stream)
    {
        var reader = new StreamReader(stream, leaveOpen: true);
        string? line = reader.ReadLine();
        if (string.IsNullOrEmpty(line)) return default;
        return JsonSerializer.Deserialize<T>(line);
    }
}

// Request sent by EasySave to CryptoSoft server
public class PipeRequest
{
    public string Action { get; set; } = "";      // "encrypt" or "decrypt"
    public string Source { get; set; } = "";       // File or directory path
    public string Key { get; set; } = "";          // Encryption key
    public string? Extensions { get; set; }        // Optional extension filter
}

// Response sent by CryptoSoft server back to EasySave
public class PipeResponse
{
    public int ExitCode { get; set; }              // 0 = success
    public string Output { get; set; } = "";       // Standard output
    public string Error { get; set; } = "";        // Error output
}
