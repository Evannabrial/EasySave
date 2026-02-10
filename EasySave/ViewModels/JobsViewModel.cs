using EasySaveLibrary;

namespace EasySave.ViewModels;

public class JobsViewModel : ViewModelBase
{
    private readonly JobManager _jobManager;
    
    public JobsViewModel(JobManager jobManager)
    {
        _jobManager = jobManager;
    }
}