using EasyLog;
using EasySaveLibrary.Model;

namespace EasySaveLibrary.Interfaces;

public interface ITypeSave
{
    string DisplayName { get; }
    int StartSave(Job job, LogType logType, ManualResetEvent pauseEvent, bool enableEncryption = false, string encryptionExtensions = "");
}