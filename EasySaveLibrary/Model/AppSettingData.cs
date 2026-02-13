namespace EasySaveLibrary.Model;

public class AppSettingData
{
    private string _pathLog;

    public string PathLog
    {
        get => _pathLog;
        set => _pathLog = value ?? throw new ArgumentNullException(nameof(value));
    }
}