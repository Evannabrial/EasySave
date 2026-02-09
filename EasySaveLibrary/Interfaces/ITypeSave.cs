using EasyLog;
using EasySaveLibrary.Model;

namespace EasySaveLibrary.Interfaces;

public interface ITypeSave
{
    public int StartSave(Job job, LogType logType);
}