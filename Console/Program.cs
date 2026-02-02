using EasySaveLibrary;
using EasySaveLibrary.Model;


JobManager jm = new JobManager(new English());
Dictionary<string, string> dictText = jm.Language.GetTranslations();


var @continue = true;
while (@continue)
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
    Console.WriteLine("------------------------------\n");
    Console.WriteLine(dictText.GetValueOrDefault("WelcomeMessage") + "\n");
    Console.WriteLine("------------------------------\n");
    Console.WriteLine(dictText.GetValueOrDefault("MenuMessage"));
    Console.WriteLine(dictText.GetValueOrDefault("ActionMessage"));
    @continue = false;

}