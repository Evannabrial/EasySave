using System;
using System.Collections.ObjectModel;
using Avalonia.Metadata;
using EasySaveLibrary;
using EasySaveLibrary.Model;

namespace EasySave.ViewModels;

public class JobsViewModel : ViewModelBase
{
    private readonly JobManager _jobManager;
    
    private ObservableCollection<Job> _jobs;

    public JobManager JobManager => _jobManager;

    public ObservableCollection<Job> Jobs
    {
        get => _jobs;
        set => _jobs = value;
    }

    public JobsViewModel(JobManager jobManager)
    {
        _jobManager = jobManager;
        Jobs = new ObservableCollection<Job>(_jobManager.LJobs);
        
    }
}