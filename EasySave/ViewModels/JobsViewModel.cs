using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using Avalonia.Metadata;
using System.Linq;
using System.Windows.Input;
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
    
    public ICommand RunDeleteJob { get; }
    
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
        
        
        ObservableCollection<JobDto> jobDtos = new ObservableCollection<JobDto>();
        
        foreach (var j in jobManager.LJobs)
        {
            jobDtos.Add(new JobDto().ToDto(j));
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
    
    
    private void RunDeleteJobFunction(JobDto dto)
    {
        if (dto == null) return;

        // 1. Trouver le modèle original dans la bibliothèque via l'ID
        var jobModel = _jobManager.LJobs.FirstOrDefault(j => j.Id.ToString() == dto.Id);

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
}