using System;
using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Data.Core;
using Avalonia.Data.Core.Plugins;
using System.Diagnostics;
using System.Linq;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using EasySave.Services;
using EasySave.ViewModels;
using EasySave.Views;
using EasySaveLibrary;

namespace EasySave;

public partial class App : Application
{
    public JobManager JobManager => JobManagerService.Instance.JobManager;
    public static Window MainWindow { get; set; }

    // CryptoSoft server process (mono-instance, started at app launch)
    private Process? _cryptoSoftServer;

    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);
    }

    public override void OnFrameworkInitializationCompleted()
    {
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            // Avoid duplicate validations from both Avalonia and the CommunityToolkit. 
            // More info: https://docs.avaloniaui.net/docs/guides/development-guides/data-validation#manage-validationplugins
            DisableAvaloniaDataAnnotationValidation();
            desktop.MainWindow = new MainWindow
            {
                DataContext = new MainWindowViewModel(JobManager),
            };
            MainWindow = desktop.MainWindow;

            // Start CryptoSoft server in background
            StartCryptoSoftServer();

            // Stop server when EasySave closes
            desktop.ShutdownRequested += (_, _) => StopCryptoSoftServer();
        }

        base.OnFrameworkInitializationCompleted();
    }

    // Starts CryptoSoft.exe in server mode (--server)
    private void StartCryptoSoftServer()
    {
        try
        {
            string path = System.IO.Path.Combine(
                AppDomain.CurrentDomain.BaseDirectory, "CryptoSoft.exe");

            _cryptoSoftServer = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = path,
                    Arguments = "--server",
                    UseShellExecute = false,
                    CreateNoWindow = true,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true
                }
            };

            _cryptoSoftServer.Start();
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"Failed to start CryptoSoft server: {ex.Message}");
        }
    }

    // Stops the CryptoSoft server when EasySave shuts down
    private void StopCryptoSoftServer()
    {
        try
        {
            if (_cryptoSoftServer is { HasExited: false })
            {
                _cryptoSoftServer.Kill();
                _cryptoSoftServer.WaitForExit(2000);
            }
        }
        catch { /* Server already stopped */ }
    }

    private void DisableAvaloniaDataAnnotationValidation()
    {
        // Get an array of plugins to remove
        var dataValidationPluginsToRemove =
            BindingPlugins.DataValidators.OfType<DataAnnotationsValidationPlugin>().ToArray();

        // remove each entry found
        foreach (var plugin in dataValidationPluginsToRemove)
        {
            BindingPlugins.DataValidators.Remove(plugin);
        }
    }
}
