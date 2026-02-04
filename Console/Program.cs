using System.Collections.Concurrent;
using EasySaveLibrary;
using EasySaveLibrary.Model;

JobManager jm = new JobManager(new English());


jm.AddJob("Autre chose",
    "C:\\Users\\evann\\OneDrive\\Documents\\Evann\\Travail\\iut_depot\\Python\\NSI",
    "C:\\Users\\evann\\Documents\\SavesEasySave",
    new Full());

jm.AddJob("Sol",
    "C:\\Users\\evann\\OneDrive\\Documents\\Evann\\Travail\\iut_depot\\Python\\NSI\\PROG.py",
    "C:\\Users\\evann\\Documents\\SavesEasySave",
    new Full());

return jm.StartMultipleSave([0]);
