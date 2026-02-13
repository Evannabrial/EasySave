namespace EasySaveLibrary.Model;

public class JobStatus
{
    private Guid _idJob;
    private string _status;
    private double _progress;

    public Guid IdJob
    {
        get => _idJob;
        set => _idJob = value;
    }

    public string Status
    {
        get => _status;
        set => _status = value ?? throw new ArgumentNullException(nameof(value));
    }

    public double Progress
    {
        get => _progress;
        set => _progress = value;
    }

    public JobStatus(Guid idJob, string status, double progress)
    {
        _idJob = idJob;
        _status = status;
        _progress = progress;
    }
}