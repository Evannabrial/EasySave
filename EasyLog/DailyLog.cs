using System.Text.Json;
using System.Xml.Serialization;

namespace EasyLog;

public class DailyLog : Log
{
    private string _sourcePath;
    private string _targetPath;
    private long _size;
    private double _execTime;

    public string SourcePath
    {
        get => _sourcePath;
        set => _sourcePath = value ?? throw new ArgumentNullException(nameof(value));
    }

    public string TargetPath
    {
        get => _targetPath;
        set => _targetPath = value ?? throw new ArgumentNullException(nameof(value));
    }

    public long Size
    {
        get => _size;
        set => _size = value;
    }

    public double ExecTime
    {
        get => _execTime;
        set => _execTime = value;
    }

    public DailyLog(string name, string action, string sourcePath, string targetPath, long size, double execTime, LogType type)
    {
        Id = Guid.NewGuid();
        DateTime = DateTime.Now;
        Name = name;
        Action = action;
        SourcePath = sourcePath;
        TargetPath = targetPath;
        Size = size;
        ExecTime = execTime;
        Type = type;
    }


    /// <summary>
    /// Write the object inside a file in json.
    /// </summary>
    /// <param name="path"></param>
    /// <returns>
    /// 0 => OK
    /// 1 => KO
    /// </returns>
    public override int WriteLog(string path)
    {
        try
        {
            switch (Type)
            {
                case LogType.JSON:
                    JsonSerializerOptions options = new()
                    {
                        WriteIndented = true
                    };

                    JsonSerializerOptions optionsCopy = new(options);
            
                    string jsonObject = JsonSerializer.Serialize(this, options);
                    File.AppendAllLines(
                        path + "\\" +  this.Name + "-" + DateTime.ToString("yyyyMMddHHmmss") + ".json", 
                        [jsonObject]);
                    break;
                
                case LogType.XML:
                    XmlSerializer xmlSerializer = new(typeof(DailyLog));
                    break;
            }
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
            return 1;
        }
        return 0;
    }
}