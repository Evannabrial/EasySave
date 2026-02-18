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
using EasySaveLibrary.Interfaces;
using EasySaveLibrary.Model;

namespace EasySave.ViewModels;

public class JobsViewModel : ViewModelBase
{
    private readonly LogObserverService _observer;
    private JobDto _currentEditingDto; // Pour garder une référence au DTO en cours d'édition
    private Dictionary<string, string> _dictText;
    private string _formName;
    private string _formSource;
    private string _formTarget;
    private string _formTitle;
    private bool _isEditMode;
    private bool _isUpdateScheduled = false;
    // --- Propriétés pour le Formulaire (Pop-up) ---
    private bool _isFormVisible;
    private string _selectedSaveType;
    
    
    public string FormName
    {
        get => _formName;
        set
        {
            _formName = value;
            OnPropertyChanged();
        }
    }

    public string FormSource
    {
        get => _formSource;
        set
        {
            _formSource = value.TrimEnd('\\');
            OnPropertyChanged();
        }
    }

    public string FormTarget
    {
        get => _formTarget;
        set
        {
            _formTarget = value.TrimEnd('\\');
            OnPropertyChanged();
        }
    }

    public string SelectedSaveType
    {
        get => _selectedSaveType;
        set
        {
            _selectedSaveType = value;
            OnPropertyChanged();
        }
    }

    // --- Fin Propriétés Formulaire ---

    public JobManager JobManager { get; }

    public ObservableCollection<string> SaveTypes { get; } = new() { "Full", "Differential" };

    public bool ShowMultiSelectButton => Jobs.Count(j => j.IsSelected) >= 2;

    public ObservableCollection<JobDto> Jobs { get; set; }

    public Dictionary<string, string> DictText
    {
        get => _dictText;
        set
        {
            _dictText = value;
            OnPropertyChanged();
        }
    }
    
    public bool IsFormVisible
    {
        get => _isFormVisible;
        set
        {
            _isFormVisible = value;
            OnPropertyChanged();
        }
    }

    public string FormTitle
    {
        get => _formTitle;
        set
        {
            _formTitle = value;
            OnPropertyChanged();
        }
    }

    public ICommand RunDeleteJob { get; }
    public ICommand RunStartSingleSave { get; }
    public ICommand OpenFilePickerSourceCommand { get; }
    public ICommand OpenFilePickerTargetCommand { get; }
    public ICommand RunMultipleSaveCommand { get; }
    public ICommand PauseJobCommand { get; }
    public ICommand ResumeJobCommand { get; }

    // Commandes pour le Formulaire
    public ICommand OpenAddJobCommand { get; }
    public ICommand OpenEditJobCommand { get; }
    public ICommand CloseFormCommand { get; }
    public ICommand SaveFormCommand { get; }

    public JobsViewModel(JobManager jobManager)
    {
        JobManager = jobManager;
        DictText = jobManager.Language.GetTranslations();

        // Initialisation des commandes existantes
        RunDeleteJob = new RelayCommandService(param =>
        {
            if (param is JobDto dto) RunDeleteJobFunction(dto);
        });

        RunStartSingleSave = new RelayCommandService(param =>
        {
            if (param is JobDto dto) RunSingleJobSave(dto);
        });

        RunMultipleSaveCommand = new RelayCommandService(param => { RunMultipleSaveFunction(); });

        OpenFilePickerSourceCommand = new RelayCommandService(param => { OpenFilePickerSourceFunction(); });

        OpenFilePickerTargetCommand = new RelayCommandService(param => { OpenFilePickerTargetFunction(); });
        
        PauseJobCommand = new RelayCommandService(param => {
            if (param is JobDto dto)
            {
                JobManager.PauseJob(Guid.Parse(dto.Id));
                // Change le statut visuel si tu veux, 
                // ou attends que le LogObserver le fasse (mais le log ne change pas pendant la pause)
                dto.Status = "En Pause"; 
            }
        });

        ResumeJobCommand = new RelayCommandService(param => {
            if (param is JobDto dto)
            {
                JobManager.ResumeJob(Guid.Parse(dto.Id));
                dto.Status = "En cours";
            }
        });

        // --- Initialisation des commandes du Formulaire ---

        // Ouvre le popup en mode "Ajout"
        OpenAddJobCommand = new RelayCommandService(_ =>
        {
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
        OpenEditJobCommand = new RelayCommandService(param =>
        {
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

        // LogService.Observer.OnLogChanged += () => { Dispatcher.UIThread.InvokeAsync(RefreshJobsStatus); };

        // Ferme le popup sans sauvegarder
        CloseFormCommand = new RelayCommandService(_ => { IsFormVisible = false; });

        // Sauvegarde (Ajout ou Update)
        SaveFormCommand = new RelayCommandService(_ => { ProcessSaveForm(); });

        // --- Fin Commandes Formulaire ---

        LogService.Observer.OnLogChanged += ScheduleRefresh;

        var jobDtos = new ObservableCollection<JobDto>();

        foreach (var j in jobManager.LJobs)
        {
            var jobDto = new JobDto().ToDto(j);
            var currentStatus = JobManager.GetStatusOfJob(j.Id);

            // On ne met à jour le statut que si on a trouvé un log correspondant
            if (currentStatus != null) jobDto.SetStatus(currentStatus);

            jobDtos.Add(jobDto);
        }

        Jobs = jobDtos;

        foreach (var job in Jobs)
            job.PropertyChanged += (s, e) =>
            {
                if (e.PropertyName == nameof(JobDto.IsSelected))
                    OnPropertyChanged(nameof(ShowMultiSelectButton));
            };
    }
    
    // On change la méthode pour qu'elle prépare le travail en arrière-plan
    private void ScheduleRefresh()
    {
        if (_isUpdateScheduled) return;
        _isUpdateScheduled = true;

        // On attend 100ms pour ne pas spammer (Throttling)
        Task.Delay(100).ContinueWith(async _ =>
        {
            try
            {
                // Etape 1 : On capture la liste des IDs sur le thread UI (très rapide)
                // On fait ça pour ne pas toucher à la collection 'Jobs' depuis un autre thread
                var jobIds = new List<string>();
                await Dispatcher.UIThread.InvokeAsync(() => 
                {
                    jobIds = Jobs.Select(j => j.Id).ToList();
                });

                // Etape 2 : LE TRAVAIL LOURD (Lecture Disque)
                // On lance ça sur un thread séparé (Task.Run). L'UI reste fluide pendant ce temps.
                var updates = await Task.Run(() =>
                {
                    var listUpdates = new List<(string Id, JobStatus Status)>();
                    
                    foreach (var id in jobIds)
                    {
                        // C'est ICI que ça bloquait ton interface avant (lecture fichier)
                        // Maintenant c'est fait en cachette
                        var status = JobManager.GetStatusOfJob(Guid.Parse(id));
                        if (status != null)
                        {
                            if (JobManager.IsJobPaused(Guid.Parse(id)))
                            {
                                status.Status = "En Pause";
                            }
                            
                            listUpdates.Add((id, status));
                        }
                    }
                    return listUpdates;
                });

                // Etape 3 : MISE A JOUR UI
                // On revient sur le thread principal juste pour appliquer les valeurs
                await Dispatcher.UIThread.InvokeAsync(() =>
                {
                    foreach (var update in updates)
                    {
                        var jobDto = Jobs.FirstOrDefault(j => j.Id == update.Id);
                        if (jobDto != null)
                        {
                            jobDto.Progress = (int)update.Status.Progress;
                            // On ne change le status que s'il est différent pour éviter des clignotements
                            if(jobDto.Status != update.Status.Status)
                            {
                                jobDto.Status = update.Status.Status;
                            }
                        }
                    }
                });
            }
            catch (Exception ex)
            {
                // En cas d'erreur de lecture, on ignore pour cette frame
                Console.WriteLine(ex.Message);
            }
            finally
            {
                _isUpdateScheduled = false;
            }
        });
    }

    private void ProcessSaveForm()
    {
        // 1. Validation basique
        if (string.IsNullOrWhiteSpace(FormName) ||
            string.IsNullOrWhiteSpace(FormSource) ||
            string.IsNullOrWhiteSpace(FormTarget))
            // Ici on pourrait afficher une erreur, pour l'instant on ne fait rien
            return;

        // Création du type de sauvegarde (Model)
        ITypeSave typeSave = SelectedSaveType == "Differential" ? new Differential() : new Full();

        if (_isEditMode && _currentEditingDto != null)
        {
            // --- MODE ÉDITION ---

            // Trouver le vrai Job dans le Manager
            var jobModel = JobManager.LJobs.FirstOrDefault(j => j.Id.ToString() == _currentEditingDto.Id);
            if (jobModel != null)
            {
                // Mise à jour dans le Manager (Logique Métier)
                JobManager.UpdateJob(jobModel, FormName, FormSource, FormTarget, typeSave);
                JobManager.SaveJobs(); // Sauvegarde JSON

                // Mise à jour de l'UI (DTO)
                _currentEditingDto.Name = FormName;
                _currentEditingDto.Source = FormSource;
                _currentEditingDto.Target = FormTarget;
                _currentEditingDto.Save = SelectedSaveType;
            }
        }
        else
        {
            // --- MODE AJOUT ---

            // Ajout dans le Manager
            var newJob = JobManager.AddJob(FormName, FormSource, FormTarget, typeSave);
            JobManager.SaveJobs(); // Sauvegarde JSON

            // Création du DTO pour l'UI
            var newDto = new JobDto().ToDto(newJob);

            // Abonnement pour le bouton multiselect
            newDto.PropertyChanged += (s, e) =>
            {
                if (e.PropertyName == nameof(JobDto.IsSelected))
                    OnPropertyChanged(nameof(ShowMultiSelectButton));
            };
            JobManager.SaveJobs();
            Jobs.Add(newDto);
        }

        // Fermer le formulaire
        IsFormVisible = false;
    }

    private void RunSingleJobSave(JobDto jobDto)
    {
        LogService.Observer.StartWatcher();
        var index = Jobs.IndexOf(jobDto);

        JobManager.StartMultipleSave(index.ToString());

        // Dispatcher.UIThread.InvokeAsync(() => { RefreshJobsStatus(); });
    }

    private void RunDeleteJobFunction(JobDto dto)
    {
        if (dto == null) return;

        var jobModel = JobManager.LJobs.FirstOrDefault(j => j.Id.ToString() == dto.Id);

        if (jobModel != null)
        {
            JobManager.DeleteJob(jobModel);
            JobManager.SaveJobs(); // Penser à sauvegarder la suppression dans le JSON aussi
            Jobs.Remove(dto);
            OnPropertyChanged(nameof(ShowMultiSelectButton));
        }
    }

    private void RefreshJobsStatus()
    {
        // foreach (var jobDto in Jobs)
        // {
        //     var status = JobManager.GetStatusOfJob(Guid.Parse(jobDto.Id));
        //     if (status != null)
        //     {
        //         jobDto.Progress = (int)status.Progress;
        //         jobDto.Status = status.Status;
        //     }
        // }
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
            // On récupère le chemin local du dossier
            FormTarget = folders[0].Path.LocalPath;
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
            // On récupère le chemin local du dossier
            FormSource = folders[0].Path.LocalPath;
    }

    private void RunMultipleSaveFunction()
    {
        var selectedIndexes = Jobs
            .Select((job, index) => new { Job = job, Index = index })
            .Where(x => x.Job.IsSelected)
            .Select(x => x.Index)
            .ToList();

        var userChoice = string.Join(";", selectedIndexes);
        LogService.Observer.StartWatcher();


        JobManager.StartMultipleSave(userChoice);

        // Dispatcher.UIThread.InvokeAsync(() => { RefreshJobsStatus(); });
    }
}