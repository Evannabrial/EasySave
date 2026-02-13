using System;
using System.IO;
using System.Threading.Tasks;
using EasySaveLibrary;

namespace EasySave.Services;

public class LogObserverService
{
    private FileSystemWatcher _watcher;
    public event Action OnLogChanged;

    public LogObserverService()
    {
        StartWatcher();
    }

    public void StartWatcher()
    {
        if (_watcher != null)
        {
            _watcher.EnableRaisingEvents = false;
            _watcher.Dispose();
        }

        string path = ConfigManager.LogPath;
        if (!Directory.Exists(path)) return;

        _watcher = new FileSystemWatcher(path, "livestate.*");
        _watcher.NotifyFilter = NotifyFilters.LastWrite | NotifyFilters.Size | NotifyFilters.Attributes;
        _watcher.Changed += (s, e) => OnLogChanged?.Invoke();
        _watcher.Created += (s, e) => OnLogChanged?.Invoke();
        _watcher.EnableRaisingEvents = true;
    }
}