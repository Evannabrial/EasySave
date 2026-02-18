using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Input;
using Avalonia.Controls;
using Avalonia.Platform.Storage;
using Avalonia.Threading;
using EasySave.DTO;
using EasySave.Services;
using EasySaveLibrary;
using EasySaveLibrary.Model;
using EasySaveLibrary.Interfaces;

namespace EasySave.ViewModels;

public class JobsViewModel : ViewModelBase
{
    private readonly JobManager _jobManager;
    private Dictionary<string, string> _dictText;
    
    private ObservableCollection<JobDto> _jobs;
    private LogObserverService _observer;

    // --- Propriétés pour le Formulaire (Pop-up) ---
    private bool _isFormVisible;
    private string _formTitle;
    private string _formName;
    private string _formSource;
    private string _formTarget;
    private string _selectedSaveType;
    private bool _isEditMode;
    private JobDto _currentEditingDto; // Pour garder une référence au DTO en cours d'édition

    public ObservableCollection<string> SaveTypes { get; } = new ObservableCollection<string> { "Full", "Differential" };

    public bool IsFormVisible
    {
        get => _isFormVisible;
        set { _isFormVisible = value; OnPropertyChanged(); }
    }

    public string FormTitle
    {
        get => _formTitle;
        set { _formTitle = value; OnPropertyChanged(); }
    }

    public string FormName
    {
        get => _formName;
        set { _formName = value; OnPropertyChanged(); }
    }

    public string FormSource
    {
        get => _formSource;
        set { _formSource = value; OnPropertyChanged(); }
    }

    public string FormTarget
    {
        get => _formTarget;
        set { _formTarget = value; OnPropertyChanged(); }
    }

    public string SelectedSaveType
    {
        get => _selectedSaveType;
        set { _selectedSaveType = value; OnPropertyChanged(); }
    }

    // --- Fin Propriétés Formulaire ---

    public JobManager JobManager => _jobManager;
    
    public ObservableCollection<JobDto> Jobs
    {
        get => _jobs;
        set => _jobs = value;
    }
    
    public Dictionary<string, string> DictText
    {
        get => _dictText;
        set 
        { 
            _dictText = value; 
            OnPropertyChanged();
        }    
    }
    
    public bool ShowMultiSelectButton => Jobs.Count(j => j.IsSelected) >= 2;
    
    public ICommand RunDeleteJob { get; }
    public ICommand RunStartSingleSave { get; }
    public ICommand OpenFilePickerSourceCommand { get; }
    public ICommand OpenFilePickerTargetCommand { get; }

    
    // Commandes pour le Formulaire
    public ICommand OpenAddJobCommand { get; }
    public ICommand OpenEditJobCommand { get; }
    public ICommand CloseFormCommand { get; }
    public ICommand SaveFormCommand { get; }

    public JobsViewModel(JobManager jobManager)
    {
        _jobManager = jobManager;
        DictText = jobManager.Language.GetTranslations();
        
        // Initialisation des commandes existantes
        RunDeleteJob = new RelayCommandService(param => {
            if (param is JobDto dto) RunDeleteJobFunction(dto);
        });
        
        RunStartSingleSave = new RelayCommandService(param => {
            if (param is JobDto dto) RunSingleJobSave(dto);
        });
        
        OpenFilePickerSourceCommand = new RelayCommandService(param => {
            OpenFilePickerSourceFunction();
        });
        
        OpenFilePickerTargetCommand = new RelayCommandService(param => {
            OpenFilePickerTargetFunction();
        });

        // --- Initialisation des commandes du Formulaire ---
        
        // Ouvre le popup en mode "Ajout"
        OpenAddJobCommand = new RelayCommandService(_ => {
            _isEditMode = false;
            _currentEditingDto = null;
            
            FormTitle = DictText.ContainsKey("AddJobMessage") ? DictText["AddJobMessage"] : "Add a Job";
            FormName = "";
            FormSource = "";
            FormTarget = "";
            SelectedSaveType = "Full"; // Valeur par défaut
            
            IsFormVisible = true;
        });

        // Ouvre le popup en mode "Édition"
        OpenEditJobCommand = new RelayCommandService(param => {
            if (param is JobDto dto)
            {
                _isEditMode = true;
                _currentEditingDto = dto;

                FormTitle = "Edit Job"; // Ou via DictText si dispo
                FormName = dto.Name;
                FormSource = dto.Source;
                FormTarget = dto.Target;
                
                // On s'assure que le type correspond à la liste (Full/Differential)
                SelectedSaveType = SaveTypes.Contains(dto.Save) ? dto.Save : "Full";

                IsFormVisible = true;
            }
        });

        LogService.Observer.OnLogChanged += () => 
        {
            Dispatcher.UIThread.InvokeAsync(RefreshJobsStatus);
        };

        // Ferme le popup sans sauvegarder
        CloseFormCommand = new RelayCommandService(_ => {
            IsFormVisible = false;
        });

        // Sauvegarde (Ajout ou Update)
        SaveFormCommand = new RelayCommandService(_ => {
            ProcessSaveForm();
        });

        // --- Fin Commandes Formulaire ---

        _observer = new LogObserverService();
        _observer.OnLogChanged += () => 
        {
            // On demande au Dispatcher d'Avalonia d'exécuter la mise à jour
            Dispatcher.UIThread.InvokeAsync(RefreshJobsStatus);
        };
        
        ObservableCollection<JobDto> jobDtos = new ObservableCollection<JobDto>();
        
        foreach (var j in jobManager.LJobs)
        {
            JobDto jobDto = new JobDto().ToDto(j);
            var currentStatus = _jobManager.GetStatusOfJob(j.Id);
    
            // On ne met à jour le statut que si on a trouvé un log correspondant
            if (currentStatus != null)
            {
                jobDto.SetStatus(currentStatus);
            }
    
            jobDtos.Add(jobDto);
        }

        Jobs = jobDtos;
        
        foreach (var job in Jobs)
        {
            job.PropertyChanged += (s, e) => {
                if (e.PropertyName == nameof(JobDto.IsSelected))
                    OnPropertyChanged(nameof(ShowMultiSelectButton));
            };
        }
    }

    private void ProcessSaveForm()
    {
        // 1. Validation basique
        if (string.IsNullOrWhiteSpace(FormName) || 
            string.IsNullOrWhiteSpace(FormSource) || 
            string.IsNullOrWhiteSpace(FormTarget))
        {
            NotificationService.Instance.Show(
                DictText.ContainsKey("ErrorInvalidInput") ? DictText["ErrorInvalidInput"] : "Entrée invalide, veuillez réessayer.",
                ToastType.Error);
            return;
        }

        // Création du type de sauvegarde (Model)
        ITypeSave typeSave = SelectedSaveType == "Differential" ? new Differential() : new Full();

        if (_isEditMode && _currentEditingDto != null)
        {
            // --- MODE ÉDITION ---
            
            // Trouver le vrai Job dans le Manager
            var jobModel = _jobManager.LJobs.FirstOrDefault(j => j.Id.ToString() == _currentEditingDto.Id);
            if (jobModel != null)
            {
                // Mise à jour dans le Manager (Logique Métier)
                _jobManager.UpdateJob(jobModel, FormName, FormSource, FormTarget, typeSave);
                _jobManager.SaveJobs(); // Sauvegarde JSON

                // Mise à jour de l'UI (DTO)
                _currentEditingDto.Name = FormName;
                _currentEditingDto.Source = FormSource.TrimEnd('\\');
                _currentEditingDto.Target = FormTarget.TrimEnd('\\');
                _currentEditingDto.Save = SelectedSaveType;
                

                NotificationService.Instance.Show(DictText.ContainsKey("JobModifiedMessage") ? DictText["JobModifiedMessage"] : "Job modifié");
            }
        }
        else
        {
            // --- MODE AJOUT ---
            
            // Ajout dans le Manager
            Job newJob = _jobManager.AddJob(FormName.TrimEnd('\\'), FormSource.TrimEnd('\\'), FormTarget, typeSave);
            _jobManager.SaveJobs(); // Sauvegarde JSON

            // Création du DTO pour l'UI
            JobDto newDto = new JobDto().ToDto(newJob);
            
            // Abonnement pour le bouton multiselect
            newDto.PropertyChanged += (s, e) => {
                if (e.PropertyName == nameof(JobDto.IsSelected))
                    OnPropertyChanged(nameof(ShowMultiSelectButton));
            };
            _jobManager.SaveJobs();
            Jobs.Add(newDto);
            
            NotificationService.Instance.Show(DictText.ContainsKey("JobAddedMessage") ? DictText["JobAddedMessage"] : "Job ajouté");
        }

        // Fermer le formulaire
        IsFormVisible = false;
    }

    private void RunSingleJobSave(JobDto jobDto)
    {
        LogService.Observer.StartWatcher();
        int index = Jobs.IndexOf(jobDto); 

        Task.Run(() => 
        {
            _jobManager.StartMultipleSave(index.ToString());
            
            Dispatcher.UIThread.InvokeAsync(() => {
                RefreshJobsStatus();
                NotificationService.Instance.Show(DictText.ContainsKey("JobDoneMessage") ? DictText["JobDoneMessage"] : "Sauvegarde terminée");
            });
        });
    }
    
    private void RunDeleteJobFunction(JobDto dto)
    {
        if (dto == null) return;

        Job jobModel = _jobManager.LJobs.FirstOrDefault(j => j.Id.ToString() == dto.Id);

        if (jobModel != null)
        {
            _jobManager.DeleteJob(jobModel);
            _jobManager.SaveJobs(); // Penser à sauvegarder la suppression dans le JSON aussi
            Jobs.Remove(dto);
            OnPropertyChanged(nameof(ShowMultiSelectButton));
            
            NotificationService.Instance.Show(DictText.ContainsKey("JobDeletedMessage") ? DictText["JobDeletedMessage"] : "Job supprimé");
        }
    }
    
    private void RefreshJobsStatus()
    {
        foreach (var jobDto in Jobs)
        {
            var status = _jobManager.GetStatusOfJob(Guid.Parse(jobDto.Id));
            if (status != null)
            {
                jobDto.Progress = (int)status.Progress;
                jobDto.Status = status.Status;
            }
        }
    }
    
    private async void OpenFilePickerTargetFunction()
    {
        var topLevel = TopLevel.GetTopLevel(App.MainWindow); // Assure-toi d'avoir une référence à ta fenêtre

        if (topLevel == null) return;

        // Ouvre le sélecteur de dossiers
        var folders = await topLevel.StorageProvider.OpenFolderPickerAsync(new FolderPickerOpenOptions
        {
            Title = "Sélectionner le dossier de logs",
            AllowMultiple = false
        });

        if (folders.Count > 0)
        {
            // On récupère le chemin local du dossier
            FormTarget = folders[0].Path.LocalPath;
        }
    }
    
    private async void OpenFilePickerSourceFunction()
    {
        var topLevel = TopLevel.GetTopLevel(App.MainWindow); // Assure-toi d'avoir une référence à ta fenêtre

        if (topLevel == null) return;

        // Ouvre le sélecteur de dossiers
        var folders = await topLevel.StorageProvider.OpenFolderPickerAsync(new FolderPickerOpenOptions
        {
            Title = "Sélectionner le dossier de source",
            AllowMultiple = false
        });

        if (folders.Count > 0)
        {
            // On récupère le chemin local du dossier
            FormSource = folders[0].Path.LocalPath;
        }
    }
}