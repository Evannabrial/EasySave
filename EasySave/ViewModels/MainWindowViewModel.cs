using System.Collections.Generic;
using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using EasySaveLibrary;
using EasySave.Services;
using EasySaveLibrary.Interfaces;
using EasySaveLibrary.Model;

namespace EasySave.ViewModels;

public partial class MainWindowViewModel : ViewModelBase
{
    private readonly JobManager _jobManager;
    private Dictionary<string, string> _dictText;
    private int _selectedLanguageIndex;

    private ViewModelBase _currentPage;

    // Cette propriété est liée au ContentControl du XAML
    public ViewModelBase CurrentPage
    {
        get => _currentPage;
        set => SetProperty(ref _currentPage, value);
    }

    public void NavJobs() => CurrentPage = new JobsViewModel(_jobManager);
    public void NavStatus() => CurrentPage = new StatusViewModel(_jobManager);
    public void NavSettings() => CurrentPage = new SettingsViewModel(_jobManager);
    public NotificationService Notifications => NotificationService.Instance;
    
    public MainWindowViewModel()
    {
        _jobManager = null; 
    }

    public Dictionary<string, string> DictText
    {
        get => _dictText;
        set => SetProperty(ref _dictText, value);
    }

    public int SelectedLanguageIndex
    {
        get => _selectedLanguageIndex;
        set
        {
            if (SetProperty(ref _selectedLanguageIndex, value))
            {
                ChangeLanguage(value);
            }
        }
    }

    private void ChangeLanguage(int languageIndex)
    {
        ILanguage newLanguage = languageIndex switch
        {
            0 => new French(),
            1 => new English(),
            _ => new French()
        };

        _jobManager.Language = newLanguage;
        DictText = _jobManager.Language.GetTranslations();
        
        string message = languageIndex == 0 ? "Langue : Français" : "Language: English";
        NotificationService.Instance.Show(message);
    }
    
    public MainWindowViewModel(JobManager jobManager)
    {
        _jobManager = jobManager;
        DictText = jobManager.Language.GetTranslations();
        _selectedLanguageIndex = jobManager.Language is French ? 0 : 1;
    }
}
