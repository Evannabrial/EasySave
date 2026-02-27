using Avalonia;
using Avalonia.Media;
using System;
using System.Collections.Generic;

namespace EasySave.Services;

/// <summary>
/// Représente un preset de thème avec ses couleurs.
/// </summary>
public class ThemePreset
{
    public string Name { get; }
    public string PrimaryColor { get; }
    public string HoverColor { get; }
    public string SecondaryColor { get; }
    public string TextColor { get; }

    public ThemePreset(string name, string primaryColor, string hoverColor, string secondaryColor, string textColor)
    {
        Name = name;
        PrimaryColor = primaryColor;
        HoverColor = hoverColor;
        SecondaryColor = secondaryColor;
        TextColor = textColor;
    }

    public override string ToString() => Name;
}

/// <summary>
/// Service singleton pour gérer les thèmes de couleurs de l'application.
/// Permet de changer les couleurs primaires et secondaires dynamiquement.
/// </summary>
public class ThemeService
{
    private static ThemeService? _instance;
    public static ThemeService Instance => _instance ??= new ThemeService();

    // Couleurs par défaut (thème Classique beige/jaune)
    public const string DefaultPrimaryColor = "#F5B800";      // Jaune/Or
    public const string DefaultHoverColor = "#D4A000";        // Jaune foncé
    public const string DefaultSecondaryColor = "#F5F5DC";    // Beige/Crème
    public const string DefaultTextColor = "#000000";         // Noir

    /// <summary>
    /// Liste des thèmes prédéfinis disponibles.
    /// Chaque thème contient une couleur primaire et une couleur secondaire (fond).
    /// </summary>
    public List<ThemePreset> AvailableThemes { get; } = new List<ThemePreset>
    {
        new ThemePreset("Classique (Défaut)", DefaultPrimaryColor, DefaultHoverColor, DefaultSecondaryColor, DefaultTextColor),
        new ThemePreset("Vert & Noir", "#008000", "#2E8B57", "#000000", "#FFFFFF"),
        new ThemePreset("Bleu & Gris", "#1E90FF", "#4169E1", "#1E1E1E", "#FFFFFF"),
        new ThemePreset("Rouge & Noir", "#DC143C", "#B22222", "#121212", "#FFFFFF"),
        new ThemePreset("Violet & Sombre", "#8A2BE2", "#9400D3", "#1A1A2E", "#FFFFFF"),
        new ThemePreset("Orange & Noir", "#FF8C00", "#FF6600", "#1C1C1C", "#FFFFFF"),
        new ThemePreset("Cyan & Sombre", "#00CED1", "#008B8B", "#0D1B2A", "#FFFFFF"),
        new ThemePreset("Rose & Noir", "#FF69B4", "#FF1493", "#1A1A1A", "#FFFFFF"),
        new ThemePreset("Or & Sombre", "#FFD700", "#DAA520", "#1F1F1F", "#000000"),
        new ThemePreset("Bleu Clair", "#4FC3F7", "#0288D1", "#263238", "#FFFFFF"),
        new ThemePreset("Vert Menthe", "#00E676", "#00C853", "#212121", "#FFFFFF"),
    };

    private string _currentPrimaryColor = DefaultPrimaryColor;
    private string _currentHoverColor = DefaultHoverColor;
    private string _currentSecondaryColor = DefaultSecondaryColor;
    private string _currentTextColor = DefaultTextColor;

    public string CurrentPrimaryColor => _currentPrimaryColor;
    public string CurrentHoverColor => _currentHoverColor;
    public string CurrentSecondaryColor => _currentSecondaryColor;
    public string CurrentTextColor => _currentTextColor;

    /// <summary>
    /// Applique un thème prédéfini à l'application.
    /// </summary>
    public void ApplyTheme(ThemePreset theme)
    {
        ApplyColors(theme.PrimaryColor, theme.HoverColor, theme.SecondaryColor, theme.TextColor);
    }

    /// <summary>
    /// Applique des couleurs personnalisées à l'application.
    /// </summary>
    public void ApplyColors(string primaryHex, string hoverHex, string secondaryHex, string textHex)
    {
        _currentPrimaryColor = primaryHex;
        _currentHoverColor = hoverHex;
        _currentSecondaryColor = secondaryHex;
        _currentTextColor = textHex;

        var app = Application.Current;
        if (app == null) return;

        try
        {
            var primaryColor = Color.Parse(primaryHex);
            var hoverColor = Color.Parse(hoverHex);
            var secondaryColor = Color.Parse(secondaryHex);
            var textColor = Color.Parse(textHex);
            
            // Calculer la couleur du texte des boutons en fonction de la luminosité de la couleur primaire
            var buttonTextColor = GetContrastingTextColor(primaryColor);

            // Mettre à jour les ressources de couleur dans l'application
            app.Resources["PrimaryColor"] = primaryColor;
            app.Resources["SecondaryColor"] = secondaryColor;
            app.Resources["ButtonColor"] = primaryColor;
            app.Resources["HoverButtonColor"] = hoverColor;
            app.Resources["TextColor"] = textColor;
            app.Resources["ButtonTextColor"] = buttonTextColor;

            // Mettre à jour les brushes
            app.Resources["PrimaryBrush"] = new SolidColorBrush(primaryColor);
            app.Resources["HoverButtonBrush"] = new SolidColorBrush(hoverColor);
            app.Resources["ButtonTextBrush"] = new SolidColorBrush(buttonTextColor);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Erreur lors de l'application du thème : {ex.Message}");
        }
    }
    
    /// <summary>
    /// Calcule la luminosité relative d'une couleur et retourne noir ou blanc pour le contraste optimal.
    /// Utilise la formule de luminosité perçue (ITU-R BT.709).
    /// </summary>
    private Color GetContrastingTextColor(Color backgroundColor)
    {
        // Formule de luminosité perçue (pondérée pour la perception humaine)
        // L'œil humain est plus sensible au vert, puis au rouge, puis au bleu
        double luminance = (0.299 * backgroundColor.R + 0.587 * backgroundColor.G + 0.114 * backgroundColor.B) / 255;
        
        // Si la couleur est claire (luminance > 0.5), utiliser du texte noir, sinon blanc
        return luminance > 0.5 ? Colors.Black : Colors.White;
    }

    /// <summary>
    /// Applique une couleur primaire personnalisée avec génération automatique du hover.
    /// </summary>
    public void ApplyCustomPrimaryColor(string primaryHex)
    {
        try
        {
            var primaryColor = Color.Parse(primaryHex);
            // Générer une couleur hover plus foncée
            var hoverColor = DarkenColor(primaryColor, 0.2);
            ApplyColors(primaryHex, hoverColor.ToString(), _currentSecondaryColor, _currentTextColor);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Erreur couleur primaire : {ex.Message}");
        }
    }

    /// <summary>
    /// Applique une couleur secondaire (fond) personnalisée.
    /// </summary>
    public void ApplyCustomSecondaryColor(string secondaryHex)
    {
        ApplyColors(_currentPrimaryColor, _currentHoverColor, secondaryHex, _currentTextColor);
    }

    /// <summary>
    /// Assombrit une couleur d'un certain pourcentage.
    /// </summary>
    private Color DarkenColor(Color color, double factor)
    {
        return Color.FromRgb(
            (byte)(color.R * (1 - factor)),
            (byte)(color.G * (1 - factor)),
            (byte)(color.B * (1 - factor))
        );
    }

    /// <summary>
    /// Charge le thème depuis la configuration au démarrage.
    /// </summary>
    public void LoadThemeFromConfig(string? primaryColor, string? hoverColor, string? secondaryColor, string? textColor)
    {
        var primary = primaryColor ?? DefaultPrimaryColor;
        var hover = hoverColor ?? DefaultHoverColor;
        var secondary = secondaryColor ?? DefaultSecondaryColor;
        var text = textColor ?? DefaultTextColor;
        
        ApplyColors(primary, hover, secondary, text);
    }
}






