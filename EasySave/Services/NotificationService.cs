using System.Timers;
using Avalonia.Media;
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

    [ObservableProperty]
    private bool _isError;

    [ObservableProperty]
    private bool _isSuccess;

    [ObservableProperty]
    private string _icon = "✓";

    [ObservableProperty]
    private IBrush _backgroundColor = new SolidColorBrush(Color.Parse("#2ecc71"));

    private Timer? _timer;

    public void Show(string message, ToastType type = ToastType.Success)
    {
        CurrentNotification = new ToastNotification
        {
            Message = message,
            Type = type
        };
        
        // Définir les propriétés visuelles selon le type
        switch (type)
        {
            case ToastType.Error:
                IsError = true;
                IsSuccess = false;
                Icon = "✗";
                BackgroundColor = new SolidColorBrush(Color.Parse("#e74c3c")); // Rouge
                break;
            case ToastType.Warning:
                IsError = false;
                IsSuccess = false;
                Icon = "⚠";
                BackgroundColor = new SolidColorBrush(Color.Parse("#f39c12")); // Orange
                break;
            case ToastType.Info:
                IsError = false;
                IsSuccess = false;
                Icon = "ℹ";
                BackgroundColor = new SolidColorBrush(Color.Parse("#3498db")); // Bleu
                break;
            default: // Success
                IsError = false;
                IsSuccess = true;
                Icon = "✓";
                BackgroundColor = new SolidColorBrush(Color.Parse("#2ecc71")); // Vert
                break;
        }
        
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