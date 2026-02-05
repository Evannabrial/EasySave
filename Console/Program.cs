using System.Drawing;
using EasySaveLibrary;
using EasySaveLibrary.Interfaces;
using EasySaveLibrary.Model;


JobManager jm = new JobManager(new English());
Dictionary<string, string> dictText = jm.Language.GetTranslations();
Console.ForegroundColor = ConsoleColor.DarkBlue;

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

    var _continue = true;
    while (_continue)
    {
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
                    string name = Console.ReadLine();
                    Console.WriteLine(dictText.GetValueOrDefault("SourcePathMessage"));
                    string source = Console.ReadLine();
                    Console.WriteLine(dictText.GetValueOrDefault("DestinationPathMessage"));
                    string target = Console.ReadLine();
                    Console.WriteLine(dictText.GetValueOrDefault("BackupTypeMessage"));
                    string typeSave = Console.ReadLine();
                    ITypeSave typesave = new Full();
                    switch (typeSave)
                    {
                        case "1":
                        {
                            typesave = new Full();
                            break;
                        }
                        case "2":
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
                        Console.WriteLine(job + "\n");
                    }

                    isActionChoose = true;
                    break;
                case "3":
                    Console.Clear();
                    Console.WriteLine(dictText.GetValueOrDefault("ModifyJobMessage"));
                    foreach (var job in jm.LJobs.Select((value, index) => new { value, index }))
                    {
                        Console.WriteLine(job.index.ToString() + " - " + job.value + "\n");
                    }
                    string input2 = Console.ReadLine();
                    var selectedJob = new Job("", "", "", new Full());
                    if (int.TryParse(input2, out int selectedIndex) && selectedIndex >= 0 &&
                        selectedIndex < jm.LJobs.Count)
                    {
                        selectedJob = jm.LJobs[selectedIndex];
                    }
                    Console.WriteLine(dictText.GetValueOrDefault("JobNameMessage"));
                    string name2 = Console.ReadLine();
                    Console.WriteLine(dictText.GetValueOrDefault("SourcePathMessage"));
                    string source2 = Console.ReadLine();
                    Console.WriteLine(dictText.GetValueOrDefault("DestinationPathMessage"));
                    string target2 = Console.ReadLine();
                    Console.WriteLine(dictText.GetValueOrDefault("BackupTypeMessage"));
                    string typeSave2 = Console.ReadLine();
                    ITypeSave typesave2 = new Full();
                    switch (typeSave2)
                    {
                        case "1":
                        {
                            typesave2 = new Full();
                            break;
                        }
                        case "2":
                        {
                            typesave2 = new Differential();
                            break;
                        }
                        default:
                        {
                            typesave2 = new Full();
                            break;
                        }
                    }
                    jm.UpdateJob(selectedJob, name2, source2, target2, typesave2);
                    isActionChoose = true;
                    break;
                case "4":
                    Console.Clear();
                    Console.WriteLine(dictText.GetValueOrDefault("DeleteJobMessage"));
                    foreach (var job in jm.LJobs.Select((value, index) => new { value, index }))
                    {
                        Console.WriteLine(job.index.ToString() + " - " + job.value + "\n");
                    }
                    string input3 = Console.ReadLine();
                    var selectedJob2 = new Job("", "", "", new Full());
                    if (int.TryParse(input3, out int selectedIndex2) && selectedIndex2 >= 0 &&
                        selectedIndex2 < jm.LJobs.Count)
                    {
                        selectedJob2 = jm.LJobs[selectedIndex2];
                    }
                    jm.DeleteJob(selectedJob2);
                    isActionChoose = true;
                    break;
                case "5":
                    Console.Clear();
                    Console.WriteLine(dictText.GetValueOrDefault("StartSaveMessage"));
                    foreach (var job in jm.LJobs.Select((value, index) => new { value, index }))
                    {
                        Console.WriteLine(job.index.ToString() + " - " + job.value + "\n");
                    }
                    string input4 = Console.ReadLine();
                    jm.StartMultipleSave(input4);
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
}

