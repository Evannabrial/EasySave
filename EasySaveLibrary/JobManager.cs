using EasySaveLibrary.Interfaces;
using EasySaveLibrary.Model;

namespace EasySaveLibrary;

public class JobManager
{
    private ILanguage _language;
    private List<Job> _lJobs;

    public JobManager(ILanguage language)
    {
        _language = language;
        _lJobs = new List<Job>();
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

    // TODO Implémenter l'ajout d'un Job dans lJobs
    public Job AddJob(string name, string source, string target, ITypeSave typeSave)
    {
        throw new NotImplementedException();
    }

    public Job UpdateJob(Job jobToUpdate, string name, string source, string target, ITypeSave typeSave)
    {
        throw new NotImplementedException();
    }

    public int DeleteJob(Job jobToDelete)
    {
        throw new NotImplementedException();
    }

    public int StartMultipleSave(List<int> lIndexJob)
    {
        throw new NotImplementedException();
    }

    public void SwitchLanguage(ILanguage language)
    {
        throw new NotImplementedException();
    }
}