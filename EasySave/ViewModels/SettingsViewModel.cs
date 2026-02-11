using EasySaveLibrary;

namespace EasySave.ViewModels;

public class SettingsViewModel : ViewModelBase
{
    private readonly JobManager _jobManager;

    public SettingsViewModel(JobManager jobManager)
    {
        _jobManager = jobManager;
    }
}