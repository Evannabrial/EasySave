using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Windows.Input;
using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.Platform.Storage;
using EasyLog;
using EasySave.Services;
using EasySaveLibrary;
using ReactiveUI;

namespace EasySave.ViewModels;

public class SettingsViewModel : ViewModelBase
{
    private readonly JobManager _jobManager;
    private Dictionary<string, string> _dictText;
    private string _listeProcess;
    private string _logPath;
    private int _selectedFormatIndex;
    private LogType _actualLogType;
    private double _fileSizeGb;
    private string _extensionsPrio;
    private int _selectedThemeIndex;
    private string _customPrimaryColor;
    private string _customSecondaryColor;
    

    public ICommand OpenFilePickerCommand { get; }
    public ICommand ApplySavesCommand { get; }
    public ICommand ApplyCustomColorsCommand { get; }

    public LogType ActualLogType { get; set; }
    
    // Collection des thèmes disponibles
    public ObservableCollection<ThemePreset> AvailableThemes { get; }

    public int SelectedThemeIndex
    {
        get => _selectedThemeIndex;
        set
        {
            if (_selectedThemeIndex != value)
            {
                _selectedThemeIndex = value;
                OnPropertyChanged();
                
                // Appliquer le thème sélectionné immédiatement
                if (value >= 0 && value < AvailableThemes.Count)
                {
                    var theme = AvailableThemes[value];
                    ThemeService.Instance.ApplyTheme(theme);
                    CustomPrimaryColor = theme.PrimaryColor;
                    CustomSecondaryColor = theme.SecondaryColor;
                }
            }
        }
    }
    
    public string CustomPrimaryColor
    {
        get => _customPrimaryColor;
        set
        {
            _customPrimaryColor = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(PreviewPrimaryBrush));
        }
    }
    
    public string CustomSecondaryColor
    {
        get => _customSecondaryColor;
        set
        {
            _customSecondaryColor = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(PreviewSecondaryBrush));
            OnPropertyChanged(nameof(PreviewTextBrush));
        }
    }
    
    // Propriétés Brush pour l'aperçu (lecture seule)
    public IBrush PreviewPrimaryBrush
    {
        get
        {
            try
            {
                return new SolidColorBrush(Color.Parse(CustomPrimaryColor ?? "#F5B800"));
            }
            catch
            {
                return new SolidColorBrush(Color.Parse("#F5B800"));
            }
        }
    }
    
    public IBrush PreviewSecondaryBrush
    {
        get
        {
            try
            {
                return new SolidColorBrush(Color.Parse(CustomSecondaryColor ?? "#F5F5DC"));
            }
            catch
            {
                return new SolidColorBrush(Color.Parse("#F5F5DC"));
            }
        }
    }
    
    public IBrush PreviewTextBrush
    {
        get
        {
            try
            {
                var bgColor = Color.Parse(CustomSecondaryColor ?? "#F5F5DC");
                // Calculer la luminosité pour déterminer si le texte doit être noir ou blanc
                double luminance = (0.299 * bgColor.R + 0.587 * bgColor.G + 0.114 * bgColor.B) / 255;
                return luminance > 0.5 ? new SolidColorBrush(Colors.Black) : new SolidColorBrush(Colors.White);
            }
            catch
            {
                return new SolidColorBrush(Colors.Black);
            }
        }
    }

    public double FileSizeMo
    {
        get => _fileSizeGb;
        set => SetProperty(ref _fileSizeGb, value);
    }
    public Dictionary<string, string> DictText
    {
        get => _dictText;
        set => SetProperty(ref _dictText, value);
    }

    public string LogPath
    {
        get => _logPath;
        set
        {
            _logPath = value;
            OnPropertyChanged();
        }
    }

    public int SelectedFormatIndex
    {
        get => _selectedFormatIndex;
        set
        {
            if (_selectedFormatIndex != value)
            {
                _selectedFormatIndex = value;
                OnPropertyChanged();

                // Logique de mise à jour immédiate
                UpdateLogFormat(value);
            }
        }
    }

    public bool EnableEncryption
    {
        get => _jobManager.EnableEncryption;
        set
        {
            _jobManager.EnableEncryption = value;
            OnPropertyChanged();
        }
    }

    public string EncryptionExtensions
    {
        get => _jobManager.EncryptionExtensions;
        set
        {
            _jobManager.EncryptionExtensions = value;
            OnPropertyChanged();
        }
    }

    public string EncryptionKey
    {
        get => _jobManager.EncryptionKey;
        set
        {
            _jobManager.EncryptionKey = value;
            OnPropertyChanged();
        }
    }
    
    public string ListeProcess
    {
        get => _listeProcess;
        set => _listeProcess = value ?? throw new ArgumentNullException(nameof(value));
    }

    public string ExtensionsPrio
    {
        get => _extensionsPrio;
        set => _extensionsPrio = value ?? throw new ArgumentNullException(nameof(value));
    }


    public SettingsViewModel(JobManager jobManager)
    {
        _jobManager = jobManager;
        DictText = jobManager.Language.GetTranslations();
        ListeProcess = jobManager.ListeProcess;
        LogPath = ConfigManager.LogPath ?? "";
        FileSizeMo = ConfigManager.FileSizeKo;
        
        // Initialisation des thèmes
        AvailableThemes = new ObservableCollection<ThemePreset>(ThemeService.Instance.AvailableThemes);
        CustomPrimaryColor = ThemeService.Instance.CurrentPrimaryColor;
        CustomSecondaryColor = ThemeService.Instance.CurrentSecondaryColor;
        
        // Trouver le thème actuel dans la liste
        _selectedThemeIndex = FindCurrentThemeIndex();
        
        string extFile = "";
        if (jobManager.PrioFilesExtension != null && jobManager.PrioFilesExtension.Count > 0)
        {
            foreach (string ext in jobManager.PrioFilesExtension)
            {
                extFile += ext + ",";
            }
        }
        
        ExtensionsPrio = extFile;
        
        SelectedFormatIndex = (_jobManager.LogType == LogType.XML) ? 1 : 0;

        OpenFilePickerCommand = new RelayCommandService(param => {
                OpenFilePickerFunction();
        });
        
        ApplySavesCommand = new RelayCommandService(param => {
            ApplySavesFunction();
        });
        
        ApplyCustomColorsCommand = new RelayCommandService(param => {
            ApplyCustomColors();
        });
    }
    
    private int FindCurrentThemeIndex()
    {
        var currentPrimary = ThemeService.Instance.CurrentPrimaryColor;
        for (int i = 0; i < AvailableThemes.Count; i++)
        {
            if (AvailableThemes[i].PrimaryColor.Equals(currentPrimary, StringComparison.OrdinalIgnoreCase))
                return i;
        }
        return 0; // Défaut sur le premier thème
    }
    
    private void ApplyCustomColors()
    {
        try
        {
            // Générer automatiquement une couleur hover plus foncée
            var primaryColor = Color.Parse(CustomPrimaryColor);
            var hoverColor = Color.FromRgb(
                (byte)(primaryColor.R * 0.7),
                (byte)(primaryColor.G * 0.7),
                (byte)(primaryColor.B * 0.7)
            );
            
            ThemeService.Instance.ApplyColors(
                CustomPrimaryColor, 
                hoverColor.ToString(),
                CustomSecondaryColor, 
                ThemeService.Instance.CurrentTextColor);
            
            // Mettre à jour l'aperçu
            OnPropertyChanged(nameof(PreviewPrimaryBrush));
            OnPropertyChanged(nameof(PreviewSecondaryBrush));
            
            NotificationService.Instance.Show(
                DictText.ContainsKey("ThemeAppliedMessage") ? DictText["ThemeAppliedMessage"] : "Thème appliqué",
                ToastType.Success);
        }
        catch (Exception ex)
        {
            NotificationService.Instance.Show(
                DictText.ContainsKey("ErrorThemeInvalid") ? DictText["ErrorThemeInvalid"] : $"Couleur invalide : {ex.Message}",
                ToastType.Error);
        }
    }
    
    private async void OpenFilePickerFunction()
    {
        var topLevel = TopLevel.GetTopLevel(App.MainWindow);

        if (topLevel == null) return;

        // Ouvre le sélecteur de dossiers
        var folders = await topLevel.StorageProvider.OpenFolderPickerAsync(new FolderPickerOpenOptions
        {
            Title = "Sélectionner le dossier de logs",
            AllowMultiple = false
        });

        if (folders.Count > 0)
            // On récupère le chemin local du dossier
            LogPath = folders[0].Path.LocalPath;
    }

    private void UpdateLogFormat(int index)
    {
        ActualLogType = index == 1 ? LogType.XML : LogType.JSON;
    }

    private void ApplySavesFunction()
    {
        _jobManager.LogType = ActualLogType;
        _jobManager.ListeProcess = ListeProcess;
        if (!string.IsNullOrWhiteSpace(ExtensionsPrio))
        {
            _jobManager.PrioFilesExtension = ExtensionsPrio
                .Split(',')
                .Select(ext => ext.Trim()) // Retire les espaces accidentels (ex: " txt " -> "txt")
                .Where(ext => !string.IsNullOrEmpty(ext)) // Ignore les éléments vides (ex: "txt,")
                .Select(ext => ext.StartsWith(".") ? ext : "." + ext) // Ajoute le point s'il manque
                .ToList();
        }
        else
        {
            _jobManager.PrioFilesExtension = new List<string>();
        }

        
        // Validate encryption key is provided when encryption is enabled
        if (EnableEncryption && string.IsNullOrWhiteSpace(EncryptionKey))
        {
            NotificationService.Instance.Show(
                DictText.ContainsKey("ErrorEncryptionKeyRequired") ? DictText["ErrorEncryptionKeyRequired"] : "Le mot de passe de chiffrement est obligatoire",
                ToastType.Error);
            return;
        }
        
        if (!Directory.Exists(LogPath) && !string.IsNullOrEmpty(LogPath))
        {
            NotificationService.Instance.Show(
                DictText.ContainsKey("ErrorLogPathNotFound") ? DictText["ErrorLogPathNotFound"] : "Le chemin de logs spécifié n'existe pas",
                ToastType.Error);
            return;
        }
        
        try
        {
            // Sauvegarder les paramètres avec les couleurs du thème
            ConfigManager.ConfigWritter(
                LogPath, 
                FileSizeMo,
                ThemeService.Instance.CurrentPrimaryColor,
                ThemeService.Instance.CurrentHoverColor,
                ThemeService.Instance.CurrentSecondaryColor,
                ThemeService.Instance.CurrentTextColor);
            LogService.Observer.StartWatcher();
            
            NotificationService.Instance.Show(
                DictText.ContainsKey("SettingsAppliedMessage") ? DictText["SettingsAppliedMessage"] : "Paramètres appliqués",
                ToastType.Success);
        }
        catch (Exception ex)
        {
            NotificationService.Instance.Show(
                DictText.ContainsKey("ErrorSettingsSave") ? DictText["ErrorSettingsSave"] : $"Erreur lors de la sauvegarde : {ex.Message}",
                ToastType.Error);
        }
    }
}