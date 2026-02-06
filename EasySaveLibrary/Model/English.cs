using System.Text.Json;
using EasySaveLibrary.Interfaces;

namespace EasySaveLibrary.Model;

public class English : ILanguage
{
    /// <summary>
    /// Return the Dictionnary of all text in the application.
    /// </summary>
    /// <returns></returns>
    /// <exception cref="FileNotFoundException"></exception>
    public Dictionary<string, string> GetTranslations()
    {
        string filePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Translate", "Messages.json");
        if (!File.Exists(filePath))
        {
            throw new FileNotFoundException($"Fichier introuvable à l'adresse : {filePath}");
        }

        string jsonString = File.ReadAllText(filePath);
        return JsonSerializer.Deserialize<Dictionary<string, string>>(jsonString);
    }
}