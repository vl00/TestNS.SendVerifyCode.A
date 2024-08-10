using System;

namespace Common.SimpleJobs;

public interface IJobLogger
{
    void LogDebug(string msg, params object[] args);
    void LogInfo(string msg, params object[] args);    
    void LogWarn(string msg, params object[] args);
    void LogError(string msg, params object[] args);
    void LogError(Exception ex, string msg, params object[] args);

    void LogInformation(string msg, params object[] args) => LogInfo(msg, args);
    void LogWarning(string msg, params object[] args) => LogWarn(msg, args);
}
