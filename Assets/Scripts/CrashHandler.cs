using UnityEngine;

public class CrashHandler : MonoBehaviour
{
    void Start()
    {
        Application.logMessageReceived += HandleLog;
    }

    void HandleLog(string condition, string stackTrace, LogType type)
    {
        if (type == LogType.Exception || type == LogType.Error)
        {
            // Report crash details
            Debug.Log($"Crash Report: {condition}\n{stackTrace}");
        }
    }

    private void OnApplicationQuit()
    {
        Application.logMessageReceived -= HandleLog;
    }
}