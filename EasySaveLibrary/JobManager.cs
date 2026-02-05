using EasySaveLibrary.Interfaces;
using EasySaveLibrary.Model;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace EasySaveLibrary;

public class JobManager
{
    private ILanguage _language;
    private List<Job> _lJobs;

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

    public JobManager(ILanguage language)
    {
        _language = language;
        _lJobs = this.LoadJobsFromJson();
    }

    public Job AddJob(string name, string source, string target, ITypeSave typeSave)
    {
        Job newJob = new Job(name, source, target, typeSave);
        _lJobs.Add(newJob);
        return newJob;
    }
    
    public Job UpdateJob(Job jobToUpdate, string name, string source, string target, ITypeSave typeSave)
    {
        if (jobToUpdate == null || !_lJobs.Contains(jobToUpdate))
        {
            return null;
        }

        jobToUpdate.Name = name;
        jobToUpdate.Source = source;
        jobToUpdate.Target = target;
        jobToUpdate.Save = typeSave;

        return jobToUpdate;
    }
    
    public int DeleteJob(Job jobToDelete)
    {
        if (jobToDelete == null)
        {
            return 0;
        }

        return _lJobs.Remove(jobToDelete) ? 1 : 0;
    }

    public int StartMultipleSave(string userChoice)
    {
        if (userChoice == null && userChoice.Length >3)
        {
            return 1;
        }

        List<int> lIndexJob = new List<int>();

        if (userChoice.Contains('-'))
        {
            Match regexMatches = Regex.Match(userChoice, @"(\d+)\s*[;-]\s*(\d+)");

            if (!regexMatches.Success)
            {
                return 1;
            }
            
            int firstNumber = int.Parse(regexMatches.Groups[1].Value);
            int secondNumber = int.Parse(regexMatches.Groups[2].Value);

            if (secondNumber < firstNumber && secondNumber <= LJobs.Count)
            {
                return 1;
            }
            lIndexJob = Enumerable.Range(firstNumber, secondNumber - firstNumber + 1).ToList();
        }
        else if (userChoice.Contains(';'))
        {
            Match regexMatches = Regex.Match(userChoice, @"(\d+)\s*[;-]\s*(\d+)");
            
            if (!regexMatches.Success)
            {
                return 1;
            }
            
            int firstNumber = int.Parse(regexMatches.Groups[1].Value);
            int secondNumber = int.Parse(regexMatches.Groups[2].Value);
            
            if (secondNumber < firstNumber && secondNumber <= LJobs.Count)
            {
                return 1;
            }
            
            lIndexJob.Add(firstNumber);
            lIndexJob.Add(secondNumber);
        }
        else
        {
            Match match = Regex.Match(userChoice, @"(\d+)");
            if (!match.Success)
            {
                return 1;
            }
            int job = int.Parse(match.Value);
            
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
                
                // If the save succeeds (convention: returns 1 for success)
                if (result != 0)
                {
                    return 1;
                }
            }
        }

        return 0;
    }
    
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
                "Differential" => new Differential(),
                _ => new Full()
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
