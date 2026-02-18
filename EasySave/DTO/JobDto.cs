using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using EasySaveLibrary.Model;

namespace EasySave.DTO;

public class JobDto : INotifyPropertyChanged
{
    private string _colorStatus;
    private string _id;
    private bool _isSelected;
    private DateTime? _lastTimeRun;
    private string _name;
    private double _progress;
    private string _save;
    private string _source;
    private string _status;
    private string _target;

    public string Id
    {
        get => _id;
        set => _id = value ?? throw new ArgumentNullException(nameof(value));
    }

    public string Name
    {
        get => _name;
        set
        {
            _name = value ?? throw new ArgumentNullException(nameof(value));
            OnPropertyChanged();
        }
    }

    public string Source
    {
        get => _source;
        set
        {
            _source = value ?? throw new ArgumentNullException(nameof(value));
            OnPropertyChanged();
        }
    }

    public string Target
    {
        get => _target;
        set
        {
            _target = value ?? throw new ArgumentNullException(nameof(value));
            OnPropertyChanged();
        }
    }

    public DateTime? LastTimeRun
    {
        get => _lastTimeRun;
        set
        {
            _lastTimeRun = value;
            OnPropertyChanged();
        }
    }

    public string Save
    {
        get => _save;
        set
        {
            _save = value ?? throw new ArgumentNullException(nameof(value));
            OnPropertyChanged();
        }
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
        set
        {
            _progress = value;
            OnPropertyChanged();
        }
    }

    public string Status
    {
        get => _status;
        set
        {
            _status = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(IsRunning));
            OnPropertyChanged(nameof(IsPaused));
            OnPropertyChanged(nameof(IsNothing));
            OnPropertyChanged(nameof(IsActive));
            OnPropertyChanged(nameof(IsBlocked));
            OnPropertyChanged(nameof(IsPlayDisplay));
            OnPropertyChanged(nameof(ColorStatus));
        }
    }

    // L'état "En cours" (Affiche le bouton Pause)
    public bool IsRunning => Status == "En cours";
    // L'état "En Pause" (Affiche le bouton Reprendre)
    public bool IsPaused => Status == "En Pause";
    public bool IsBlocked => Status == "Bloqué";
    // L'état "Rien du tout" (Affiche le bouton Démarrer une nouvelle save)
    public bool IsNothing => !IsRunning && !IsPaused;
    public bool IsPlayDisplay => !IsRunning && !IsPaused && !IsBlocked;
    // L'état "Actif" (Affiche le bouton Stop)
    public bool IsActive => IsRunning || IsPaused || IsBlocked;    

    public string ColorStatus
    {
        get
        {
            return Status switch
            {
                "En cours" => "Blue",
                "En Pause" => "Orange",
                "Bloqué" => "Red",
                "Terminé" => "#28a745", // Vert succès
                "Cancelled" => "Red",
                "Prêt" => "Green",
                _ => "Gray"
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