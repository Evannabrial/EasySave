namespace EasySave.Services;

public class LogService
{
    public static LogObserverService Observer { get; } = new LogObserverService();
}