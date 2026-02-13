using EasySaveLibrary;
using EasyLog;
using EasySaveLibrary.Model;

namespace EasySave.Services;

/// <summary>
/// Service singleton pour gérer l'instance unique du JobManager
/// </summary>
public class JobManagerService
{
    private static JobManagerService _instance;
    private static readonly object _lock = new object();
    private JobManager _jobManager;

    public JobManager JobManager
    {
        get => _jobManager;
    }

    private JobManagerService()
    {
        // Initialisation par défaut avec l'anglais et JSON
        _jobManager = new JobManager(new English(), LogType.JSON);
    }

    /// <summary>
    /// Obtient l'instance unique du service
    /// </summary>
    public static JobManagerService Instance
    {
        get
        {
            if (_instance == null)
            {
                lock (_lock)
                {
                    if (_instance == null)
                    {
                        _instance = new JobManagerService();
                    }
                }
            }
            return _instance;
        }
    }
}
