using System.Text.Json;

namespace CryptoSoft;

// Helper class for Named Pipe communication between EasySave (client) and CryptoSoft (server).
// Provides methods to serialize and deserialize JSON messages through a pipe stream.
public static class PipeProtocol
{
    // Name of the Named Pipe, used by both client and server to find each other
    public const string PipeName = "CryptoSoftPipe";

    // Maximum time (in ms) the client waits to connect to the server
    public const int ClientTimeoutMs = 10000;

    // Serializes a message to JSON and writes it to the pipe stream
    public static void Send<T>(Stream stream, T message)
    {
        string json = JsonSerializer.Serialize(message);
        var writer = new StreamWriter(stream, leaveOpen: true);
        writer.WriteLine(json); // Write JSON as a single line
        writer.Flush();
    }

    // Reads a single line from the pipe stream and deserializes it from JSON
    public static T? Receive<T>(Stream stream)
    {
        var reader = new StreamReader(stream, leaveOpen: true);
        string? line = reader.ReadLine();
        if (string.IsNullOrEmpty(line)) return default;
        return JsonSerializer.Deserialize<T>(line);
    }
}

// Model for a request sent by EasySave to the CryptoSoft server
public class PipeRequest
{
    public string Action { get; set; } = "";      // "encrypt" or "decrypt"
    public string Source { get; set; } = "";       // File or directory path to process
    public string Key { get; set; } = "";          // Encryption/decryption key (Base64)
    public string? Extensions { get; set; }        // Optional: comma-separated list of file extensions to filter
}

// Model for a response sent by the CryptoSoft server back to EasySave
public class PipeResponse
{
    public int ExitCode { get; set; }              // 0 = success, non-zero = error
    public string Output { get; set; } = "";       // Standard output (e.g. encryption time)
    public string Error { get; set; } = "";        // Error message if something went wrong
}
