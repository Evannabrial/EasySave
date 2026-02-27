using System.Text.Json;
using System.Xml.Serialization;

namespace EasyLog;

public class LiveLog : Log
{
    private static readonly object _fileLock = new();
    private string _source;
    private string _state;
    private string _target;
    private long _nbFile;
    private double _progress;
    private long _nbFileLeft;
    private long _sizeFileLeft;

    public LiveLog()
    {
    }

    public LiveLog(string name, string action, string state, long nbFile, double progress, long nbFileLeft,
        long sizeFileLeft, string source, string target, LogType logType)
    {
        DateTime = DateTime.Now;
        Name = name;
        Action = action;
        State = state;
        NbFile = nbFile;
        Progress = progress;
        NbFileLeft = nbFileLeft;
        SizeFileLeft = sizeFileLeft;
        Source = source;
        Target = target;
        Type = logType;
    }

    public string State
    {
        get => _state;
        set => _state = value ?? throw new ArgumentNullException(nameof(value));
    }

    public string Source
    {
        get => _source;
        set => _source = value ?? throw new ArgumentNullException(nameof(value));
    }

    public string Target
    {
        get => _target;
        set => _target = value ?? throw new ArgumentNullException(nameof(value));
    }

    public long NbFile
    {
        get => _nbFile;
        set => _nbFile = value;
    }

    public double Progress
    {
        get => _progress;
        set => _progress = value;
    }

    public long NbFileLeft
    {
        get => _nbFileLeft;
        set => _nbFileLeft = value;
    }

    public long SizeFileLeft
    {
        get => _sizeFileLeft;
        set => _sizeFileLeft = value;
    }

    /// <summary>
    ///     Write the object inside a file in json.
    /// </summary>
    /// <param name="path"></param>
    /// <returns>
    ///     0 => OK
    ///     1 => KO
    /// </returns>
    public override int WriteLog(string path)
    {
        lock (_fileLock)
        {
            try
            {
                var jsonPath = Path.Combine(path, "livestate.json");
                var currentStates = new List<LiveLog>();

                switch (Type)
                {
                    // Dans LiveLog.cs -> WriteLog
                    case LogType.JSON:
                        // 1. Lire l'état actuel du fichier (s'il existe)
                        if (File.Exists(jsonPath))
                            try
                            {
                                var existingJson = File.ReadAllText(jsonPath);
                                if (!string.IsNullOrWhiteSpace(existingJson))
                                    // On essaie de récupérer la liste des jobs en cours
                                    currentStates = JsonSerializer.Deserialize<List<LiveLog>>(existingJson) ??
                                                    new List<LiveLog>();
                            }
                            catch
                            {
                                // Si le fichier est corrompu ou au mauvais format, on repart à neuf
                                currentStates = new List<LiveLog>();
                            }

                        // 2. Mettre à jour SEULEMENT mon job dans la liste
                        // On supprime l'ancienne entrée de ce job s'il y en avait une
                        currentStates.RemoveAll(x => x.Name == Name);

                        // On ajoute mon nouvel état
                        currentStates.Add(this);

                        // 3. Réécrire le fichier complet
                        var jsonOutput = JsonSerializer.Serialize(currentStates,
                            new JsonSerializerOptions { WriteIndented = true });
                        File.WriteAllText(jsonPath, jsonOutput);
                        break;

                    case LogType.XML:
                        var xmlPath = Path.Combine(path, "livestate.xml");
                        var serializer = new XmlSerializer(typeof(List<LiveLog>));

                        if (File.Exists(xmlPath))
                            try
                            {
                                using (var reader = new StreamReader(xmlPath))
                                {
                                    currentStates = (List<LiveLog>)serializer.Deserialize(reader);
                                }
                            }
                            catch
                            {
                                currentStates = new List<LiveLog>();
                            }

                        currentStates.RemoveAll(x => x.Name == Name);
                        currentStates.Add(this);

                        using (var writer = new StreamWriter(xmlPath))
                        {
                            serializer.Serialize(writer, currentStates);
                        }

                        break;
                }

                return 0;
            }
            catch (Exception e)
            {
                // Console.WriteLine(e);
                return 1;
            }
        }
    }
}