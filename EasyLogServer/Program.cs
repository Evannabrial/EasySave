using System;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace EasySaveLogServer
{
    class Program
    {
        private static readonly object _fileLock = new object();
        private static readonly string _logDirectory = "/app/logs";

        static async Task Main(string[] args)
        {
            if (!Directory.Exists(_logDirectory))
            {
                Directory.CreateDirectory(_logDirectory);
            }

            int port = 4242;
            
            // 1. Création du Socket serveur TCP (IPv4, flux de données, protocole TCP)
            using Socket serverSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            
            // 2. Association du Socket à n'importe quelle adresse IP locale sur le port 4242
            serverSocket.Bind(new IPEndPoint(IPAddress.Any, port));
            
            // 3. Mise en écoute (Backlog de 100 connexions en attente maximum)
            serverSocket.Listen(100);
            
            Console.WriteLine($"Serveur de logs (Socket) en écoute sur le port {port}...");

            while (true)
            {
                // 4. Acceptation asynchrone des connexions entrantes
                Socket clientSocket = await serverSocket.AcceptAsync();
                
                // 5. Traitement du client dans une tâche séparée (non-bloquant)
                _ = HandleClientAsync(clientSocket);
            }
        }

        private static async Task HandleClientAsync(Socket clientSocket)
        {
            // On s'assure que le socket sera bien fermé à la fin du bloc using
            using (clientSocket)
            {
                try
                {
                    // Pour lire facilement tout le texte UTF-8 envoyé par le socket
                    using (NetworkStream stream = new NetworkStream(clientSocket))
                    using (StreamReader reader = new StreamReader(stream, Encoding.UTF8))
                    {
                        // Lit jusqu'à ce que le client ferme la connexion
                        string jsonLog = await reader.ReadToEndAsync();
                        
                        if (!string.IsNullOrWhiteSpace(jsonLog))
                        {
                            WriteLogToFile(jsonLog);
                        }
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Erreur de réception : {ex.Message}");
                }
            }
        }

        private static void WriteLogToFile(string logData)
        {
            // Format du fichier : logs_20260224.json
            string fileName = $"logs_{DateTime.Now:yyyyMMdd}.json";
            string filePath = Path.Combine(_logDirectory, fileName);

            // Verrou pour empêcher deux threads d'écrire en même temps dans le fichier
            lock (_fileLock)
            {
                bool isNewFile = !File.Exists(filePath);

                if (isNewFile)
                {
                    // Création d'un tableau JSON valide
                    File.WriteAllText(filePath, "\n" + logData + "\n");
                }
                else
                {
                    File.AppendAllLines(filePath, ["\n" + logData + "\n"]);
                    
                    // Ajout propre dans le tableau JSON existant
                    // using (FileStream fs = new FileStream(filePath, FileMode.Open, FileAccess.ReadWrite))
                    // {
                    //     fs.Seek(-2, SeekOrigin.End); // Recule avant le dernier saut de ligne et crochet "\n]"
                    //     
                    //     byte[] commaAndNewline = Encoding.UTF8.GetBytes(",\n");
                    //     fs.Write(commaAndNewline, 0, commaAndNewline.Length);
                    //     
                    //     byte[] newData = Encoding.UTF8.GetBytes(logData + "\n]");
                    //     fs.Write(newData, 0, newData.Length);
                    // }
                }
            }
        }
    }
}