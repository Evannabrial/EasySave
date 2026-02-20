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

    // Reference to the CryptoSoft server process.
    // CryptoSoft is launched once when EasySave starts (mono-instance)
    // and communicates with EasySave via Named Pipes.
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

    // Starts CryptoSoft.exe as a Named Pipe server in the background.
    // CryptoSoft will listen for encryption/decryption requests from EasySave.
    // The path is resolved relative to the EasySave executable directory.
    private void StartCryptoSoftServer()
    {
        try
        {
            // Find CryptoSoft.exe in the same directory as EasySave.exe
            string path = System.IO.Path.Combine(
                AppDomain.CurrentDomain.BaseDirectory, "CryptoSoft.exe");

            // Start CryptoSoft as a hidden background process (no window)
            _cryptoSoftServer = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = path,
                    UseShellExecute = false,
                    CreateNoWindow = true,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true
                }
            };

            _cryptoSoftServer.Start();
            
            // Drain stdout/stderr asynchronously to prevent OS pipe buffer deadlock.
            // If nobody reads these streams, Console.Write in CryptoSoft will block
            // once the ~4 KB buffer fills up, causing encryption to stall.
            _cryptoSoftServer.BeginOutputReadLine();
            _cryptoSoftServer.BeginErrorReadLine();
        }
        catch { }
    }

    // Stops the CryptoSoft server process when EasySave shuts down.
    // This ensures no orphan processes remain after closing the app.
    private void StopCryptoSoftServer()
    {
        try
        {
            // Kill the process if it is still running
            if (_cryptoSoftServer is { HasExited: false })
            {
                _cryptoSoftServer.Kill();
                _cryptoSoftServer.WaitForExit(2000); // Wait max 2 seconds
            }
        }
        catch { }
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
