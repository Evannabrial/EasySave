using System;
using System.Collections.Generic;
using System.IO;
using System.Windows.Input;
using Avalonia.Controls;
using Avalonia.Platform.Storage;
using EasyLog;
using EasySave.DTO;
using EasySave.Services;
using EasySaveLibrary;

namespace EasySave.ViewModels;

public class SettingsViewModel : ViewModelBase
{
    private readonly JobManager _jobManager;
    private Dictionary<string, string> _dictText;
    private string _logPath;
    private int _selectedFormatIndex;
    private LogType _actualLogType;

    public JobManager JobManager => _jobManager;
    
    public ICommand OpenFilePickerCommand { get; }
    public ICommand ApplySavesCommand { get; }


    public Dictionary<string, string> DictText
    {
        get => _dictText;
        set => _dictText = value ;
    }
    
    public string LogPath
    {
        get => _logPath;
        set { _logPath = value; OnPropertyChanged(); }
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

    public LogType ActualLogType
    {
        get => _actualLogType;
        set => _actualLogType = value;
    }

    public SettingsViewModel(JobManager jobManager)
    {
        _jobManager = jobManager;
        DictText = jobManager.Language.GetTranslations();
        
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
        {
            // On récupère le chemin local du dossier
            LogPath = folders[0].Path.LocalPath;
        }
    }

    private void UpdateLogFormat(int index)
    {
        ActualLogType = (index == 1) ? LogType.XML : LogType.JSON;
    }
    
    private void ApplySavesFunction()
    {
        _jobManager.LogType = ActualLogType;
        if (Directory.Exists(LogPath))
        {
            ConfigManager.ConfigWritter(LogPath);
            
            LogService.Observer.StartWatcher();
        }
    }
}
