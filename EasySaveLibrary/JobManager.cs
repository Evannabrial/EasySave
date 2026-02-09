using EasySaveLibrary.Interfaces;
using EasySaveLibrary.Model;
using System.Text.Json;
using System.Text.RegularExpressions;
using EasyLog;

namespace EasySaveLibrary;

public class JobManager
{
    private ILanguage _language;
    private List<Job> _lJobs;
    private LogType _logType;

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

    public LogType LogType
    {
        get => _logType;
        set => _logType = value;
    }

    public JobManager(ILanguage language, LogType logType)
    {
        Language = language;
        LogType = logType;
        LJobs = this.LoadJobsFromJson();
    }

    public Job AddJob(string name, string source, string target, ITypeSave typeSave)
    {
        Job newJob = new Job(name, source, target, typeSave);
        _lJobs.Add(newJob);
        return newJob;
    }
    
    /// <summary>
    /// Update a Job
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
        if (jobToUpdate == null || !_lJobs.Contains(jobToUpdate))
        {
            return null;
        }

        // Update the job
        jobToUpdate.Name = name;
        jobToUpdate.Source = source;
        jobToUpdate.Target = target;
        jobToUpdate.Save = typeSave;

        return jobToUpdate;
    }
    
    /// <summary>
    /// Delete a job 
    /// </summary>
    /// <param name="jobToDelete"></param>
    /// <returns></returns>
    public int DeleteJob(Job jobToDelete)
    {
        // Check if the Job to delete exist
        if (jobToDelete == null)
        {
            return 1;
        }

        return _lJobs.Remove(jobToDelete) ? 0 : 1;
    }

    /// <summary>
    /// Start multiple job save
    /// </summary>
    /// <param name="userChoice"></param>
    /// <returns>
    /// 0 => OK
    /// 1 => KO
    /// </returns>
    public int StartMultipleSave(string userChoice)
    {
        // If the userChoice is null
        if (userChoice == null)
        {
            return 1;
        }

        List<int> lIndexJob = new List<int>();

        // If the userChoice is like 1-3 we need to start the save of 1-2-3
        if (userChoice.Contains('-'))
        {
            // Match the first number before the '-' et the first number after '-'
            Match regexMatches = Regex.Match(userChoice, @"(\d+)\s*[-]\s*(\d+)");

            if (!regexMatches.Success)
            {
                return 1;
            }
            
            int firstNumber = int.Parse(regexMatches.Groups[1].Value);
            int secondNumber = int.Parse(regexMatches.Groups[2].Value);

            // Check if the second number is bigger than the first or
            // if the second number is bigger than the number of jobs in the list 
            if (secondNumber < firstNumber || secondNumber > LJobs.Count - 1)
            {
                return 1;
            }
            // Create a list of int of all number between the first element and the last
            // Exemple: '2-6' ==> [2,3,4,5,6]
            lIndexJob = Enumerable.Range(firstNumber, secondNumber - firstNumber + 1).ToList();
        }
        // If the userChoice is like 1;3 we need to start the save of 1-3
        else if (userChoice.Contains(';'))
        {
            // Match the first number before the ';' et the first number after '-'
            Match regexMatches = Regex.Match(userChoice, @"(\d+)\s*[;]\s*(\d+)");
            
            if (!regexMatches.Success)
            {
                return 1;
            }
            
            int firstNumber = int.Parse(regexMatches.Groups[1].Value);
            int secondNumber = int.Parse(regexMatches.Groups[2].Value);
            
            // Check if the second number is bigger than the first or
            // if the second number is bigger than the number of jobs in the list 
            if (secondNumber < firstNumber || secondNumber > LJobs.Count - 1)
            {
                return 1;
            }
            
            lIndexJob.Add(firstNumber);
            lIndexJob.Add(secondNumber);
        }
        else
        {
            // Match the first number of the string
            Match match = Regex.Match(userChoice, @"(\d+)");
            if (!match.Success)
            {
                return 1;
            }
            int job = int.Parse(match.Value);

            if (job > LJobs.Count - 1)
            {
                return 1;
            }
            
            lIndexJob.Add(job);
        }

        foreach (int index in lIndexJob)
        {
            // Check if the index is valid
            if (index >= 0 && index < _lJobs.Count)
            {
                Job job = _lJobs[index];
                
                // Start the save
                int result = job.Save.StartSave(job);
                
                // If the save succeeds
                if (result != 0)
                {
                    return 1;
                }
            }
        }

        return 0;
    }
    
    /// <summary>
    /// Change the language of the JobManager
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
        string filePath = "../Jobs/jobs.json";

        // Check if file exists
        if (!File.Exists(filePath))
        {
            return new List<Job>();
        }

        // Read file
        string jsonString = File.ReadAllText(filePath);

        // If file is empty, return empty list
        if (string.IsNullOrWhiteSpace(jsonString))
        {
            return new List<Job>();
        }

        // Deserialize manually because ITypeSave is an interface
        List<Job> jobs = new List<Job>();
        using JsonDocument doc = JsonDocument.Parse(jsonString);
        
        foreach (JsonElement element in doc.RootElement.EnumerateArray())
        {
            string name = element.GetProperty("Name").GetString() ?? "";
            string source = element.GetProperty("Source").GetString() ?? "";
            string target = element.GetProperty("Target").GetString() ?? "";
            string saveType = element.GetProperty("SaveType").GetString() ?? "Full";
            
            // Create the correct save type
            ITypeSave typeSave = saveType switch
            {
                "Differential" => new Differential(LogType),
                _ => new Full(LogType)
            };
            
            Job job = new Job(name, source, target, typeSave);
            
            // Get ID if present
            if (element.TryGetProperty("Id", out JsonElement idElement))
            {
                job.Id = idElement.GetGuid();
            }
            
            // Get LastTimeRun if present
            if (element.TryGetProperty("LastTimeRun", out JsonElement lastTimeElement) && lastTimeElement.ValueKind != JsonValueKind.Null)
            {
                job.LastTimeRun = lastTimeElement.GetDateTime();
            }
            
            jobs.Add(job);
        }

        return jobs;
    }

    // Save the jobs in lJobs in json file
    public void SaveJobs()
    {
        string filePath = "../Jobs/jobs.json";

        // Create directory if it doesn't exist
        string? directory = Path.GetDirectoryName(filePath);
        if (directory != null && !Directory.Exists(directory))
        {
            Directory.CreateDirectory(directory);
        }

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
        string jsonString = JsonSerializer.Serialize(jobsToSave, new JsonSerializerOptions
        {
            WriteIndented = true
        });

        // Write to file
        File.WriteAllText(filePath, jsonString);
    }
}
