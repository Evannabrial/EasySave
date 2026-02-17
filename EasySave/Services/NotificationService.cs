// Services/NotificationService.cs
using System.Timers;
using CommunityToolkit.Mvvm.ComponentModel;
using Timer = System.Timers.Timer;

namespace EasySave.Services;

public partial class NotificationService : ObservableObject
{
    private static NotificationService? _instance;
    public static NotificationService Instance => _instance ??= new NotificationService();

    [ObservableProperty]
    private ToastNotification? _currentNotification;

    [ObservableProperty]
    private bool _isVisible;

    private Timer? _timer;

    public void Show(string message, ToastType type = ToastType.Success)
    {
        CurrentNotification = new ToastNotification
        {
            Message = message,
            Type = type
        };
        IsVisible = true;

        _timer?.Stop();
        _timer = new Timer(3000);
        _timer.Elapsed += (_, _) =>
        {
            Avalonia.Threading.Dispatcher.UIThread.Invoke(() => IsVisible = false);
            _timer.Stop();
        };
        _timer.Start();
    }
}