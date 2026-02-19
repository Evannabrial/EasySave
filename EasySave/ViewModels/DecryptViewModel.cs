using System;
using System.Collections.Generic;
using System.IO.Pipes;
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

    private void DecryptFunction()
    {
        // Validate all fields
        if (string.IsNullOrWhiteSpace(SourcePath) ||
            string.IsNullOrWhiteSpace(Password))
        {
            NotificationService.Instance.Show(
                DictText.GetValueOrDefault("DecryptErrorFieldsRequired", "All fields are required."),
                ToastType.Error);
            return;
        }

        try
        {
            // Build the decryption request for CryptoSoft
            var request = new CryptoSoft.PipeRequest
            {
                Action = "decrypt",
                Source = SourcePath,
                Key = Password
            };

            // Connect to CryptoSoft Named Pipe server
            using var pipe = new NamedPipeClientStream(
                ".", CryptoSoft.PipeProtocol.PipeName, PipeDirection.InOut);
            pipe.Connect(CryptoSoft.PipeProtocol.ClientTimeoutMs);

            // Send request and receive response
            CryptoSoft.PipeProtocol.Send(pipe, request);
            var response = CryptoSoft.PipeProtocol.Receive<CryptoSoft.PipeResponse>(pipe);

            if (response != null && response.ExitCode == 0 &&
                string.IsNullOrWhiteSpace(response.Error) &&
                !response.Output.Contains("0 file(s)"))
            {
                NotificationService.Instance.Show(
                    DictText.GetValueOrDefault("DecryptSuccessMessage", "Decryption completed successfully!"),
                    ToastType.Success);
            }
            else
            {
                string errorMsg = response?.Error ?? "";
                NotificationService.Instance.Show(
                    DictText.GetValueOrDefault("DecryptErrorMessage", "An error occurred during decryption.") +
                    (string.IsNullOrEmpty(errorMsg) ? "" : $" ({errorMsg.Trim()})"),
                    ToastType.Error);
            }
        }
        catch (Exception)
        {
            NotificationService.Instance.Show(
                DictText.GetValueOrDefault("DecryptErrorCryptoSoft", "Could not connect to CryptoSoft server."),
                ToastType.Error);
        }
    }
}