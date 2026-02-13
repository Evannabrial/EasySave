using System;
using System.IO;
using EasySaveLibrary;

namespace EasySave.Services;

public class LogObserverService
{
    private FileSystemWatcher _watcher;
    public event Action OnLogChanged;

    public LogObserverService(string extension)
    {
        string path = ConfigReader.Root["PathLog"];
        if (!Directory.Exists(path) || !File.Exists(path + "\\livestate." + extension))
        {
            return;
        }

        _watcher = new FileSystemWatcher(path , "livestate." + extension);
        // Dans LogObserverService.cs
        _watcher.NotifyFilter = NotifyFilters.LastWrite 
                                | NotifyFilters.Size 
                                | NotifyFilters.Attributes; // Ajoute Attributes

        _watcher.Changed += (s, e) => OnLogChanged?.Invoke();
        _watcher.Created += (s, e) => OnLogChanged?.Invoke(); // Ajoute la création
        _watcher.EnableRaisingEvents = true;
    }
}