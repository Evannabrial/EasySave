namespace EasySaveLibrary.Model;

public static class PriorityManager
{
    private static int _globalPriorityCount = 0;
    
    // Initialisé à true car au début, il n'y a pas de fichiers prioritaires
    private static readonly ManualResetEventSlim _nonPriorityGate = new ManualResetEventSlim(true);
    private static readonly object _lock = new object();

    public static void AddPriorityFiles(int count)
    {
        if (count <= 0) return;

        lock (_lock)
        {
            _globalPriorityCount += count;
            if (_globalPriorityCount > 0)
            {
                _nonPriorityGate.Reset(); // Feu ROUGE pour les fichiers normaux
            }
        }
    }

    public static void RemovePriorityFiles(int count = 1)
    {
        if (count <= 0) return;

        lock (_lock)
        {
            _globalPriorityCount -= count;
            if (_globalPriorityCount <= 0)
            {
                _globalPriorityCount = 0;
                // Feu VERT pour les fichiers normaux
                _nonPriorityGate.Set();
            }
        }
    }

    public static void WaitForPriorityToFinish(CancellationToken token)
    {
        try
        {
            // Attend que la barrière soit verte. Se réveille si le token d'annulation est déclenché.
            _nonPriorityGate.Wait(token);
        }
        catch (OperationCanceledException)
        {
            // Le job a été annulé pendant l'attente
        }
    }
}