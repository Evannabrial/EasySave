// See https://aka.ms/new-console-template for more information

using EasySaveLibrary;
using EasySaveLibrary.Model;

JobManager jm = new JobManager(new English());


Job j = new Job("Autre chose",
    "C:\\Users\\evann\\OneDrive\\Documents\\Evann\\Travail\\iut_depot\\Python\\NSI",
    "C:\\Users\\evann\\Documents\\SavesEasySave", 
    new Differential());

return j.Save.StartSave(j);

