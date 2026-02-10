using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using EasySaveLibrary;
using EasySave.Services;
using EasySaveLibrary.Model;

namespace EasySave.ViewModels;

public partial class MainWindowViewModel : ViewModelBase
{
    private readonly JobManager _jobManager;

    // On expose la liste pour que la Vue puisse la voir
    public ObservableCollection<Job> Jobs { get; set; }

    public MainWindowViewModel(JobManager jobManager)
    {
        _jobManager = jobManager;
        
        // On synchronise ou on référence la liste du modèle
        Jobs = new ObservableCollection<Job>(_jobManager.LJobs);
    }
}
