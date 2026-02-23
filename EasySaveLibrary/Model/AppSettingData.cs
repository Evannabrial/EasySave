namespace EasySaveLibrary.Model;

public class AppSettingData
{
    private string _pathLog;
    private double _fileSizeKo;

    public string PathLog
    {
        get => _pathLog;
        set => _pathLog = value ?? throw new ArgumentNullException(nameof(value));
    }
    
    public double FileSizeKo
    {
        get => _fileSizeKo;
        set => _fileSizeKo = value;
    }
}