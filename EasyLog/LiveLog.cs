using System.Text.Json;
using System.Xml.Serialization;

namespace EasyLog;

public class LiveLog : Log
{
    private string _state;
    private long _nbFile;
    private double _progress;
    private long _nbFileLeft;
    private long _sizeFileLeft;
    private string _source;
    private string _target;

    public string State
    {
        get => _state;
        set => _state = value ?? throw new ArgumentNullException(nameof(value));
    }

    public long NbFile
    {
        get => _nbFile;
        set => _nbFile = value;
    }

    public double Progress
    {
        get => _progress;
        set => _progress = value;
    }

    public long NbFileLeft
    {
        get => _nbFileLeft;
        set => _nbFileLeft = value;
    }

    public long SizeFileLeft
    {
        get => _sizeFileLeft;
        set => _sizeFileLeft = value;
    }

    public string Source
    {
        get => _source;
        set => _source = value ?? throw new ArgumentNullException(nameof(value));
    }

    public string Target
    {
        get => _target;
        set => _target = value ?? throw new ArgumentNullException(nameof(value));
    }

    public LiveLog()
    {
    }

    public LiveLog(string name, string action, string state, long nbFile, double progress, long nbFileLeft, 
        long sizeFileLeft, string source, string target, LogType logType)
    {
        DateTime = DateTime.Now;
        Name = name;
        Action = action;
        State = state;
        NbFile = nbFile;
        Progress = progress;
        NbFileLeft = nbFileLeft;
        SizeFileLeft = sizeFileLeft;
        Source = source;
        Target = target;
        Type = logType;
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
                // Dans LiveLog.cs -> WriteLog
                case LogType.JSON:
                    string jsonObject = JsonSerializer.Serialize(this, new JsonSerializerOptions { WriteIndented = true });
    
                    // On utilise FileShare.Read pour que l'UI puisse lire pendant qu'on écrit
                    using (var fs = new FileStream(Path.Combine(path, "livestate.json"), FileMode.Create, FileAccess.Write, FileShare.Read))
                    using (var sw = new StreamWriter(fs))
                    {
                        sw.Write(jsonObject);
                    }
                    break;
                
                case LogType.XML:
                    XmlSerializer xmlSerializer = new(typeof(LiveLog));

                    // Utilisation d'un FileStream partagé pour éviter le blocage de l'UI
                    using (var fs = new FileStream(Path.Combine(path, "livestate.xml"), FileMode.Create, FileAccess.Write, FileShare.Read))
                    using (var writer = new StreamWriter(fs))
                    {
                        xmlSerializer.Serialize(writer, this);
                    }
                    break;
            }
            
            return 0;
        }
        catch (Exception e)
        {
            // Console.WriteLine(e);
            return 1;
        }
    }
}