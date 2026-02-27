using System.Diagnostics;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Text.Json;

namespace EasyLog;

public class LogManager
{
    private readonly string _baseLogPath;
    private LogType _typeSave;
    private LogDestination _logDestination;
    private string _serverIp;
    private int _serverPort;

    public LogType TypeSave
    {
        get => _typeSave;
        set => _typeSave = value;
    }

    public LogDestination LogDestination
    {
        get => _logDestination;
        set => _logDestination = value;
    }

    public string ServerIp
    {
        get => _serverIp;
        set => _serverIp = value ?? "127.0.0.1";
    }

    public int ServerPort
    {
        get => _serverPort;
        set => _serverPort = value;
    }
    
    public LogManager(string baseLogPath, LogDestination logDestination, string serverIp, int serverPort)
    {
        _baseLogPath = baseLogPath;
        LogDestination = logDestination;
        ServerIp = serverIp;
        ServerPort = serverPort;
    }


    /// <summary>
    /// Write a DailyLog 
    /// </summary>
    /// <param name="name"></param>
    /// <param name="sourcePath"></param>
    /// <param name="targetPath"></param>
    /// <param name="action"></param>
    /// <param name="execTime"></param>
    /// <returns></returns>
    public int WriteNewLog(string name, string sourcePath, string targetPath, string action, double execTime)
    {
        long size = -1;
        if (File.Exists(sourcePath))
        {
            FileInfo fi = new FileInfo(sourcePath);
            size = fi.Length;
        }
        else if(Directory.Exists(sourcePath))
        {
            size = Directory.EnumerateFiles(sourcePath, "*", SearchOption.AllDirectories)
                .Sum(f => new FileInfo(f).Length);;
        }
        
        Log log = new DailyLog(name, action, sourcePath, targetPath, size, execTime, TypeSave);
        
        return ProcessLogDispatch(log);
    }
    
    
    /// <summary>
    /// Write a LiveLog
    /// </summary>
    /// <param name="name"></param>
    /// <param name="sourcePath"></param>
    /// <param name="targetPath"></param>
    /// <param name="action"></param>
    /// <param name="state"></param>
    /// <param name="progress"></param>
    /// <param name="nbFile"></param>
    /// <param name="nbFileLeft"></param>
    /// <param name="sizeFileLeft"></param>
    /// <returns></returns>
    public int WriteNewLog(string name, string sourcePath, string targetPath, string action, string state, double progress, 
        int nbFile, int nbFileLeft, long sizeFileLeft )
    {
        Log log = new LiveLog(name, action, state, nbFile, progress, nbFileLeft, sizeFileLeft, sourcePath, targetPath, TypeSave);
        var p = AppDomain.CurrentDomain;
        
        return log.WriteLog(_baseLogPath);
    }
    
    private int ProcessLogDispatch(Log log)
    {
        int result = 0;

        // 1. ÉCRITURE LOCALE
        if (LogDestination == LogDestination.Local || LogDestination == LogDestination.Both)
        {
            result = log.WriteLog(_baseLogPath);
        }

        // 2. ENVOI CENTRALISÉ (SOCKET TCP)
        if (LogDestination == LogDestination.Docker || LogDestination == LogDestination.Both)
        {
            SendLogViaSocket(log);
        }

        return result;
    }
    
    private void SendLogViaSocket(Log log)
    {
        try
        {
            // 1. Préparation des données
            string jsonString = JsonSerializer.Serialize(log, log.GetType(), new JsonSerializerOptions { WriteIndented = true });
            byte[] data = Encoding.UTF8.GetBytes(jsonString);
        
            // 2. Création du Socket client
            using Socket clientSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
        
            // Paramètre de sécurité : Timeout de 2 secondes pour ne pas bloquer la sauvegarde si le serveur est down
            clientSocket.SendTimeout = 2000;
            clientSocket.ReceiveTimeout = 2000;

            // 3. Connexion au serveur distant
            IPAddress ipAddress = IPAddress.Parse(ServerIp);
            IPEndPoint remoteEndPoint = new IPEndPoint(ipAddress, ServerPort);
        
            clientSocket.Connect(remoteEndPoint);
        
            // 4. Envoi des données
            clientSocket.Send(data);
        
            // 5. Indique proprement au serveur qu'on a fini d'envoyer des données
            clientSocket.Shutdown(SocketShutdown.Send);
        }
        catch (SocketException)
        {
            // Le serveur est éteint ou l'IP est fausse
            // On étouffe l'erreur réseau pour ne pas faire crasher la sauvegarde locale
            Console.WriteLine($"Avertissement : Impossible d'envoyer le log au serveur {ServerIp}:{ServerPort}.");
        }
    }
}
