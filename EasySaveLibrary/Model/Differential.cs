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
    public int StartSave(Job job, LogType logType, ManualResetEvent pauseEvent, bool enableEncryption = false, 
        string encryptionExtensions = "")
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
            return new Full().StartSave(job, logType, pauseEvent, enableEncryption, encryptionExtensions);
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
                    
                    Stopwatch startTime = Stopwatch.StartNew();
                    File.Copy(job.Source, target + "\\"  + nameFile);
                    startTimeDir.Stop();
                    logManager.WriteNewLog(
                        name: job.Name, 
                        sourcePath: job.Source, 
                        targetPath: target,
                        action: "Copy of a file", 
                        execTime: startTimeDir.Elapsed.TotalMilliseconds
                    );
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
            string actual = queue.Dequeue();

            if (Directory.Exists(actual))
            {
                foreach (string el in Directory.EnumerateFileSystemEntries(actual))
                {
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

        // CryptoSoft encryption if enabled
        if (enableEncryption)
        {
            try
            {
                // Generate a random 16-byte key and encode it in Base64
                string key = Convert.ToBase64String(RandomNumberGenerator.GetBytes(16));
                // Display the key so the user can save it for later decryption
                Console.WriteLine($"Encryption key for '{job.Name}' : {key}");

                // Build the path to the CryptoSoft executable (expected in the same directory)
                string cryptoSoftPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "CryptoSoft.exe");

                // Build the command-line arguments: encrypt <targetDir> <key> [extensions]
                string arguments = $"encrypt \"{target}\" \"{key}\"";
                // If specific extensions are set, only those file types will be encrypted
                if (!string.IsNullOrWhiteSpace(encryptionExtensions))
                {
                    arguments += $" \"{encryptionExtensions}\"";
                }

                // Launch CryptoSoft as an external process
                var process = new Process
                {
                    StartInfo = new ProcessStartInfo
                    {
                        FileName = cryptoSoftPath,
                        Arguments = arguments,
                        RedirectStandardOutput = true,
                        UseShellExecute = false,
                        CreateNoWindow = true
                    }
                };

                process.Start();
                // Read the output (contains the encryption time on the last line)
                string output = process.StandardOutput.ReadToEnd();
                process.WaitForExit();

                if (process.ExitCode == 0)
                {
                    // Parse the encryption time (in ms) from the last non-empty line of CryptoSoft output
                    double encryptTimeMs = double.Parse(
                        output.Split('\n').Last(l => !string.IsNullOrWhiteSpace(l))
                    );
                    // Log the encryption result
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
                return 4; // Encryption failed
            }
        }

        return 0;
    }
}