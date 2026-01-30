// See https://aka.ms/new-console-template for more information

using EasySaveLibrary;
using EasySaveLibrary.Interfaces;
using EasySaveLibrary.Model;

JobManager jm = new JobManager(new English());

ITypeSave typeSave = new Differential();
Job job = jm.AddJob("test", "C:/source", "C:/target", typeSave);
Console.WriteLine($"Job créé: {job.Name} | {job.Source} -> {job.Target}");

Job updatedJob = jm.UpdateJob(job, "test modifié", "C:/new_source", "C:/new_target", new Sequential());
Console.WriteLine($"Job mis à jour: {updatedJob.Name} | {updatedJob.Source} -> {updatedJob.Target}");

int result = jm.DeleteJob(updatedJob);
Console.WriteLine($"Job supprimé: {(result == 1 ? "succès" : "échec")}");

