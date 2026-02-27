namespace EasySave.Services;

public class ToastNotification
{
    public string Message { get; set; } = string.Empty;
    public ToastType Type { get; set; } = ToastType.Info;
}

public enum ToastType
{
    Success,
    Info,
    Warning,
    Error
}