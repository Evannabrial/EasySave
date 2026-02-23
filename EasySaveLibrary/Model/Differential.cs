using System.Diagnostics;
using System.Security.Cryptography;
using EasySaveLibrary.Interfaces;
using System.Runtime.InteropServices.JavaScript;
using System.Text.RegularExpressions;
using EasyLog;
using EasySaveLibrary.Model;

namespace EasySaveLibrary.Model;

public class Differential : ITypeSave
{
    public string DisplayName => "Differential";
    private LogManager logManager;
    private bool hasLogBlockWritte = false;
    public Differential()
    {
        logManager = new LogManager(ConfigManager.Root["PathLog"]);
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
        string[] listBlockProcess, bool enableEncryption = false, string encryptionExtensions = "", string encryptionKey = "")
    {
        logManager.TypeSave = logType;
        
        // S'il n'y a jamais eu de sauvegarde de faites, on effectue une sauvegarde complète
        IEnumerable<string> listNameSave = Directory.EnumerateDirectories(job.Target);
        bool isCompleteSaveExist = false;
        foreach (string el in listNameSave)
        {
            if (el.Contains(job.Name))
            {
                isCompleteSaveExist = true;
                break;
            }
        }
        
        if (job.LastTimeRun == null && !isCompleteSaveExist)
        {
            return new Full().StartSave(job, logType, pauseEvent, token, listBlockProcess, enableEncryption, 
                encryptionExtensions, encryptionKey);
        }
        
        bool isFile = File.Exists(job.Source);
        bool isDirectory = Directory.Exists(job.Source);
        string target = job.Target + "\\" + job.Name + "-" + DateTime.Now.ToString("yyyyMMddHHmmss");
        
        if (!isDirectory && !isFile)
        {
            return 1;
        }
        
        // On récupère le chemin de la dernière save
        DateTime dateLast = DateTime.MinValue;
        string pathLastSave = "";
        foreach (string el in Directory.EnumerateDirectories(job.Target))
        {
            if (el.Contains(job.Name))
            {
                string dateString = Regex.Match(el, @"[^-]*$").Value;
                DateTime date = DateTime.ParseExact(dateString, "yyyyMMddHHmmss", null);
                if (date > dateLast)
                {
                    pathLastSave = el;
                    dateLast = date;
                }
            }
        }

        if (isFile)
        {
            try
            {
                string nameFile = Regex.Match(job.Source, @"[^\\]+$").Value;
                pauseEvent.WaitOne();
                while (IsBusinessSoftwareRunning(listBlockProcess.ToList()))
                {
                    // Si on annule pendant le blocage logiciel
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
                    // On attend 1 seconde avant de revérifier pour ne pas surcharger le CPU
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
                if (File.GetLastWriteTime(job.Source) > File.GetLastWriteTime(pathLastSave))
                {
                    Stopwatch startTimeDir = Stopwatch.StartNew();
                    logManager.WriteNewLog(
                        name: job.Name,
                        sourcePath: job.Source,
                        targetPath: target,
                        action: "Creation of a directory",
                        state: "ON",
                        progress: 0,
                        nbFile: 2,
                        nbFileLeft: 2,
                        sizeFileLeft: new FileInfo(job.Source).Length
                    );
                    Directory.CreateDirectory(target);
                    startTimeDir.Stop();
                    logManager.WriteNewLog(
                        name: job.Name, 
                        sourcePath: job.Source, 
                        targetPath: target, 
                        action: "Creation of a directory", 
                        execTime: startTimeDir.Elapsed.TotalMilliseconds
                    );
                    logManager.WriteNewLog(
                        name: job.Name,
                        sourcePath: job.Source,
                        targetPath: target,
                        action: "Copy of a file",
                        state: "ON",
                        progress: 50,
                        nbFile: 2,
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
        
                            logManager.WriteNewLog(
                                name: job.Name, 
                                sourcePath: job.Source, 
                                targetPath: target,
                                action: "Copy of a file", 
                                execTime: startTime.Elapsed.TotalMilliseconds
                            );
                        }
                        catch (OperationCanceledException) { return -1; }
                        finally { TransferManager.LargeFileSemaphore.Release(); }
                    }
                    else
                    {
                        Stopwatch startTime = Stopwatch.StartNew();
                        File.Copy(job.Source, target + "\\" + nameFile);
                        startTime.Stop();
    
                        logManager.WriteNewLog(
                            name: job.Name, 
                            sourcePath: job.Source, 
                            targetPath: target,
                            action: "Copy of a file", 
                            execTime: startTime.Elapsed.TotalMilliseconds
                        );
                    }
                    logManager.WriteNewLog(
                        name: job.Name,
                        sourcePath: job.Source,
                        targetPath: target,
                        action: "Copy of a file",
                        state: "ON",
                        progress: 100,
                        nbFile: 2,
                        nbFileLeft: 0,
                        sizeFileLeft: 0
                    );
                }
            }
            catch (Exception e)
            {
                // Console.WriteLine(e);
                return 2;
            }
            return 0;
        }
        
        // Vars for data and logs
        long size = Directory.EnumerateFiles(job.Source, "*", SearchOption.AllDirectories)
            .Sum(f => new FileInfo(f).Length);
        int nbFile = Directory.EnumerateFiles(job.Source, "*", SearchOption.AllDirectories)
            .Count();
        int nbFileManaged = 0;
        
        // We use the Breadth-first (parcours en largeur) search algorithm to visit all the folders and files in the source
        // See algorithm here https://en.wikipedia.org/wiki/Breadth-first_search
        Queue<string> queue = new Queue<string>();
        List<string> marked = new List<string>();
        
        Stopwatch startTimeDirStart = Stopwatch.StartNew();
        Directory.CreateDirectory(target);
        startTimeDirStart.Stop();
        logManager.WriteNewLog(
            name: job.Name, 
            sourcePath: job.Source, 
            targetPath: target , 
            action: "Creation of a directory", 
            execTime: startTimeDirStart.Elapsed.TotalMilliseconds
        );
        queue.Enqueue(job.Source);
        
        while (queue.Count > 0)
        {
            pauseEvent.WaitOne();
            while (IsBusinessSoftwareRunning(listBlockProcess.ToList()))
            {
                // Si on annule pendant le blocage logiciel
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
                // On attend 1 seconde avant de revérifier pour ne pas surcharger le CPU
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
            string actual = queue.Dequeue();

            if (Directory.Exists(actual))
            {
                foreach (string el in Directory.EnumerateFileSystemEntries(actual))
                {
                    while (IsBusinessSoftwareRunning(listBlockProcess.ToList()))
                    {
                        // Si on annule pendant le blocage logiciel
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
                        // On attend 1 seconde avant de revérifier pour ne pas surcharger le CPU
                        Thread.Sleep(1000); 
                    }
                    pauseEvent.WaitOne();
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
                    if (Directory.Exists(el) &&  !marked.Contains(el))
                    {
                        queue.Enqueue(el);
                        marked.Add(el);
                        DirectoryInfo actualDirInfo = new DirectoryInfo(el);
                        
                        // If a directory is new and he has been add after the last save we create it
                        if (!Directory.Exists(pathLastSave + pathToCreate) && actualDirInfo.LastWriteTime > dateLast)
                        {
                            Stopwatch startTime = Stopwatch.StartNew();
                            Directory.CreateDirectory(target + pathToCreate);
                            startTime.Stop();
                            logManager.WriteNewLog(
                                name: job.Name, 
                                sourcePath: job.Source, 
                                targetPath: target, 
                                action: "Creation of a directory", 
                                execTime: startTime.Elapsed.TotalMilliseconds
                            );
                        }
                    }
                    else
                    {
                        try
                        {
                            if (File.GetLastWriteTime(el) > dateLast)
                            {
                                MatchCollection directories = Regex.Matches(pathToCreate,  @"[^\\]+(?=\\)");
                                string pathNewDirectory = "";
                                foreach (Match match in directories)
                                {
                                    // We create every directory the modified file is inside
                                    if (!Directory.Exists(target + match.Value))
                                    {
                                        pathNewDirectory = pathNewDirectory + "\\" + match.Value;
                                        Stopwatch startTime = Stopwatch.StartNew();
                                        Directory.CreateDirectory(target + pathNewDirectory);
                                        startTime.Stop();
                                        logManager.WriteNewLog(
                                            name: job.Name, 
                                            sourcePath: job.Source, 
                                            targetPath: target + pathNewDirectory, 
                                            action: "Creation of a directory", 
                                            execTime: startTime.Elapsed.TotalMilliseconds
                                        );
                                    }
                                }
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
                                        Stopwatch startTimeFile = Stopwatch.StartNew();
                                        File.Copy(el, target + pathToCreate);
                                        startTimeFile.Stop();
                                        nbFileManaged++;
        
                                        logManager.WriteNewLog(
                                            name: job.Name, 
                                            sourcePath: job.Source, 
                                            targetPath: target + pathToCreate, 
                                            action: "Copy of a file", 
                                            execTime: startTimeFile.Elapsed.TotalMilliseconds
                                        );
                                    }
                                    catch (OperationCanceledException) { return -1; }
                                    finally { TransferManager.LargeFileSemaphore.Release(); }
                                }
                                else
                                {
                                    Stopwatch startTimeFile = Stopwatch.StartNew();
                                    File.Copy(el, target + pathToCreate);
                                    startTimeFile.Stop();
                                    nbFileManaged++;
    
                                    logManager.WriteNewLog(
                                        name: job.Name, 
                                        sourcePath: job.Source, 
                                        targetPath: target + pathToCreate, 
                                        action: "Copy of a file", 
                                        execTime: startTimeFile.Elapsed.TotalMilliseconds
                                    );
                                }
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
                            }
                        }
                        catch (Exception e)
                        {
                            // Console.WriteLine(e.Message);
                            return 2;
                        }
                    }
                }
            }
        }
        logManager.WriteNewLog(
            name: job.Name,
            sourcePath: job.Source,
            targetPath: job.Target,
            action: "Copy of a File",
            state: "OFF",
            progress: 0,
            nbFile: 0,
            nbFileLeft: 0,
            sizeFileLeft: 0
        );

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
    
    private bool IsBusinessSoftwareRunning(List<string> targetProcessNames)
    {
        if (targetProcessNames == null || targetProcessNames.Count == 0) return false;
        
        // On récupère tous les processus actuels
        var currentProcesses = Process.GetProcesses();
        
        // On regarde si l'un d'eux correspond à notre liste noire
        return currentProcesses.Any(p => targetProcessNames.Contains(p.ProcessName, StringComparer.OrdinalIgnoreCase));
    }
}