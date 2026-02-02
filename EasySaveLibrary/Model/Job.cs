using EasySaveLibrary.Interfaces;

namespace EasySaveLibrary.Model;

public class Job
{
    private string _name;
    private ITypeSave _save;
    private string _source;
    private string _target;

    public Job(string name, string source, string target, ITypeSave save)
    {
        Id = Guid.NewGuid();
        Name = name;
        Source = source;
        Target = target;
        Save = save;
    }

    public Guid Id { get; set; }

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