namespace EasyLog;

public class LogManager
{
    /// <summary>
    /// Write a log 
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
        
        Log log = new DailyLog(name, action, sourcePath, targetPath, size, execTime, LogType.JSON);

        return log.WriteLog(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Logs"));
    }
    
    
    public int WriteNewLog(string name, string sourcePath, string targetPath, string action, string state, double progress, 
        int nbFile, int nbFileLeft, long sizeFileLeft )
    {
        Log log = new LiveLog(name, action, state, nbFile, progress, nbFileLeft, sizeFileLeft, sourcePath, targetPath);
        var p = AppDomain.CurrentDomain;
        return log.WriteLog(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Logs"));
    }
}
