using System.Text.Json;
using EasySaveLibrary.Interfaces;

namespace EasySaveLibrary.Model;

public class French : ILanguage
{
    public Dictionary<string, string> GetTranslations()
    {
        string filePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Translate", "Messages.fr.json");
        Console.WriteLine($"DEBUG: Je cherche ici -> {filePath}");
        if (!File.Exists(filePath))
        {
            throw new FileNotFoundException($"Fichier introuvable à l'adresse : {filePath}");
        }

        string jsonString = File.ReadAllText(filePath);
        return JsonSerializer.Deserialize<Dictionary<string, string>>(jsonString);
    }
}