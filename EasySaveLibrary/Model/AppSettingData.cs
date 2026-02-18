namespace EasySaveLibrary.Model;

public class AppSettingData
{
    private string _pathLog;
    private double _fileSizeMo;

    public string PathLog
    {
        get => _pathLog;
        set => _pathLog = value ?? throw new ArgumentNullException(nameof(value));
    }
    
    public double FileSizeMo
    {
        get => _fileSizeMo;
        set => _fileSizeMo = value;
    }
}