namespace LogLib;

public abstract class Log
{
    private Guid _id;
    private string? _path;

    public Guid Id
    {
        get => _id;
        set => _id = value;
    }

    public string Path
    {
        get => _path;
        set => _path = value;
    }

    public abstract int Write();
}