using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Pipes;
using System.Text.Json;
using System.Threading.Tasks;
using System.Windows.Input;
using Avalonia.Controls;
using Avalonia.Platform.Storage;
using EasySave.Services;
using EasySaveLibrary;

namespace EasySave.ViewModels;

public class DecryptViewModel : ViewModelBase
{
    private readonly JobManager _jobManager;
    private Dictionary<string, string> _dictText;
    private string _sourcePath = "";
    private string _password = "";
    private bool _isDecrypting;

    public DecryptViewModel(JobManager jobManager)
    {
        _jobManager = jobManager;
        DictText = jobManager.Language.GetTranslations();
        DecryptCommand = new RelayCommandService(_ => DecryptFunction());
        OpenSourcePickerCommand = new RelayCommandService(_ => OpenSourcePicker());
    }

    public ICommand DecryptCommand { get; }
    public ICommand OpenSourcePickerCommand { get; }

    public Dictionary<string, string> DictText
    {
        get => _dictText;
        set => SetProperty(ref _dictText, value);
    }

    public string SourcePath
    {
        get => _sourcePath;
        set => SetProperty(ref _sourcePath, value);
    }

    public string Password
    {
        get => _password;
        set => SetProperty(ref _password, value);
    }

    public bool IsDecrypting
    {
        get => _isDecrypting;
        set => SetProperty(ref _isDecrypting, value);
    }

    private async void OpenSourcePicker()
    {
        var topLevel = TopLevel.GetTopLevel(App.MainWindow);
        if (topLevel == null) return;

        var folders = await topLevel.StorageProvider.OpenFolderPickerAsync(new FolderPickerOpenOptions
        {
            Title = DictText.GetValueOrDefault("DecryptSourceMessage", "Select source"),
            AllowMultiple = false
        });

        if (folders.Count > 0)
            SourcePath = folders[0].Path.LocalPath;
    }

    /// <summary>
    /// Sends a decrypt request to CryptoSoft for a single file via Named Pipe.
    /// </summary>
    private bool DecryptSingleFile(string filePath)
    {
        var request = new CryptoSoft.PipeRequest { Action = "decrypt", Source = filePath, Key = Password };

        using var pipe = new NamedPipeClientStream(".", CryptoSoft.PipeProtocol.PipeName, PipeDirection.InOut);
        pipe.Connect(CryptoSoft.PipeProtocol.ClientTimeoutMs);
        CryptoSoft.PipeProtocol.Send(pipe, request);

        using var reader = new StreamReader(pipe, leaveOpen: false);
        string raw = reader.ReadToEnd().Trim();
        if (string.IsNullOrEmpty(raw)) return false;

        var response = JsonSerializer.Deserialize<CryptoSoft.PipeResponse>(raw.Split('\n')[0]);
        return response is { ExitCode: 0 };
    }

    private async void DecryptFunction()
    {
        if (string.IsNullOrWhiteSpace(SourcePath) || string.IsNullOrWhiteSpace(Password))
        {
            NotificationService.Instance.Show(
                DictText.GetValueOrDefault("DecryptErrorFieldsRequired", "All fields are required."), ToastType.Error);
            return;
        }

        IsDecrypting = true;
        NotificationService.Instance.Show(
            DictText.GetValueOrDefault("DecryptInProgressMessage", "Decryption in progress..."), ToastType.Info);

        try
        {
            var (success, fail) = await Task.Run(() =>
            {
                int ok = 0, ko = 0;

                // Collect all .enc files upfront to avoid modifying the directory while enumerating
                string[] files = File.Exists(SourcePath)
                    ? new[] { SourcePath }
                    : Directory.GetFiles(SourcePath, "*.enc", SearchOption.AllDirectories);

                foreach (var file in files)
                {
                    try { if (DecryptSingleFile(file)) ok++; else ko++; }
                    catch { ko++; }
                }

                return (ok, ko);
            });

            if (success > 0 && fail == 0)
                NotificationService.Instance.Show(
                    DictText.GetValueOrDefault("DecryptSuccessMessage", "Decryption completed successfully!"), ToastType.Success);
            else if (success == 0)
                NotificationService.Instance.Show(
                    DictText.GetValueOrDefault("DecryptErrorMessage", "An error occurred during decryption."), ToastType.Error);
            else
                NotificationService.Instance.Show($"{success} OK, {fail} failed", ToastType.Warning);
        }
        catch
        {
            NotificationService.Instance.Show(
                DictText.GetValueOrDefault("DecryptErrorCryptoSoft", "Could not connect to CryptoSoft server."), ToastType.Error);
        }
        finally
        {
            IsDecrypting = false;
        }
    }
}