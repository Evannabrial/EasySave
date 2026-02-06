using System.Text.Json;

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

    public LiveLog(string name, string action, string state, long nbFile, double progress, long nbFileLeft, 
        long sizeFileLeft, string source, string target)
    {
        Id = Guid.NewGuid();
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
                    File.WriteAllLines(
                        path + "\\" + "livestate.json", 
                        [jsonObject]);
                    break;
                
                case LogType.XML:
                    //TODO: to implement
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