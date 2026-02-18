using System.Diagnostics;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Xml.Serialization;
using EasyLog;
using EasySaveLibrary.Interfaces;
using EasySaveLibrary.Model;

namespace EasySaveLibrary;

public class JobManager
{
    private string _encryptionExtensions = "";
    private ILanguage _language;
    private string _listeProcess = "";
    private List<Job> _lJobs;
    
    private Dictionary<Guid, ManualResetEvent> _jobPauseEvents = new Dictionary<Guid, ManualResetEvent>();
    private Dictionary<Guid, CancellationTokenSource> _jobCancellationTokens = new Dictionary<Guid, CancellationTokenSource>();
    private readonly object _lockEvents = new object();

    public JobManager(ILanguage language, LogType logType)
    {
        Language = language;
        LogType = logType;
        LJobs = LoadJobsFromJson();
    }

    public List<Job> LJobs
    {
        get => _lJobs;
        set => _lJobs = value ?? new List<Job>();
    }

    public ILanguage Language
    {
        get => _language;
        set => _language = value ?? throw new ArgumentNullException(nameof(value));
    }

    public LogType LogType { get; set; }

    public bool EnableEncryption { get; set; }

    public string EncryptionExtensions
    {
        get => _encryptionExtensions;
        set => _encryptionExtensions = value ?? "";
    }

    public string EncryptionKey { get; set; } = "";

    public string ListeProcess
    {
        get => _listeProcess;
        set => _listeProcess = value ?? "";
    }

    public Job AddJob(string name, string source, string target, ITypeSave typeSave)
    {
        var newJob = new Job(name, source, target, typeSave);
        _lJobs.Add(newJob);
        return newJob;
    }

    /// <summary>
    ///     Update a Job
    /// </summary>
    /// <param name="jobToUpdate"></param>
    /// <param name="name"></param>
    /// <param name="source"></param>
    /// <param name="target"></param>
    /// <param name="typeSave"></param>
    /// <returns></returns>
    public Job UpdateJob(Job jobToUpdate, string name, string source, string target, ITypeSave typeSave)
    {
        // Check if the job to update exist
        if (jobToUpdate == null || !_lJobs.Contains(jobToUpdate)) return null;

        // Update the job
        jobToUpdate.Name = name;
        jobToUpdate.Source = source;
        jobToUpdate.Target = target;
        jobToUpdate.Save = typeSave;

        return jobToUpdate;
    }

    /// <summary>
    ///     Delete a job
    /// </summary>
    /// <param name="jobToDelete"></param>
    /// <returns></returns>
    public int DeleteJob(Job jobToDelete)
    {
        // Check if the Job to delete exist
        if (jobToDelete == null) return 1;

        return _lJobs.Remove(jobToDelete) ? 0 : 1;
    }

    /// <summary>
    ///     Start multiple job save
    /// </summary>
    /// <param name="userChoice"></param>
    /// <returns>
    ///     0 => OK
    ///     1 => KO
    /// </returns>
    public int StartMultipleSave(string userChoice)
    {
        var lProcessRunning = Process.GetProcesses();
        var lProcessBlock = ListeProcess.Split(',');

        foreach (var processName in lProcessBlock)
            if (lProcessRunning.Any(p => p.ProcessName.Equals(processName, StringComparison.OrdinalIgnoreCase)))
                return 0;

        // If the userChoice is null
        if (userChoice == null) return 1;

        var lIndexJob = new List<int>();

        // If the userChoice is like 1-3 we need to start the save of 1-2-3
        if (userChoice.Contains('-'))
        {
            // Match the first number before the '-' et the first number after '-'
            var regexMatches = Regex.Match(userChoice, @"(\d+)\s*[-]\s*(\d+)");

            if (!regexMatches.Success) return 1;

            var firstNumber = int.Parse(regexMatches.Groups[1].Value);
            var secondNumber = int.Parse(regexMatches.Groups[2].Value);

            // Check if the second number is bigger than the first or
            // if the second number is bigger than the number of jobs in the list 
            if (secondNumber < firstNumber || secondNumber > LJobs.Count - 1) return 1;
            // Create a list of int of all number between the first element and the last
            // Exemple: '2-6' ==> [2,3,4,5,6]
            lIndexJob = Enumerable.Range(firstNumber, secondNumber - firstNumber + 1).ToList();
        }
        // If the userChoice is like 1;3 we need to start the save of 1-3
        else if (userChoice.Contains(';'))
        {
            // Match the first number before the ';' et the first number after '-'
            var matches = Regex.Matches(userChoice, @"\d+");


            foreach (Match match in matches)
                // match.Value contient "1", puis "3", puis "4"
                if (int.TryParse(match.Value, out var index))
                    lIndexJob.Add(index);

            // Check if the second number is bigger than the first or
            // if the second number is bigger than the number of jobs in the list 
            if (lIndexJob.Max() > LJobs.Count - 1) return 1;
        }
        else
        {
            // Match the first number of the string
            var match = Regex.Match(userChoice, @"(\d+)");
            if (!match.Success) return 1;
            var job = int.Parse(match.Value);

            if (job > LJobs.Count - 1) return 1;

            lIndexJob.Add(job);
        }

        foreach (var index in lIndexJob)
            // Check if the index is valid
            if (index >= 0 && index < _lJobs.Count)
            {
                var job = _lJobs[index];
                var result = 0;
                
                ManualResetEvent pauseEvent = new ManualResetEvent(true);
                CancellationTokenSource cts = new CancellationTokenSource();
                
                lock(_lockEvents) 
                {
                    if (_jobPauseEvents.ContainsKey(job.Id)) 
                        _jobPauseEvents[job.Id] = pauseEvent;
                    else 
                        _jobPauseEvents.Add(job.Id, pauseEvent);
                    
                    if (_jobCancellationTokens.ContainsKey(job.Id)) 
                        _jobCancellationTokens[job.Id] = cts;
                    else 
                        _jobCancellationTokens.Add(job.Id, cts);
                }

                new Thread(() =>
                {
                    try 
                    {
                        job.Save.StartSave(job, LogType, pauseEvent, cts.Token, lProcessBlock, EnableEncryption, 
                            EncryptionExtensions, EncryptionKey);
                    }
                    finally
                    {
                        lock(_lockEvents) 
                        { 
                            _jobPauseEvents.Remove(job.Id);
                            _jobCancellationTokens.Remove(job.Id);
                        }
                        pauseEvent.Dispose();
                        cts.Dispose();
                    }
                }).Start();

                // If the save succeeds
                if (result != 0) return 1;
            }

        return 0;
    }

    /// <summary>
    ///     Change the language of the JobManager
    /// </summary>
    /// <param name="language"></param>
    /// <exception cref="NotImplementedException"></exception>
    public void SwitchLanguage(ILanguage language)
    {
        throw new NotImplementedException();
    }

    // Return all the jobs from a json file
    private List<Job> LoadJobsFromJson()
    {
        var filePath = "../Jobs/jobs.json";

        // Check if file exists
        if (!File.Exists(filePath)) return new List<Job>();

        // Read file
        var jsonString = File.ReadAllText(filePath);

        // If file is empty, return empty list
        if (string.IsNullOrWhiteSpace(jsonString)) return new List<Job>();

        // Deserialize manually because ITypeSave is an interface
        var jobs = new List<Job>();
        using var doc = JsonDocument.Parse(jsonString);

        foreach (var element in doc.RootElement.EnumerateArray())
        {
            var name = element.GetProperty("Name").GetString() ?? "";
            var source = element.GetProperty("Source").GetString() ?? "";
            var target = element.GetProperty("Target").GetString() ?? "";
            var saveType = element.GetProperty("SaveType").GetString() ?? "Full";

            // Create the correct save type
            ITypeSave typeSave = saveType switch
            {
                "Differential" => new Differential(),
                _ => new Full()
            };

            var job = new Job(name, source, target, typeSave);

            // Get ID if present
            if (element.TryGetProperty("Id", out var idElement)) job.Id = idElement.GetGuid();

            // Get LastTimeRun if present
            if (element.TryGetProperty("LastTimeRun", out var lastTimeElement) &&
                lastTimeElement.ValueKind != JsonValueKind.Null) job.LastTimeRun = lastTimeElement.GetDateTime();

            jobs.Add(job);
        }

        return jobs;
    }

    // Save the jobs in lJobs in json file
    public void SaveJobs()
    {
        var filePath = "../Jobs/jobs.json";

        // Create directory if it doesn't exist
        var directory = Path.GetDirectoryName(filePath);
        if (directory != null && !Directory.Exists(directory)) Directory.CreateDirectory(directory);

        // Create a list of anonymous objects for serialization
        var jobsToSave = _lJobs.Select(job => new
        {
            job.Id,
            job.Name,
            job.Source,
            job.Target,
            job.LastTimeRun,
            SaveType = job.Save.GetType().Name // Store the type name
        }).ToList();

        // Serialize to JSON
        var jsonString = JsonSerializer.Serialize(jobsToSave, new JsonSerializerOptions
        {
            WriteIndented = true
        });

        // Write to file
        File.WriteAllText(filePath, jsonString);
    }

    public JobStatus GetStatusOfJob(Guid idJob)
    {
        var job = LJobs.FirstOrDefault(j => j.Id == idJob);

        var pathFromConfig = ConfigManager.LogPath;
        // LOG DE DEBUG

        if (job == null) return null;

        double progress = 0;
        var status = "Prêt";

        switch (LogType)
        {
            case LogType.JSON:
                var filePath = Path.Combine(ConfigManager.Root["PathLog"], "livestate.json");

                // Si le fichier n'existe pas encore, le job n'a pas commencé
                if (!File.Exists(filePath)) return new JobStatus(idJob, status, progress);

                // On ajoute une petite sécurité de lecture (retry) car le fichier peut être locké par l'écriture
                var jsonString = "";
                try
                {
                    // On utilise FileShare.ReadWrite pour lire même si un écrit est en cours (dans la mesure du possible)
                    using (var fs = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
                    using (var sr = new StreamReader(fs))
                    {
                        jsonString = sr.ReadToEnd();
                    }
                }
                catch (IOException)
                {
                    return null;
                } // Fichier inaccessible pour l'instant

                if (string.IsNullOrWhiteSpace(jsonString)) return null;

                try
                {
                    // On désérialise LA LISTE
                    var globalState = JsonSerializer.Deserialize<List<LiveLog>>(jsonString);

                    // On cherche NOTRE job dedans
                    var myJobState = globalState?.FirstOrDefault(l => l.Name == job.Name);

                    if (myJobState != null)
                    {
                        status = myJobState.State switch 
                        {
                            "ON" => "En cours",
                            "OFF" => "Terminé",
                            "BLOCKED" => "Bloqué",
                            "Cancelled" => "Cancelled",
                            _ => "Prêt"
                        };
                        progress = myJobState.Progress;
                        return new JobStatus(idJob, status, progress);
                    }
                }
                catch
                {
                    // Erreur de parsing (fichier en cours d'écriture incomplète), on ignore
                    return null;
                }

                return null;

            case LogType.XML:
                var filePathXml = Path.Combine(ConfigManager.Root["PathLog"], "livestate.xml");
            
                // Si le fichier n'existe pas, on retourne l'état par défaut
                if (!File.Exists(filePathXml)) return new JobStatus(idJob, status, progress);

                try
                {
                    // 1. IMPORTANT : On indique au sérialiseur qu'on attend une LISTE de LiveLog
                    var serializer = new XmlSerializer(typeof(List<LiveLog>));

                    // Lecture partagée pour éviter les conflits d'accès
                    using (var fs = new FileStream(filePathXml, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
                    {
                        // 2. On désérialise en List<LiveLog>
                        var globalStateXml = (List<LiveLog>)serializer.Deserialize(fs);

                        // 3. On cherche le log correspondant à NOTRE job via son nom
                        var myJobState = globalStateXml?.FirstOrDefault(l => l.Name == job.Name);

                        if (myJobState != null)
                        {
                            status = myJobState.State switch 
                            {
                                "ON" => "En cours",
                                "OFF" => "Terminé",
                                "BLOCKED" => "Bloqué",
                                "CANCELLED" => "Cancelled",
                                _ => "Prêt"
                            };
                            progress = myJobState.Progress;
                            return new JobStatus(idJob, status, progress);
                        }
                    }
                }
                catch
                {
                    // Si le fichier est vide ou corrompu pendant l'écriture, on ignore
                    return null;
                }

                break;
        }

        return new JobStatus(idJob, status, progress);
    }
    
    // Méthode pour PAUSE
    public void PauseJob(Guid jobId)
    {
        lock(_lockEvents)
        {
            if (_jobPauseEvents.ContainsKey(jobId))
            {
                _jobPauseEvents[jobId].Reset(); // Feu Rouge
            }
        }
    }

    // Méthode pour REPRENDRE (Resume)
    public void ResumeJob(Guid jobId)
    {
        lock(_lockEvents)
        {
            if (_jobPauseEvents.ContainsKey(jobId))
            {
                _jobPauseEvents[jobId].Set(); // Feu Vert
            }
        }
    }
    
    public void StopJob(Guid jobId)
    {
        lock(_lockEvents)
        {
            if (_jobCancellationTokens.ContainsKey(jobId))
            {
                // 1. On demande l'annulation
                _jobCancellationTokens[jobId].Cancel();
                
                // 2. CRUCIAL : Si le job était en pause, il est bloqué sur WaitOne().
                // Il faut le débloquer pour qu'il puisse avancer et voir que le Token est annulé.
                if (_jobPauseEvents.ContainsKey(jobId))
                {
                    _jobPauseEvents[jobId].Set(); // On force le feu vert
                }
            }
        }
    }
    
    public bool IsJobPaused(Guid jobId)
    {
        lock (_lockEvents)
        {
            if (_jobPauseEvents.ContainsKey(jobId))
            {
                // WaitOne(0) permet de tester l'état sans bloquer le thread.
                // Si ça retourne true : Le feu est VERT (Set) -> Donc pas en pause.
                // Si ça retourne false : Le feu est ROUGE (Reset) -> Donc EN PAUSE.
                return !_jobPauseEvents[jobId].WaitOne(0);
            }
            return false;
        }
    }
}