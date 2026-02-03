using System.Drawing;
using EasySaveLibrary;
using EasySaveLibrary.Interfaces;
using EasySaveLibrary.Model;


JobManager jm = new JobManager(new English());
Dictionary<string, string> dictText = jm.Language.GetTranslations();
Console.ForegroundColor = ConsoleColor.DarkBlue;

var _continue = true;
while (_continue)
{
    Console.WriteLine(dictText.GetValueOrDefault("LanguageMessage") + "\n");
    var input = Console.ReadLine();
    bool isLangChoose = false;
    while (!isLangChoose)
    {
        switch (input)
        {
            case "1":
                jm.Language = new French();
                dictText = jm.Language.GetTranslations();
                isLangChoose = true;
                Console.Clear();
                break;
            case "2":
                jm.Language = new English();
                dictText = jm.Language.GetTranslations();
                isLangChoose = true;
                Console.Clear();
                break;
            default:
                Console.Clear();
                Console.WriteLine(dictText.GetValueOrDefault("LanguageErrorMessage"));
                Console.WriteLine(dictText.GetValueOrDefault("LanguageMessage") + "\n");
                input = Console.ReadLine();
                break;
        } 
    }

    Console.ForegroundColor = ConsoleColor.Yellow;
    Console.WriteLine("------------------------------\n");
    Console.WriteLine(dictText.GetValueOrDefault("WelcomeMessage") + "\n");
    Console.WriteLine("------------------------------\n");
    Console.ForegroundColor = ConsoleColor.DarkBlue;
    Console.WriteLine(dictText.GetValueOrDefault("MenuMessage"));
    Console.WriteLine(dictText.GetValueOrDefault("ActionMessage"));
    input = Console.ReadLine();
    bool isActionChoose = false;
    while (!isActionChoose)
    {
        switch (input)
        {
            case "1":
                Console.Clear();
                Console.WriteLine(dictText.GetValueOrDefault("CreateJobMessage"));
                Console.WriteLine(dictText.GetValueOrDefault("JobNameMessage"));
                string name =  Console.ReadLine();
                Console.WriteLine(dictText.GetValueOrDefault("SourcePathMessage"));
                string source = Console.ReadLine();
                Console.WriteLine(dictText.GetValueOrDefault("TargetPathMessage"));
                string target = Console.ReadLine();
                Console.WriteLine(dictText.GetValueOrDefault("TypeSaveMessage"));
                string typeSave = Console.ReadLine();
                ITypeSave typesave = new Full();
                switch (typeSave)
                {
                    case "1" :
                    {
                        typesave = new Full();
                        break;
                    }
                    case "2" :
                    {
                        typesave = new Differential();
                        break;
                    }
                    default:
                    {
                        typesave = new Full();
                        break;
                    }
                }
                jm.AddJob(name, source, target, typesave);
                isActionChoose = true;
                break;
            case "2":
                Console.Clear();
                Console.WriteLine(dictText.GetValueOrDefault("DisplayJobMessage"));
                foreach (Job job in jm.LJobs)
                {
                    Console.WriteLine(job);
                }
                isActionChoose = true;
                break;
            case "3":
                Console.Clear();
                Console.WriteLine(dictText.GetValueOrDefault("ModifyJobsMessage"));
                foreach (var job in jm.LJobs.Select((value, index) => new { value, index }))
                {
                    Console.WriteLine(job.index.ToString() + job.value);
                }
                isActionChoose = true;
                break;
            case "4":
                Console.Clear();
                Console.WriteLine(dictText.GetValueOrDefault("DeleteJobsMessage"));
                // TODO Implémenter la suppression d'un Job
                isActionChoose = true;
                break;
            case "5":
                Console.Clear();
                Console.WriteLine(dictText.GetValueOrDefault("StartSaveMessage"));
                // TODO Implémenter le lancement d'une sauvegarde
                isActionChoose = true;
                break;
            case "6":
                Console.Clear();
                Console.WriteLine(dictText.GetValueOrDefault("ExitMessage"));
                _continue = false;
                isActionChoose = true;
                break;

            default:
                Console.Clear();
                Console.WriteLine(dictText.GetValueOrDefault("MenuErrorMessage"));
                input = Console.ReadLine();
                break;
        }
    }

}