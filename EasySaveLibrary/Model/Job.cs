using EasySaveLibrary.Interfaces;

namespace EasySaveLibrary.Model;

public class Job
{
    private Guid _id;
    private string _name;
    private string _source;
    private string _target;
    private ITypeSave _save;

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

    public ITypeSave Save
    {
        get => _save;
        set => _save = value ?? throw new ArgumentNullException(nameof(value));
    }
}