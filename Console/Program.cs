using System.Globalization;
using EasySaveLibrary;
using EasySaveLibrary.Model;


JobManager jm = new JobManager(new French());
Dictionary<string, string> dictText = jm.Language.GetTranslations();


var @continue = true;
while (@continue)
{
    Console.WriteLine(dictText.GetValueOrDefault("LanguageMessage") + "\n");
    var input = Console.ReadLine();
    Console.WriteLine("------------------------------\n");
    Console.WriteLine(dictText.GetValueOrDefault("WelcomeMessage") + "\n");
    Console.WriteLine("------------------------------\n");
    Console.WriteLine(dictText.GetValueOrDefault("MenuMessage"));
    Console.WriteLine(dictText.GetValueOrDefault("ActionMessage"));
}