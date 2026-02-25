using System.IO.Pipes;
using System.Threading;
using CryptoSoft;

// Entry point: CryptoSoft always runs as a Named Pipe server.
// It is started by EasySave and listens for encryption/decryption requests.
bool isNewInstance;
using (Mutex mutex = new Mutex(true, "Global\\CryptoSoft_Mutex", out isNewInstance))
{
    if (!isNewInstance)
    {
        // An instance of CryptoSoft is already running.
        return 1;
    }

    RunServer();
    return 0;
}

// Executes an encrypt or decrypt command on a file or directory.
// Returns a PipeResponse with the result (exit code, output, error).
// This method is called by the server for each client request.
static PipeResponse ExecuteCommand(string action, string source, string key, string? extensions)
{
    // StringWriters to capture output and error messages
    var output = new StringWriter();
    var error = new StringWriter();
    int exitCode = 0;

    action = action.ToLowerInvariant();

    // Validate the action (must be "encrypt" or "decrypt")
    if (action != "encrypt" && action != "decrypt")
    {
        error.WriteLine($"Unknown action: {action}");
        return new PipeResponse { ExitCode = 1, Output = output.ToString(), Error = error.ToString() };
    }

    try
    {
        if (Directory.Exists(source))
        {
            // Source is a directory: encrypt or decrypt all files inside it
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
            // Source is a single file
            if (action == "encrypt")
            {
                // If an extension filter is provided, check if the file matches
                if (extensions != null)
                {
                    string ext = Path.GetExtension(source);
                    var allowed = extensions
                        .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                        .Select(e => e.StartsWith('.') ? e : "." + e)
                        .ToHashSet(StringComparer.OrdinalIgnoreCase);

                    // Skip file if extension is not in the allowed list
                    if (!allowed.Contains(ext))
                    {
                        error.WriteLine($"Extension '{ext}' is not in the allowed list. File skipped.");
                        return new PipeResponse { ExitCode = 2, Output = output.ToString(), Error = error.ToString() };
                    }
                }

                // Encrypt the file and replace the original with the .enc version
                string encryptedFile = source + ".enc";
                double ms = Encryptor.EncryptFile(source, encryptedFile, key);
                File.Delete(source);
                output.WriteLine($"Encryption completed in {ms:F2} ms");
                output.WriteLine(ms.ToString("F2"));
            }
            else
            {
                // Decrypt the file: remove .enc extension for the output name
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
            // Source path does not exist
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

// Named Pipe server.
// Runs an infinite loop that:
// 1. Creates a pipe and waits for a client (EasySave) to connect
// 2. Reads the encryption/decryption request from the client
// 3. Spawns a new thread to handle the request (allows parallel processing)
// 4. Goes back to step 1 to accept the next client
// The server runs forever until EasySave kills it on shutdown.
static void RunServer()
{
    while (true)
    {
        // Create a new Named Pipe instance and wait for a client to connect
        var pipe = new NamedPipeServerStream(
            PipeProtocol.PipeName,
            PipeDirection.InOut,
            NamedPipeServerStream.MaxAllowedServerInstances);

        pipe.WaitForConnection();

        // Read the request sent by the client
        var request = PipeProtocol.Receive<PipeRequest>(pipe);
        if (request == null)
        {
            pipe.Dispose();
            continue;
        }

        // Process the request in a separate thread for parallel execution.
        // We capture the pipe reference so each thread uses its own pipe.
        var currentPipe = pipe;
        new Thread(() =>
        {
            try
            {
                // Execute the encrypt/decrypt command and send the result back
                var response = ExecuteCommand(request.Action, request.Source, request.Key, request.Extensions);
                PipeProtocol.Send(currentPipe, response);
            }
            catch { }
            finally
            {
                // Always close the pipe after the response is sent
                currentPipe.Dispose();
            }
        }).Start();
    }
}