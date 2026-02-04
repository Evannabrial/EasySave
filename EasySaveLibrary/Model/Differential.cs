using System.Diagnostics;
using EasySaveLibrary.Interfaces;
using System.Runtime.InteropServices.JavaScript;
using System.Text.RegularExpressions;
using EasyLog;
using EasySaveLibrary.Model;

namespace EasySaveLibrary.Model;

public class Differential : ITypeSave
{
    private LogManager logManager;
    
    public Differential()
    {
        logManager = new LogManager();
    }
    
    /// <returns>
    /// 0 => OK
    /// 1 => Element source non trouvé
    /// 2 => Erreur copie du fichier
    /// 3 => Erreur création du dossier
    /// </returns>
    public int StartSave(Job job)
    {
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
            return new Full().StartSave(job);
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
                if (File.GetLastWriteTime(job.Source) > File.GetLastWriteTime(pathLastSave))
                {
                    Stopwatch startTimeDir = Stopwatch.StartNew();
                    Directory.CreateDirectory(target);
                    startTimeDir.Stop();
                    logManager.WriteNewLog(
                        name: job.Name, 
                        sourcePath: job.Source, 
                        targetPath: target, 
                        action: "Creation of a directory", 
                        execTime: startTimeDir.Elapsed.TotalMilliseconds
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
                }
            }
            catch (Exception e)
            {
                // Console.WriteLine(e);
                return 2;
            }
            return 0;
        }
        
        // We use the Breadth-first (parcours en largeur) search algorithm to visit all the folders and files in the source
        // See algorithm here https://en.wikipedia.org/wiki/Breadth-first_search
        Queue<string> queue = new Queue<string>();
        List<string> marked = new List<string>();
        Directory.CreateDirectory(target);
        queue.Enqueue(job.Source);
        
        while (queue.Count > 0)
        {
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
                                Stopwatch startTimeFile = Stopwatch.StartNew();
                                File.Copy(el, target + pathToCreate);
                                startTimeFile.Stop();
                                logManager.WriteNewLog(
                                    name: job.Name, 
                                    sourcePath: job.Source, 
                                    targetPath: target + pathToCreate, 
                                    action: "Copy of a file", 
                                    execTime: startTimeFile.Elapsed.TotalMilliseconds
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
        
        return 0;
    }
}