using System.IO.Pipes;
using CryptoSoft;

// CryptoSoft runs as a Named Pipe server (started by EasySave)
RunServer();
return 0;

// Runs an encrypt or decrypt command and returns the result as a PipeResponse
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
            // Encrypt or decrypt all files in the directory
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
            // Encrypt or decrypt a single file
            if (action == "encrypt")
            {
                // Skip file if its extension is not in the allowed list
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
                // Remove .enc extension for the decrypted output file
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

// Named Pipe server: waits for client connections in a loop,
// each request is handled in a separate thread for parallel processing.
// The server runs forever until EasySave kills it on shutdown.
static void RunServer()
{
    while (true)
    {
        // Create a new pipe and wait for a client to connect
        var pipe = new NamedPipeServerStream(
            PipeProtocol.PipeName,
            PipeDirection.InOut,
            NamedPipeServerStream.MaxAllowedServerInstances);

        pipe.WaitForConnection();

        // Read the client's request
        var request = PipeProtocol.Receive<PipeRequest>(pipe);
        if (request == null)
        {
            pipe.Dispose();
            continue;
        }

        // Handle the request in a new thread so we can accept more clients
        var currentPipe = pipe;
        new Thread(() =>
        {
            try
            {
                var response = ExecuteCommand(request.Action, request.Source, request.Key, request.Extensions);
                PipeProtocol.Send(currentPipe, response);
            }
            catch { }
            finally
            {
                currentPipe.Dispose();
            }
        }).Start();
    }
}