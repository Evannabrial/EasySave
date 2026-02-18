using System.IO.Pipes;
using CryptoSoft;

// ===========================================
// ENTRY POINT: Server mode or CLI mode
// ===========================================

// Server mode: CryptoSoft runs as a Named Pipe server (mono-instance)
// EasySave starts this with: CryptoSoft.exe --server
if (args.Length > 0 && args[0] == "--server")
{
    RunServer();
    return 0;
}

// CLI mode: normal command-line usage (kept for backward compatibility)
if (args.Length < 3)
{
    PrintUsage();
    return 1;
}

var result = ExecuteCommand(args[0], args[1], args[2], args.Length >= 4 ? args[3] : null);
Console.Write(result.Output);
if (!string.IsNullOrEmpty(result.Error))
    Console.Error.Write(result.Error);
return result.ExitCode;

// ===========================================
// CORE LOGIC: Runs an encrypt/decrypt command
// ===========================================

/// <summary>
/// Executes an encryption or decryption command.
/// Used by both CLI mode and the server to handle requests.
/// </summary>
static PipeResponse ExecuteCommand(string action, string source, string key, string? extensions)
{
    var output = new StringWriter();
    var error = new StringWriter();
    int exitCode = 0;

    action = action.ToLowerInvariant();

    if (action != "encrypt" && action != "decrypt")
    {
        error.WriteLine($"Unknown action: {action}");
        return new PipeResponse { ExitCode = 1, Output = output.ToString(), Error = error.ToString() };
    }

    try
    {
        if (Directory.Exists(source))
        {
            // Directory mode
            if (action == "encrypt")
            {
                var (totalMs, count) = Encryptor.EncryptDirectory(source, key, extensions);
                output.WriteLine($"\nEncrypted {count} file(s) in {totalMs:F2} ms total");
                output.WriteLine(totalMs.ToString("F2"));
            }
            else
            {
                var (totalMs, count) = Decryptor.DecryptDirectory(source, key, extensions);
                output.WriteLine($"\nDecrypted {count} file(s) in {totalMs:F2} ms total");
                output.WriteLine(totalMs.ToString("F2"));
            }
        }
        else if (File.Exists(source))
        {
            // Single file mode
            if (action == "encrypt")
            {
                // Check extension filter
                if (extensions != null)
                {
                    string ext = Path.GetExtension(source);
                    var allowed = extensions
                        .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                        .Select(e => e.StartsWith('.') ? e : "." + e)
                        .ToHashSet(StringComparer.OrdinalIgnoreCase);

                    if (!allowed.Contains(ext))
                    {
                        error.WriteLine($"Extension '{ext}' is not in the allowed list. File skipped.");
                        return new PipeResponse { ExitCode = 2, Output = output.ToString(), Error = error.ToString() };
                    }
                }

                string encryptedFile = source + ".enc";
                double ms = Encryptor.EncryptFile(source, encryptedFile, key);
                File.Delete(source);
                output.WriteLine($"Encryption completed in {ms:F2} ms");
                output.WriteLine(ms.ToString("F2"));
            }
            else
            {
                string decryptedFile = source.EndsWith(".enc")
                    ? source.Substring(0, source.Length - 4)
                    : source;

                double ms = Decryptor.DecryptFile(source, decryptedFile, key);
                File.Delete(source);
                output.WriteLine($"Decryption completed in {ms:F2} ms");
                output.WriteLine(ms.ToString("F2"));
            }
        }
        else
        {
            error.WriteLine($"Path not found: {source}");
            exitCode = 1;
        }
    }
    catch (Exception ex)
    {
        error.WriteLine($"Error: {ex.Message}");
        exitCode = 3;
    }

    return new PipeResponse { ExitCode = exitCode, Output = output.ToString(), Error = error.ToString() };
}

// ===========================================
// SERVER: Named Pipe with one thread per client
// ===========================================

/// <summary>
/// Runs the Named Pipe server.
/// Each client connection spawns a new thread for parallel processing.
/// The server stops after 5 seconds of no new connections.
/// </summary>
static void RunServer()
{
    Console.WriteLine("CryptoSoft server started.");

    while (true)
    {
        // Create a new pipe instance for the next client
        var pipe = new NamedPipeServerStream(
            PipeProtocol.PipeName,
            PipeDirection.InOut,
            NamedPipeServerStream.MaxAllowedServerInstances);

        // Wait for a client with timeout (auto-shutdown if idle)
        var cts = new CancellationTokenSource(PipeProtocol.ServerTimeoutMs);
        try
        {
            pipe.WaitForConnectionAsync(cts.Token).Wait();
        }
        catch
        {
            // Timeout: no client connected, shut down
            pipe.Dispose();
            Console.WriteLine("Server idle, shutting down.");
            break;
        }

        // Read the request from the client
        var request = PipeProtocol.Receive<PipeRequest>(pipe);
        if (request == null)
        {
            pipe.Dispose();
            continue;
        }

        // Handle in a new thread (parallel encryption)
        var currentPipe = pipe;
        new Thread(() =>
        {
            try
            {
                Console.WriteLine($"[Thread {Thread.CurrentThread.ManagedThreadId}] {request.Action} {request.Source}");

                // Run the encrypt/decrypt command
                var response = ExecuteCommand(request.Action, request.Source, request.Key, request.Extensions);

                // Send result back to client
                PipeProtocol.Send(currentPipe, response);

                Console.WriteLine($"[Thread {Thread.CurrentThread.ManagedThreadId}] Done (exit={response.ExitCode})");
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"[Thread {Thread.CurrentThread.ManagedThreadId}] Error: {ex.Message}");
            }
            finally
            {
                currentPipe.Dispose();
            }
        }).Start();
    }
}

// ===========================================
// USAGE
// ===========================================

static void PrintUsage()
{
    Console.WriteLine("CryptoSoft – AES-256 File Encryption Tool");
    Console.WriteLine();
    Console.WriteLine("  CLI:    CryptoSoft encrypt <source> <key> [extensions]");
    Console.WriteLine("          CryptoSoft decrypt <source> <key> [extensions]");
    Console.WriteLine("  Server: CryptoSoft --server");
}