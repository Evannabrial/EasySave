using EasySaveLibrary.Interfaces;
using EasySaveLibrary.Model;

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

    public int StartMultipleSave(List<int> lIndexJob)
    {
        if (lIndexJob == null || lIndexJob.Count == 0)
        {
            return 0;
        }

        int successCount = 0;

        foreach (int index in lIndexJob)
        {
            // Check if the index is valid
            if (index >= 0 && index < _lJobs.Count)
            {
                Job job = _lJobs[index];
                
                // Start the save
                int result = job.Save.StartSave(job);
                
                // If the save succeeds (convention: returns 1 for success)
                if (result > 0)
                {
                    successCount++;
                }
            }
        }

        return successCount;
    }
    
    public void SwitchLanguage(ILanguage language)
    {
        throw new NotImplementedException();
    }

    // Return all the jobs from a json file
    private List<Job> LoadJobsFromJson()
    {
        return new List<Job>();
        //throw new NotImplementedException();
    }

    // Save the jobs in lJobs in json file
    public void SaveJobs()
    {
        //throw new NotImplementedException();
    }   
}
