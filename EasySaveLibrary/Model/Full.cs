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
    
    public Full()
    {
        logManager = new LogManager(ConfigManager.LogPath);
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
         logManager = new LogManager(ConfigManager.LogPath);
         logManager.TypeSave = logType;
        
        bool isFile = File.Exists(job.Source);
        bool isDirectory = Directory.Exists(job.Source);
        string target = job.Target + "\\" + job.Name + "-" + DateTime.Now.ToString("yyyyMMddHHmmss");
        
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
                Stopwatch startTime = Stopwatch.StartNew();
                File.Copy(job.Source, target + "\\"  + nameFile);
                startTime.Stop();
                logManager.WriteNewLog(
                    name: job.Name, 
                    sourcePath: job.Source, 
                    targetPath: target + "\\"  + nameFile, 
                    action: "Copy of a File", 
                    execTime: startTime.Elapsed.TotalMilliseconds
                    );
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
        
        queue.Enqueue(job.Source);
        
        while (queue.Count > 0)
        {
            pauseEvent.WaitOne();
            string actual = queue.Dequeue();

            if (Directory.Exists(actual))
            {
                foreach (string el in Directory.EnumerateFileSystemEntries(actual))
                {
                    pauseEvent.WaitOne();
                    string pathToCreate = el.Split(job.Source)[1];
                    if (Directory.Exists(el) &&  !marked.Contains(el))
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
                }
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