using System.Threading;
namespace EasySaveLibrary.Model;

/// <summary>
/// Le sémaphore limite le passage à 1 seul thread en même temps pour les gros fichiers
/// </summary>
public class TransferManager
{
    public static readonly SemaphoreSlim LargeFileSemaphore = new SemaphoreSlim(1, 1);
}