using System.Text.RegularExpressions;
using EasySaveLibrary.Interfaces;

namespace EasySaveLibrary.Model;

public class Full : ITypeSave
{
    
     /// <returns>
     /// 0 => OK
     /// 1 => Element source non trouvé
     /// 2 => Erreur copie du fichier
     /// 3 => Erreur création du dossier
     /// </returns>
    public int StartSave(Job job)
    {
        bool isFile = File.Exists(job.Source);
        bool isDirectory = Directory.Exists(job.Source);
        
        if (!isDirectory && !isFile)
        {
            return 1;
        }

        if (isFile)
        {
            try
            {
                string nameFile = Regex.Match(job.Source, @"[^\\]+$").Value;
                File.Copy(job.Source, job.Target + "\\"  + nameFile);
            }
            catch (Exception e)
            {
                // Console.WriteLine(e);
                return 2;
            }
            return 0;
        }
        
        // We use the Breadth-first (parcours en largeur) search algorithm to visit all the folders and files in the source
        // See algorithm here https://en.wikipedia.org/wiki/Breadth-first_search
        Queue<string> queue = new Queue<string>();
        List<string> marked = new List<string>();
        
        queue.Enqueue(job.Source);
        
        while (queue.Count > 0)
        {
            string actual = queue.Dequeue();
            
            if (Directory.Exists(actual))
            {
                foreach (string el in Directory.EnumerateFileSystemEntries(actual))
                {
                    string pathToCreate = el.Split(job.Source)[1];
                    if (Directory.Exists(el) &&  !marked.Contains(el))
                    {
                        queue.Enqueue(el);
                        marked.Add(el);

                        try
                        {
                            Directory.CreateDirectory(job.Target + pathToCreate);
                        }
                        catch (Exception e)
                        {
                            // Console.WriteLine(e);
                            return 3;
                        }
                    }
                    else
                    {
                        try
                        {
                            File.Copy(el, job.Target + pathToCreate);
                        }
                        catch (Exception e)
                        {
                            Console.WriteLine(e.Message);
                            return 2;
                        }
                    }
                }
            }
        }
        
        return 0;
    }
}