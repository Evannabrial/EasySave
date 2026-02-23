namespace EasySaveLibrary.Model;

public class AppSettingData
{
    private string _pathLog;
    private double _fileSizeKo;
    private string? _primaryColor;
    private string? _hoverColor;
    private string? _secondaryColor;
    private string? _textColor;

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
    
    // Couleurs du thème
    public string? PrimaryColor
    {
        get => _primaryColor;
        set => _primaryColor = value;
    }
    
    public string? HoverColor
    {
        get => _hoverColor;
        set => _hoverColor = value;
    }
    
    public string? SecondaryColor
    {
        get => _secondaryColor;
        set => _secondaryColor = value;
    }
    
    public string? TextColor
    {
        get => _textColor;
        set => _textColor = value;
    }
}