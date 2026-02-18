using System;
using System.Collections.Generic;
using System.IO;
using System.Windows.Input;
using Avalonia.Controls;
using Avalonia.Platform.Storage;
using EasyLog;
using EasySave.Services;
using EasySaveLibrary;
using ReactiveUI;

namespace EasySave.ViewModels;

public class SettingsViewModel : ViewModelBase
{
    private readonly JobManager _jobManager;
    private Dictionary<string, string> _dictText;
    private string _listeProcess;
    private string _logPath;
    private int _selectedFormatIndex;
    private LogType _actualLogType;
    private double _fileSizeGb;

    

    public ICommand OpenFilePickerCommand { get; }
    public ICommand ApplySavesCommand { get; }


    public double FileSizeMo
    {
        get => _fileSizeGb;
        set => SetProperty(ref _fileSizeGb, value);
    }
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
        get => _jobManager.EnableEncryption;
        set
        {
            _jobManager.EnableEncryption = value;
            OnPropertyChanged();
        }
    }

    public string EncryptionExtensions
    {
        get => _jobManager.EncryptionExtensions;
        set
        {
            _jobManager.EncryptionExtensions = value;
            OnPropertyChanged();
        }
    }

    public LogType ActualLogType { get; set; }

    public string ListeProcess
    {
        get => _listeProcess;
        set => _listeProcess = value ?? throw new ArgumentNullException(nameof(value));
    }

    public SettingsViewModel(JobManager jobManager)
    {
        _jobManager = jobManager;
        DictText = jobManager.Language.GetTranslations();
        ListeProcess = jobManager.ListeProcess;
        LogPath = ConfigManager.LogPath ?? "";
        FileSizeMo = ConfigManager.FileSizeMo;
        
        SelectedFormatIndex = (_jobManager.LogType == LogType.XML) ? 1 : 0;

        OpenFilePickerCommand = new RelayCommandService(param => {
                OpenFilePickerFunction();
        });
        
        ApplySavesCommand = new RelayCommandService(param => {
            ApplySavesFunction();
        });
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
        _jobManager.LogType = ActualLogType;
        _jobManager.ListeProcess = ListeProcess;
        
        if (!Directory.Exists(LogPath) && !string.IsNullOrEmpty(LogPath))
        {
            NotificationService.Instance.Show(
                DictText.ContainsKey("ErrorLogPathNotFound") ? DictText["ErrorLogPathNotFound"] : "Le chemin de logs spécifié n'existe pas",
                ToastType.Error);
            return;
        }
        
        try
        {
            // FileSizeMo est déjà en Mo, on le passe directement
            ConfigManager.ConfigWritter(LogPath, FileSizeMo);
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