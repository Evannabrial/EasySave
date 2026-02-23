using Microsoft.Extensions.Configuration;
using System.IO;
using System.Text.Json;
using System;
using EasySaveLibrary.Model;

namespace EasySaveLibrary;

public class ConfigManager
{
    public static IConfiguration Root { get; private set; }
    private static readonly string FilePath = Path.Combine(AppContext.BaseDirectory, "appsettings.json");

    static ConfigManager()
    {
        BuildRoot();
    }
    
    public static string LogPath 
    {
        get 
        {
            // On recharge la config pour être sûr à 100% d'avoir la dernière valeur disque
            // C'est un peu lourd mais ça garantit que ça marche
            BuildRoot(); 
            return Root["PathLog"];
        }
    }
    
    public static double FileSizeKo
    {
        get
        {
            BuildRoot();
            var value = Root["FileSizeKo"];
            return double.TryParse(value, out var result) ? result : 0;
        }
    }
    
    // Propriétés des couleurs du thème
    public static string? PrimaryColor
    {
        get
        {
            BuildRoot();
            return Root["PrimaryColor"];
        }
    }
    
    public static string? HoverColor
    {
        get
        {
            BuildRoot();
            return Root["HoverColor"];
        }
    }
    
    public static string? SecondaryColor
    {
        get
        {
            BuildRoot();
            return Root["SecondaryColor"];
        }
    }
    
    public static string? TextColor
    {
        get
        {
            BuildRoot();
            return Root["TextColor"];
        }
    }

    private static void BuildRoot()
    {
        Root = new ConfigurationBuilder()
            .SetBasePath(AppContext.BaseDirectory)
            .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
            .Build();
    }

    /// <summary>
    /// Met à jour le chemin des logs dans le fichier appsettings.json
    /// </summary>
    public static void ConfigWritter(string newLogPath, double fileSizeKo = 0, 
        string? primaryColor = null, string? hoverColor = null, 
        string? secondaryColor = null, string? textColor = null)
    {
        // Couleurs par défaut (thème Classique beige/jaune)
        const string defaultPrimary = "#F5B800";
        const string defaultHover = "#D4A000";
        const string defaultSecondary = "#F5F5DC";
        const string defaultText = "#000000";
        
        try
        {
            // 1. Créer l'objet avec la nouvelle valeur
            var settings = new AppSettingData 
            { 
                PathLog = newLogPath,
                FileSizeKo = fileSizeKo,
                PrimaryColor = primaryColor ?? PrimaryColor ?? defaultPrimary,
                HoverColor = hoverColor ?? HoverColor ?? defaultHover,
                SecondaryColor = secondaryColor ?? SecondaryColor ?? defaultSecondary,
                TextColor = textColor ?? TextColor ?? defaultText
            };

            // 2. Sérialiser en JSON (avec indentation pour la lisibilité)
            var options = new JsonSerializerOptions { WriteIndented = true };
            string jsonString = JsonSerializer.Serialize(settings, options);

            string logDir = newLogPath;
            if (!Directory.Exists(logDir)) Directory.CreateDirectory(logDir);

            // 3. Écrire physiquement sur le disque
            File.WriteAllText(FilePath, jsonString);

            // 4. Forcer la reconstruction de Root pour que les changements 
            // soient immédiats dans le reste de l'application
            BuildRoot();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Erreur lors de l'écriture de la config : {ex.Message}");
        }
    }
}