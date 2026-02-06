using System.Diagnostics;
using System.Text.RegularExpressions;
using EasyLog;
using EasySaveLibrary.Interfaces;

namespace EasySaveLibrary.Model;

public class Full : ITypeSave
{
    private LogManager logManager;
    
    public Full()
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
                // Console.WriteLine(e);
                return 2;
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
                            // Console.WriteLine(e);
                            return 3;
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
        job.LastTimeRun = DateTime.Now;
        return 0;
    }
}