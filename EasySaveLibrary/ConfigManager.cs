using Microsoft.Extensions.Configuration;
using System.IO;
using System.Text.Json;
using System;
using EasyLog;
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
    
    public static LogDestination LogDestination 
    {
        get 
        {
            BuildRoot(); 
            var value = Root["LogDestination"];
            // Conversion du string (JSON) vers l'Enum
            if (Enum.TryParse<LogDestination>(value, out var result))
            {
                return result;
            }
            return LogDestination.Local; // Valeur par défaut
        }
    }
    
    public static string ServerIp
    {
        get
        {
            BuildRoot();
            return Root["ServerIp"] ?? "127.0.0.1"; // Valeur par défaut
        }
    }

    public static int ServerPort
    {
        get
        {
            BuildRoot();
            var value = Root["ServerPort"];
            return int.TryParse(value, out var result) ? result : 4242; // Valeur par défaut
        }
    }
    
    public static string LogPath 
    {
        get 
        {
            // On recharge la config pour être sûr à 100% d'avoir la dernière valeur disque
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
        string? secondaryColor = null, string? textColor = null,
        LogDestination? logDestination = null, string? serverIp = null, int? serverPort = null) // <--- AJOUTS ICI
    {
        const string defaultPrimary = "#F5B800";
        const string defaultHover = "#D4A000";
        const string defaultSecondary = "#F5F5DC";
        const string defaultText = "#000000";
        
        try
        {
            // 1. Créer l'objet avec la nouvelle valeur (ou garder l'ancienne si null)
            var settings = new AppSettingData 
            { 
                PathLog = newLogPath,
                FileSizeKo = fileSizeKo,
                PrimaryColor = primaryColor ?? PrimaryColor ?? defaultPrimary,
                HoverColor = hoverColor ?? HoverColor ?? defaultHover,
                SecondaryColor = secondaryColor ?? SecondaryColor ?? defaultSecondary,
                TextColor = textColor ?? TextColor ?? defaultText,
                
                LogDestination = (logDestination ?? LogDestination).ToString(), // On stocke en string
                ServerIp = serverIp ?? ServerIp,
                ServerPort = serverPort ?? ServerPort
            };

            var options = new JsonSerializerOptions { WriteIndented = true };
            string jsonString = JsonSerializer.Serialize(settings, options);

            // 3. Écrire physiquement sur le disque
            File.WriteAllText(FilePath, jsonString);

            // 4. Forcer la reconstruction
            BuildRoot();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Erreur lors de l'écriture de la config : {ex.Message}");
        }
    }
}