using System;
using System.Collections.Generic;
using System.IO;
using System.Windows.Input;
using Avalonia.Controls;
using Avalonia.Platform.Storage;
using EasyLog;
using EasySave.Services;
using EasySaveLibrary;

namespace EasySave.ViewModels;

public class SettingsViewModel : ViewModelBase
{
    private Dictionary<string, string> _dictText;
    private string _listeProcess;
    private string _logPath;
    private int _selectedFormatIndex;

    public SettingsViewModel(JobManager jobManager)
    {
        JobManager = jobManager;
        DictText = jobManager.Language.GetTranslations();
        ListeProcess = jobManager.ListeProcess;

        SelectedFormatIndex = JobManager.LogType == LogType.XML ? 1 : 0;

        OpenFilePickerCommand = new RelayCommandService(param => { OpenFilePickerFunction(); });

        ApplySavesCommand = new RelayCommandService(param => { ApplySavesFunction(); });
    }

    public JobManager JobManager { get; }

    public ICommand OpenFilePickerCommand { get; }
    public ICommand ApplySavesCommand { get; }


    public Dictionary<string, string> DictText
    {
        get => _dictText;
        set => SetProperty(ref _dictText, value);
    }

    public string LogPath
    {
        get => _logPath;
        set
        {
            _logPath = value;
            OnPropertyChanged();
        }
    }

    public int SelectedFormatIndex
    {
        get => _selectedFormatIndex;
        set
        {
            if (_selectedFormatIndex != value)
            {
                _selectedFormatIndex = value;
                OnPropertyChanged();

                // Logique de mise à jour immédiate
                UpdateLogFormat(value);
            }
        }
    }

    public bool EnableEncryption
    {
        get => JobManager.EnableEncryption;
        set
        {
            JobManager.EnableEncryption = value;
            OnPropertyChanged();
        }
    }

    public string EncryptionExtensions
    {
        get => JobManager.EncryptionExtensions;
        set
        {
            JobManager.EncryptionExtensions = value;
            OnPropertyChanged();
        }
    }

    public string EncryptionKey
    {
        get => JobManager.EncryptionKey;
        set
        {
            JobManager.EncryptionKey = value;
            OnPropertyChanged();
        }
    }

    public LogType ActualLogType { get; set; }

    public string ListeProcess
    {
        get => _listeProcess;
        set => _listeProcess = value ?? throw new ArgumentNullException(nameof(value));
    }

    private async void OpenFilePickerFunction()
    {
        var topLevel = TopLevel.GetTopLevel(App.MainWindow); // Assure-toi d'avoir une référence à ta fenêtre

        if (topLevel == null) return;

        // Ouvre le sélecteur de dossiers
        var folders = await topLevel.StorageProvider.OpenFolderPickerAsync(new FolderPickerOpenOptions
        {
            Title = "Sélectionner le dossier de logs",
            AllowMultiple = false
        });

        if (folders.Count > 0)
            // On récupère le chemin local du dossier
            LogPath = folders[0].Path.LocalPath;
    }

    private void UpdateLogFormat(int index)
    {
        ActualLogType = index == 1 ? LogType.XML : LogType.JSON;
    }

    private void ApplySavesFunction()
    {
        JobManager.LogType = ActualLogType;
        JobManager.ListeProcess = ListeProcess;

        // Validate encryption key is provided when encryption is enabled
        if (EnableEncryption && string.IsNullOrWhiteSpace(EncryptionKey))
        {
            NotificationService.Instance.Show(
                DictText.ContainsKey("ErrorEncryptionKeyRequired") ? DictText["ErrorEncryptionKeyRequired"] : "Le mot de passe de chiffrement est obligatoire",
                ToastType.Error);
            return;
        }
        
        if (!Directory.Exists(LogPath) && !string.IsNullOrEmpty(LogPath))
        {
            NotificationService.Instance.Show(
                DictText.ContainsKey("ErrorLogPathNotFound") ? DictText["ErrorLogPathNotFound"] : "Le chemin de logs spécifié n'existe pas",
                ToastType.Error);
            return;
        }
        
        try
        {
            ConfigManager.ConfigWritter(LogPath);

            LogService.Observer.StartWatcher();
            
            NotificationService.Instance.Show(
                DictText.ContainsKey("SettingsAppliedMessage") ? DictText["SettingsAppliedMessage"] : "Paramètres appliqués",
                ToastType.Success);
        }
        catch (Exception ex)
        {
            NotificationService.Instance.Show(
                DictText.ContainsKey("ErrorSettingsSave") ? DictText["ErrorSettingsSave"] : $"Erreur lors de la sauvegarde : {ex.Message}",
                ToastType.Error);
        }
    }
}