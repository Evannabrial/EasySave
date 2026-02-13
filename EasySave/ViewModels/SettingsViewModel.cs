using System;
using System.Collections.Generic;
using EasySaveLibrary;

namespace EasySave.ViewModels;

public class SettingsViewModel : ViewModelBase
{
    private readonly JobManager _jobManager;
    private Dictionary<string, string> _dictText;

    public JobManager JobManager => _jobManager;

    public Dictionary<string, string> DictText
    {
        get => _dictText;
        set => _dictText = value ;
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

    public SettingsViewModel(JobManager jobManager)
    {
        _jobManager = jobManager;
        DictText = jobManager.Language.GetTranslations();
    }
}