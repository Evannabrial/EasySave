using System.Diagnostics;

namespace EasyLog;

public class LogManager
{
    private LogType _typeSave;

    public LogType TypeSave
    {
        get => _typeSave;
        set => _typeSave = value;
    }

    public LogManager()
    {
    }


    /// <summary>
    /// Write a DailyLog 
    /// </summary>
    /// <param name="name"></param>
    /// <param name="sourcePath"></param>
    /// <param name="targetPath"></param>
    /// <param name="action"></param>
    /// <param name="execTime"></param>
    /// <returns></returns>
    public int WriteNewLog(string name, string sourcePath, string targetPath, string action, double execTime)
    {
        long size = -1;
        if (File.Exists(sourcePath))
        {
            FileInfo fi = new FileInfo(sourcePath);
            size = fi.Length;
        }
        else if(Directory.Exists(sourcePath))
        {
            size = Directory.EnumerateFiles(sourcePath, "*", SearchOption.AllDirectories)
                .Sum(f => new FileInfo(f).Length);;
        }
        
        Log log = new DailyLog(name, action, sourcePath, targetPath, size, execTime, TypeSave);
        
        string programDataPath = Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData);
        return log.WriteLog(Path.Combine(programDataPath, "EasySave", "Logs"));
    }
    
    
    /// <summary>
    /// Write a LiveLog
    /// </summary>
    /// <param name="name"></param>
    /// <param name="sourcePath"></param>
    /// <param name="targetPath"></param>
    /// <param name="action"></param>
    /// <param name="state"></param>
    /// <param name="progress"></param>
    /// <param name="nbFile"></param>
    /// <param name="nbFileLeft"></param>
    /// <param name="sizeFileLeft"></param>
    /// <returns></returns>
    public int WriteNewLog(string name, string sourcePath, string targetPath, string action, string state, double progress, 
        int nbFile, int nbFileLeft, long sizeFileLeft )
    {
        Log log = new LiveLog(name, action, state, nbFile, progress, nbFileLeft, sizeFileLeft, sourcePath, targetPath, TypeSave);
        var p = AppDomain.CurrentDomain;
        
        string programDataPath = Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData);

        // Créer le dossier s'il n'existe pas
        if (!Directory.Exists(Path.Combine(programDataPath, "EasySave")))
        {
            Directory.CreateDirectory(Path.Combine(programDataPath, "EasySave"));
        }
        
        // Créer le dossier s'il n'existe pas
        if (!Directory.Exists(Path.Combine(programDataPath, "EasySave", "Logs")))
        {
            Directory.CreateDirectory(Path.Combine(programDataPath, "EasySave", "Logs"));
        }
        
        return log.WriteLog(Path.Combine(programDataPath, "EasySave", "Logs"));
    }
}
