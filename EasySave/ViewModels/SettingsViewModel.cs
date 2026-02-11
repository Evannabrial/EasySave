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

    public SettingsViewModel(JobManager jobManager)
    {
        _jobManager = jobManager;
        DictText = jobManager.Language.GetTranslations();

    }
}