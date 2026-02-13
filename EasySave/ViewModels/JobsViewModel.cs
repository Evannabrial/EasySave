using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using Avalonia.Metadata;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Input;
using Avalonia.Threading;
using EasySave.DTO;
using EasySave.Services;
using EasySaveLibrary;
using EasySaveLibrary.Model;

namespace EasySave.ViewModels;

public class JobsViewModel : ViewModelBase
{
    private readonly JobManager _jobManager;
    private Dictionary<string, string> _dictText;
    
    private ObservableCollection<JobDto> _jobs;

    public JobManager JobManager => _jobManager;
    
    public ObservableCollection<JobDto> Jobs
    {
        get => _jobs;
        set => _jobs = value;
    }
    
    public bool ShowMultiSelectButton => Jobs.Count(j => j.IsSelected) >= 2;
    
    private LogObserverService _observer;
    
    public ICommand RunDeleteJob { get; }
    public ICommand RunStartSingleSave { get; }
    
    public Dictionary<string, string> DictText
    {
        get => _dictText;
        set => _dictText = value ;
    }
    public JobsViewModel(JobManager jobManager)
    {
        _jobManager = jobManager;
        DictText = jobManager.Language.GetTranslations();
        RunDeleteJob = new RelayCommandService(param => {
            if (param is JobDto dto) 
            {
                RunDeleteJobFunction(dto);
            }
        });
        
        RunStartSingleSave = new RelayCommandService(param => {
            if (param is JobDto dto) 
            {
                RunSingleJobSave(dto);
            }
        });
        
        _observer = new LogObserverService(_jobManager.LogType.ToString().ToLower());
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
        
        // On s'abonne aux changements de chaque Job
        foreach (var job in Jobs)
        {
            job.PropertyChanged += (s, e) => {
                if (e.PropertyName == nameof(JobDto.IsSelected))
                    OnPropertyChanged(nameof(ShowMultiSelectButton));
            };
        }
    }

    private void RunSingleJobSave(JobDto jobDto)
    {
        int index = Jobs.IndexOf(jobDto); 

        Task.Run(() => 
        {
            _jobManager.StartMultipleSave(index.ToString());
            
            Dispatcher.UIThread.InvokeAsync(() => {
                RefreshJobsStatus();
            });
        });
    }
    
    private void RunDeleteJobFunction(JobDto dto)
    {
        if (dto == null) return;

        // 1. Trouver le modèle original dans la bibliothèque via l'ID
        Job jobModel = _jobManager.LJobs.FirstOrDefault(j => j.Id.ToString() == dto.Id);

        if (jobModel != null)
        {
            // 2. Supprimer dans le modèle (la logique métier)
            _jobManager.DeleteJob(jobModel);

            // 3. Supprimer dans la collection de la vue (l'UI)
            Jobs.Remove(dto);
        
            // Optionnel : notifier pour le bouton multi-select si besoin
            OnPropertyChanged(nameof(ShowMultiSelectButton));
        }
    }
    
    private void RefreshJobsStatus()
    {
        foreach (var jobDto in Jobs)
        {
            // On repasse par le manager pour lire le fichier désérialisé
            var status = _jobManager.GetStatusOfJob(Guid.Parse(jobDto.Id));
            if (status != null)
            {
                jobDto.Progress = (int)status.Progress;
                jobDto.Status = status.Status;
                
                // La vue se mettra à jour car JobDto doit implémenter INotifyPropertyChanged
            }
        }
    }
}