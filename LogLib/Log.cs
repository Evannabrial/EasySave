namespace LogLib;

public abstract class Log
{
    private string? _path;

    public Guid Id { get; set; }

    public string Path
    {
        get => _path;
        set => _path = value;
    }

    public abstract int Write();
}