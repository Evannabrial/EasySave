using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using EasySaveLibrary.Model;

namespace EasySave.DTO;

public class JobDto : INotifyPropertyChanged
{
    private string _id;
    private string _name;
    private string _source;
    private string _target;
    private DateTime? _lastTimeRun;
    private string _save;
    private bool _isSelected;
    private double _progress;
    private string _status;
    private string _colorStatus;

    public string Id
    {
        get => _id;
        set => _id = value ?? throw new ArgumentNullException(nameof(value));
    }

    public string Name
    {
        get => _name;
        set => _name = value ?? throw new ArgumentNullException(nameof(value));
    }

    public string Source
    {
        get => _source;
        set => _source = value ?? throw new ArgumentNullException(nameof(value));
    }

    public string Target
    {
        get => _target;
        set => _target = value ?? throw new ArgumentNullException(nameof(value));
    }

    public DateTime? LastTimeRun
    {
        get => _lastTimeRun;
        set => _lastTimeRun = value;
    }

    public string Save
    {
        get => _save;
        set => _save = value ?? throw new ArgumentNullException(nameof(value));
    }

    public bool IsSelected
    {
        get => _isSelected;
        set 
        {
            _isSelected = value;
            // Important : Appeler OnPropertyChanged ici pour que l'UI réagisse
            OnPropertyChanged();
        }
    }

    public double Progress
    {
        get => _progress;
        set { _progress = value; OnPropertyChanged(); }
    }

    public string Status
    {
        get => _status;
        set
        {
            _status = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(IsRunning));
            OnPropertyChanged(nameof(ColorStatus));
        }
    }
    
    public bool IsRunning => Status == "En cours";

    public string ColorStatus
    {
        get
        {
            return Status switch
            {
                "En cours" => "Blue",
                "Terminé"  => "#28a745", // Vert succès
                "Prêt"     => "Green",
                _          => "Gray"
            };
        }
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
    
    public JobDto ToDto(Job job)
    {
        Id = job.Id.ToString();
        Name = job.Name;
        Source = job.Source;
        Target = job.Target;
        LastTimeRun = job.LastTimeRun;
        Save = job.Save.GetType().Name;
        Progress = 0;
        Status = "Prêt"; 
        return this;
    }

    public JobDto SetStatus(JobStatus status)
    {
        Progress = status.Progress;
        Status = status.Status;
        
        return this;
    }
}
