using EasySaveLibrary.Interfaces;

namespace EasySaveLibrary.Model;

public class Job
{
    private Guid _id;
    private string _name;
    private string _source;
    private string _target;
    private DateTime? _lastTimeRun;
    private ITypeSave _save;
    private bool _enableEncryption;

    public Guid Id
    {
        get => _id;
        set => _id = value;
    }


    public string Name
    {
        get => _name;
        set => _name = value ?? throw new ArgumentNullException(nameof(value));
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

    public DateTime? LastTimeRun
    {
        get => _lastTimeRun;
        set => _lastTimeRun = value;
    }

    public ITypeSave Save
    {
        get => _save;
        set => _save = value ?? throw new ArgumentNullException(nameof(value));
    }

    public Job(string name, string source, string target, ITypeSave save)
    {
        Id = Guid.NewGuid();
        Name = name;
        Source = source;
        Target = target;
        LastTimeRun = null;
        Save = save;
    }

    public override string ToString()
    {
        string stringTypesave = "";
        if (Save is Full)
        {
            stringTypesave = "Full backup";
        }
        else if (Save is Differential)
        {
            stringTypesave = "Differential backup";
        }
        
        return Name + " | " + Source + " | " + Target + " | "+ stringTypesave;
    }
}