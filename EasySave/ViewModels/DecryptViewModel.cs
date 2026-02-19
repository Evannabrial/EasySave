using EasySaveLibrary;

namespace EasySave.ViewModels;

public class DecryptViewModel : ViewModelBase
{
    private readonly JobManager _jobManager;
    
    public DecryptViewModel(JobManager jobManager)
    {
        _jobManager = jobManager;
    }
}