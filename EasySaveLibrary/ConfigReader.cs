using Microsoft.Extensions.Configuration;
using System.IO;

namespace EasySaveLibrary;

public class ConfigReader
{
    public static IConfiguration Root { get; }

    static ConfigReader()
    {
        // AppContext.BaseDirectory pointe TOUJOURS vers le dossier de l'EXE
        string path = AppContext.BaseDirectory;

        Root = new ConfigurationBuilder()
            .SetBasePath(path)
            .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
            .Build();
    }
}
