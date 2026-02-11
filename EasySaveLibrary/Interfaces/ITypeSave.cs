using EasyLog;
using EasySaveLibrary.Model;

namespace EasySaveLibrary.Interfaces;

public interface ITypeSave
{
    string DisplayName { get; }
    public int StartSave(Job job, LogType logType);
}