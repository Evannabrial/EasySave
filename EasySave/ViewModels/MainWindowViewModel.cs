using System.Collections.Generic;
using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using EasySaveLibrary;
using EasySave.Services;
using EasySaveLibrary.Model;

namespace EasySave.ViewModels;

public partial class MainWindowViewModel : ViewModelBase
{
    private readonly JobManager _jobManager;
    private Dictionary<string, string> _dictText;

    private ViewModelBase _currentPage;

    // Cette propriété est liée au ContentControl du XAML
    public ViewModelBase CurrentPage
    {
        get => _currentPage;
        set => SetProperty(ref _currentPage, value);
    }

    public void NavJobs() => CurrentPage = new JobsViewModel(_jobManager); // Injection de votre manager !
    public void NavStatus() => CurrentPage = new StatusViewModel(_jobManager); // Injection de votre manager !
    public void NavSettings() => CurrentPage = new SettingsViewModel(_jobManager); // Injection de votre manager !
    
    public MainWindowViewModel()
    {
        _jobManager = null; // Ou new JobManager() si possible
    }
    public Dictionary<string, string> DictText
    {
        get => _dictText;
        set => _dictText = value ;
    }
    
    public MainWindowViewModel(JobManager jobManager)
    {
        _jobManager = jobManager;
        DictText = jobManager.Language.GetTranslations();
    }
    
    
}
