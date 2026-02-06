using System.Text.Json.Serialization;

namespace EasyLog;

public abstract class Log
{
    private Guid _id;
    private DateTime _dateTime;
    private string _name;
    private string _action;
    private LogType _type;

    public Guid Id
    {
        get => _id;
        set => _id = value;
    }

    public DateTime DateTime
    {
        get => _dateTime;
        set => _dateTime = value;
    }

    public string Name
    {
        get => _name;
        set => _name = value ?? throw new ArgumentNullException(nameof(value));
    }

    public string Action
    {
        get => _action;
        set => _action = value ?? throw new ArgumentNullException(nameof(value));
    }

    [JsonIgnore] 
    public LogType Type
    {
        get => _type;
        set => _type = value;
    }

    public abstract int WriteLog(string path);
}
