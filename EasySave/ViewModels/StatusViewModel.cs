using EasySaveLibrary;

namespace EasySave.ViewModels;

public class StatusViewModel : ViewModelBase
{
    private readonly JobManager _jobManager;
    
    public StatusViewModel(JobManager jobManager)
    {
        _jobManager = jobManager;
    }
}