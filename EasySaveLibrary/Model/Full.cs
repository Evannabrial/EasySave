using System.Diagnostics;
using System.Security.Cryptography;
using System.Text.RegularExpressions;
using EasyLog;
using EasySaveLibrary.Interfaces;

namespace EasySaveLibrary.Model;

public class Full : ITypeSave
{
    public string DisplayName => "Full";
    private LogManager logManager;
    private bool hasLogBlockWritte;

    public Full()
    {
        logManager = new LogManager(
            baseLogPath: ConfigManager.LogPath,
            logDestination: ConfigManager.LogDestination,
            serverIp: ConfigManager.ServerIp,
            serverPort: ConfigManager.ServerPort
            );
    }

    /// <summary>
    /// Start the save of a job
    /// </summary>
    /// <param name="job"></param>
    /// <returns>
    /// 0 => OK
    /// 1 => Element source non trouvé
    /// 2 => Erreur copie du fichier
    /// 3 => Erreur création du dossier
    /// </returns>
    public int StartSave(Job job, LogType logType, ManualResetEvent pauseEvent, CancellationToken token,
        List<string> fileExtensions,
        string[] listBlockProcess, bool enableEncryption = false, string encryptionExtensions = "",
        string encryptionKey = "")
    {
        logManager = new LogManager(
            baseLogPath: ConfigManager.LogPath,
            logDestination: ConfigManager.LogDestination,
            serverIp: ConfigManager.ServerIp,
            serverPort: ConfigManager.ServerPort
        );
        logManager.TypeSave = logType;

        bool isFile = File.Exists(job.Source);
        bool isDirectory = Directory.Exists(job.Source);
        string target = job.Target + "\\" + job.Name + "-" + DateTime.Now.ToString("yyyyMMddHHmmss");
        List<string> blockedProcesses = listBlockProcess
            .Where(p => !string.IsNullOrWhiteSpace(p))
            .ToList();

        int totalPrioFiles = 0;
        if (isDirectory) {
            totalPrioFiles = DefinePriorityFiles(fileExtensions, job);
        } else {
            // Sécurité si fileExtensions est null
            if (fileExtensions != null && fileExtensions.Contains(Path.GetExtension(job.Source), StringComparer.OrdinalIgnoreCase)) {
                totalPrioFiles = 1;
                PriorityManager.AddPriorityFiles(1);
            }
        }

        Stopwatch startTimeDirStart = Stopwatch.StartNew();
        Directory.CreateDirectory(target);
        startTimeDirStart.Stop();
        logManager.WriteNewLog(
            name: job.Name,
            sourcePath: job.Source,
            targetPath: target,
            action: "Creation of a directory",
            execTime: startTimeDirStart.Elapsed.TotalMilliseconds
        );

        if (!isDirectory && !isFile)
        {
            return 1;
        }

        if (isFile)
        {
            try
            {
                string nameFile = Regex.Match(job.Source, @"[^\\]+$").Value;
                pauseEvent.WaitOne();
                while (IsBusinessSoftwareRunning(blockedProcesses))
                {
                    // Cancel if the user ask it
                    if (token.IsCancellationRequested) return -1;
                    if (!hasLogBlockWritte)
                    {
                        logManager.WriteNewLog(
                            name: job.Name,
                            sourcePath: job.Source,
                            targetPath: target,
                            action: "Program block the save",
                            state: "Blocked",
                            progress: 0,
                            nbFile: 0,
                            nbFileLeft: 0,
                            sizeFileLeft: 0
                        );
                    }

                    // Wait 1 second to not DDOS the app
                    Thread.Sleep(1000);
                }

                if (token.IsCancellationRequested)
                {
                    logManager.WriteNewLog(
                        name: job.Name,
                        sourcePath: job.Source,
                        targetPath: target + "\\" + nameFile,
                        action: "Cancel by user",
                        state: "Cancelled",
                        progress: 0,
                        nbFile: 0,
                        nbFileLeft: 0,
                        sizeFileLeft: 0
                    );
                    return -1;
                }

                logManager.WriteNewLog(
                    name: job.Name,
                    sourcePath: job.Source,
                    targetPath: target + "\\" + nameFile,
                    action: "Copy of a File",
                    state: "ON",
                    progress: 0,
                    nbFile: 1,
                    nbFileLeft: 1,
                    sizeFileLeft: new FileInfo(job.Source).Length
                );
                
                long fileSizeBytes = new FileInfo(job.Source).Length;
                long limitBytes = (long)(ConfigManager.FileSizeKo * 1024);
                if (limitBytes > 0 && fileSizeBytes > limitBytes)
                {
                    TransferManager.LargeFileSemaphore.Wait(token);
                    try
                    {
                        Stopwatch startTime = Stopwatch.StartNew();
                        File.Copy(job.Source, target + "\\" + nameFile);
                        startTime.Stop();
                        logManager.WriteNewLog(name: job.Name, sourcePath: job.Source, targetPath: target + "\\" + nameFile, action: "Copy of a File", execTime: startTime.Elapsed.TotalMilliseconds);
                    }
                    catch (OperationCanceledException) { return -1; }
                    finally { TransferManager.LargeFileSemaphore.Release(); }
                }
                else
                {
                    Stopwatch startTime = Stopwatch.StartNew();
                    File.Copy(job.Source, target + "\\" + nameFile);
                    startTime.Stop();
                    logManager.WriteNewLog(name: job.Name, sourcePath: job.Source, targetPath: target + "\\" + nameFile, action: "Copy of a File", execTime: startTime.Elapsed.TotalMilliseconds);
                }
                logManager.WriteNewLog(
                    name: job.Name,
                    sourcePath: job.Source,
                    targetPath: target + "\\" + nameFile,
                    action: "Copy of a File",
                    state: "OFF",
                    progress: 100,
                    nbFile: 1,
                    nbFileLeft: 0,
                    sizeFileLeft: 0
                );
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
            }

            return 0;
        }

        // We use the Breadth-first (parcours en largeur) search algorithm to visit all the folders and files in the source
        // See algorithm here https://en.wikipedia.org/wiki/Breadth-first_search
        Queue<string> queue = new Queue<string>();
        List<string> marked = new List<string>();

        // Vars for data and logs
        long size = Directory.EnumerateFiles(job.Source, "*", SearchOption.AllDirectories)
            .Sum(f => new FileInfo(f).Length);
        int nbFile = Directory.EnumerateFiles(job.Source, "*", SearchOption.AllDirectories)
            .Count();
        int nbFileManaged = 0;

        // The List of non-prio file we will copy at the end
        List<string> pendingNormalFiles = new List<string>();

        queue.Enqueue(job.Source);
        try
        {
            while (queue.Count > 0)
            {
                pauseEvent.WaitOne();
                while (IsBusinessSoftwareRunning(blockedProcesses))
                {
                    // Cancel if the user ask it
                    if (token.IsCancellationRequested) return -1;
                    if (!hasLogBlockWritte)
                    {
                        logManager.WriteNewLog(
                            name: job.Name,
                            sourcePath: job.Source,
                            targetPath: target,
                            action: "Program block the save",
                            state: "BLOCKED",
                            progress: (nbFileManaged / (double)nbFile) * 100,
                            nbFile: nbFile,
                            nbFileLeft: nbFile - nbFileManaged,
                            sizeFileLeft: size / (nbFileManaged + 1)
                        );
                    }

                    // Wait 1 second to not DDOS the app
                    Thread.Sleep(1000);
                }

                if (token.IsCancellationRequested)
                {
                    logManager.WriteNewLog(
                        name: job.Name,
                        sourcePath: job.Source,
                        targetPath: target,
                        action: "Cancel by user",
                        state: "Cancelled",
                        progress: 0,
                        nbFile: 0,
                        nbFileLeft: 0,
                        sizeFileLeft: 0
                    );
                    return -1;
                }

                string actual = queue.Dequeue();

                if (Directory.Exists(actual))
                {
                    foreach (string el in Directory.EnumerateFileSystemEntries(actual))
                    {
                        pauseEvent.WaitOne();
                        while (IsBusinessSoftwareRunning(blockedProcesses))
                        {
                            // Cancel if the user ask it
                            if (token.IsCancellationRequested) return -1;
                            if (!hasLogBlockWritte)
                            {
                                logManager.WriteNewLog(
                                    name: job.Name,
                                    sourcePath: job.Source,
                                    targetPath: target,
                                    action: "Program block the save",
                                    state: "BLOCKED",
                                    progress: (nbFileManaged / (double)nbFile) * 100,
                                    nbFile: nbFile,
                                    nbFileLeft: nbFile - nbFileManaged,
                                    sizeFileLeft: size / (nbFileManaged + 1)
                                );
                            }

                            // Wait 1 second to not DDOS the app
                            Thread.Sleep(1000);
                        }

                        if (token.IsCancellationRequested)
                        {
                            logManager.WriteNewLog(
                                name: job.Name,
                                sourcePath: job.Source,
                                targetPath: target + "\\",
                                action: "Cancel by user",
                                state: "Cancelled",
                                progress: 0,
                                nbFile: 0,
                                nbFileLeft: 0,
                                sizeFileLeft: 0
                            );
                            return -1;
                        }

                        string pathToCreate = el.Split(job.Source)[1];
                        if (Directory.Exists(el) && !marked.Contains(el))
                        {
                            queue.Enqueue(el);
                            marked.Add(el);

                            try
                            {
                                if (!Directory.Exists(target + pathToCreate))
                                {
                                    Stopwatch startTime = Stopwatch.StartNew();
                                    Directory.CreateDirectory(target + pathToCreate);
                                    startTime.Stop();
                                    logManager.WriteNewLog(
                                        name: job.Name,
                                        sourcePath: job.Source,
                                        targetPath: target + pathToCreate,
                                        action: "Creation of a directory",
                                        execTime: startTime.Elapsed.TotalMilliseconds
                                    );
                                }
                            }
                            catch (Exception e)
                            {
                                Console.WriteLine(e);
                            }
                        }
                        else
                        {
                            string ext = Path.GetExtension(el);
                            bool isPrio;
                            if (fileExtensions == null || fileExtensions.Count == 0)
                            {
                                isPrio = false;
                            }
                            else
                            {
                                isPrio = fileExtensions.Contains(ext, StringComparer.OrdinalIgnoreCase);
                            }


                            if (isPrio)
                            {
                                // if prio we copy now
                                try
                                {
                                    logManager.WriteNewLog(
                                        name: job.Name,
                                        sourcePath: job.Source,
                                        targetPath: target + pathToCreate,
                                        action: "Copy of a File",
                                        state: "ON",
                                        progress: (nbFileManaged / (double)nbFile) * 100,
                                        nbFile: nbFile,
                                        nbFileLeft: nbFile - nbFileManaged,
                                        sizeFileLeft: size / (nbFileManaged + 1)
                                    );

                                    long fileSizeBytes = new FileInfo(el).Length;
                                    long limitBytes = (long)(ConfigManager.FileSizeKo * 1024);

                                    if (limitBytes > 0 && fileSizeBytes > limitBytes)
                                    {
                                        TransferManager.LargeFileSemaphore.Wait(token);
                                        try
                                        {
                                            Stopwatch startTime = Stopwatch.StartNew();
                                            File.Copy(el, target + pathToCreate);
                                            startTime.Stop();
                                            logManager.WriteNewLog(name: job.Name, sourcePath: job.Source, targetPath: target + pathToCreate, action: "Copy of a File", execTime: startTime.Elapsed.TotalMilliseconds);
                                        }
                                        catch (OperationCanceledException) { return -1; }
                                        finally { TransferManager.LargeFileSemaphore.Release(); }
                                    }
                                    else
                                    {
                                        Stopwatch startTime = Stopwatch.StartNew();
                                        File.Copy(el, target + pathToCreate);
                                        startTime.Stop();
                                        logManager.WriteNewLog(name: job.Name, sourcePath: job.Source, targetPath: target + pathToCreate, action: "Copy of a File", execTime: startTime.Elapsed.TotalMilliseconds);
                                    }
                                    nbFileManaged++;
                                    logManager.WriteNewLog(
                                        name: job.Name,
                                        sourcePath: job.Source,
                                        targetPath: target + pathToCreate,
                                        action: "Copy of a File",
                                        state: "ON",
                                        progress: (nbFileManaged / (double)nbFile) * 100,
                                        nbFile: nbFile,
                                        nbFileLeft: nbFile - nbFileManaged,
                                        sizeFileLeft: size / nbFileManaged
                                    );
                                }
                                catch (Exception e)
                                {
                                    Console.WriteLine(e);
                                }

                                totalPrioFiles--;
                                PriorityManager.RemovePriorityFiles(1);
                            }
                            else
                            {
                                pendingNormalFiles.Add(el);
                            }
                        }
                    }
                }
            }

            // We wait the other process to copy their priority files
            PriorityManager.WaitForPriorityToFinish(token);

            // Check if user cancel
            if (token.IsCancellationRequested) return -1;

            // Process the non-prio files
            foreach (string el in pendingNormalFiles)
            {
                pauseEvent.WaitOne();
                while (IsBusinessSoftwareRunning(blockedProcesses))
                {
                    // Cancel if the user ask it
                    if (token.IsCancellationRequested) return -1;
                    if (!hasLogBlockWritte)
                    {
                        logManager.WriteNewLog(
                            name: job.Name,
                            sourcePath: job.Source,
                            targetPath: target,
                            action: "Program block the save",
                            state: "BLOCKED",
                            progress: (nbFileManaged / (double)nbFile) * 100,
                            nbFile: nbFile,
                            nbFileLeft: nbFile - nbFileManaged,
                            sizeFileLeft: size / (nbFileManaged + 1)
                        );
                    }

                    // Wait 1 second to not DDOS the app
                    Thread.Sleep(1000);
                }

                if (token.IsCancellationRequested) return -1;

                string pathToCreate = el.Split(job.Source)[1];

                try
                {
                    logManager.WriteNewLog(
                        name: job.Name,
                        sourcePath: job.Source,
                        targetPath: target + pathToCreate,
                        action: "Copy of a File",
                        state: "ON",
                        progress: (nbFileManaged / (double)nbFile) * 100,
                        nbFile: nbFile,
                        nbFileLeft: nbFile - nbFileManaged,
                        sizeFileLeft: size / (nbFileManaged + 1)
                    );

                    Stopwatch startTime = Stopwatch.StartNew();
                    File.Copy(el, target + pathToCreate);
                    startTime.Stop();
                    logManager.WriteNewLog(
                        name: job.Name,
                        sourcePath: job.Source,
                        targetPath: target + pathToCreate,
                        action: "Copy of a File",
                        execTime: startTime.Elapsed.TotalMilliseconds
                    );
                    nbFileManaged++;
                    logManager.WriteNewLog(
                        name: job.Name,
                        sourcePath: job.Source,
                        targetPath: target + pathToCreate,
                        action: "Copy of a File",
                        state: "ON",
                        progress: (nbFileManaged / (double)nbFile) * 100,
                        nbFile: nbFile,
                        nbFileLeft: nbFile - nbFileManaged,
                        sizeFileLeft: size / nbFileManaged
                    );
                }
                catch (Exception e)
                {
                    Console.WriteLine(e);
                }
            }

            logManager.WriteNewLog(
                name: job.Name,
                sourcePath: job.Source,
                targetPath: job.Target,
                action: "Job Finished",
                state: "OFF",
                progress: 100,
                nbFile: nbFile,
                nbFileLeft: 0,
                sizeFileLeft: 0
            );

            job.LastTimeRun = DateTime.Now;

            // Encrypt the backup files using the CryptoSoft server.
            // EasySave sends a request to CryptoSoft via a Named Pipe,
            // CryptoSoft encrypts the files and sends back the result.
            if (enableEncryption)
            {
                try
                {
                    // Use the user-provided encryption key
                    string key = encryptionKey;

                    // Build the encryption request with the target path and key
                    var request = new CryptoSoft.PipeRequest
                    {
                        Action = "encrypt",
                        Source = target,
                        Key = key,
                        Extensions = string.IsNullOrWhiteSpace(encryptionExtensions) ? null : encryptionExtensions
                    };

                    // Open a Named Pipe connection to the CryptoSoft server
                    using var pipe = new System.IO.Pipes.NamedPipeClientStream(
                        ".", CryptoSoft.PipeProtocol.PipeName, System.IO.Pipes.PipeDirection.InOut);
                    pipe.Connect(CryptoSoft.PipeProtocol.ClientTimeoutMs);

                    // Send the request and wait for CryptoSoft to respond
                    CryptoSoft.PipeProtocol.Send(pipe, request);
                    var response = CryptoSoft.PipeProtocol.Receive<CryptoSoft.PipeResponse>(pipe);

                    // If encryption succeeded, parse the time and write it to the log
                    if (response != null && response.ExitCode == 0)
                    {
                        // The last non-empty line of the output contains the time in ms
                        double encryptTimeMs = double.Parse(
                            response.Output.Split('\n').Last(l => !string.IsNullOrWhiteSpace(l))
                        );
                        logManager.WriteNewLog(
                            name: job.Name,
                            sourcePath: target,
                            targetPath: target,
                            action: "Encryption",
                            execTime: encryptTimeMs
                        );
                    }
                    else
                    {
                        return 4; // CryptoSoft returned an error
                    }
                }
                catch (Exception)
                {
                    return 4; // Could not connect to CryptoSoft server
                }
            }

            return 0;
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
            throw;
        }
        finally
        {
            // Si la méthode s'arrête brutalement (annulation utilisateur, crash réseau),
            // on enlève du compteur global le nombre de fichiers prioritaires que ce job
            // avait promis de faire mais n'a pas pu faire.
            if (totalPrioFiles > 0)
            {
                PriorityManager.RemovePriorityFiles(totalPrioFiles);
            }
        }
    }

    private bool IsBusinessSoftwareRunning(List<string> targetProcessNames)
    {
        if (targetProcessNames == null || targetProcessNames.Count == 0) return false;

        // On récupère tous les processus actuels
        var currentProcesses = Process.GetProcesses();

        // On regarde si l'un d'eux correspond à notre liste noire
        return currentProcesses.Any(p => targetProcessNames.Contains(p.ProcessName, StringComparer.OrdinalIgnoreCase));
    }

    private int DefinePriorityFiles(List<string> fileExtensionsPrio, Job job)
    {
        if (fileExtensionsPrio == null || fileExtensionsPrio.Count == 0) return 0;

        var allFiles = Directory.EnumerateFiles(job.Source, "*.*", SearchOption.AllDirectories);

        var priorityFiles = new List<string>();
        var normalFiles = new List<string>();

        foreach (var file in allFiles)
        {
            string ext = Path.GetExtension(file);
            if (fileExtensionsPrio.Contains(ext, StringComparer.OrdinalIgnoreCase))
                priorityFiles.Add(file);
            else
                normalFiles.Add(file);
        }

        int myPendingPriorityFiles = priorityFiles.Count;

        // Say to the app that we have priority files
        PriorityManager.AddPriorityFiles(myPendingPriorityFiles);

        return myPendingPriorityFiles;
    }
}