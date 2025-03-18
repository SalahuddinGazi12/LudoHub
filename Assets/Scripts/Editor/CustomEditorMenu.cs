using System.IO;
using UnityEditor;
using UnityEngine;

namespace Editor
{
    public static class CustomEditorMenu
    {
        [MenuItem("Ludo/Delete All Data")]
        private static void DeleteAllData()
        {
            var tempUrl = Path.Combine(Application.persistentDataPath, Application.productName, "_db");

            if (Directory.Exists(tempUrl))
            {
                Directory.Delete(tempUrl, true);
            }

            PlayerPrefs.DeleteAll();

            Debug.Log("All Data Deleted");
        }


        [MenuItem("Ludo/Open Persistent Data Path")]
        private static void OpenFileDirectory()
        {
            string path = Path.Combine(Application.persistentDataPath);

            // Ensure the directory exists
            if (!Directory.Exists(path))
            {
                Directory.CreateDirectory(path);
            }

            switch (Application.platform)
            {
                // Handle opening the directory in Windows and macOS
                case RuntimePlatform.WindowsEditor:
                    System.Diagnostics.Process.Start("explorer.exe", path.Replace("/", "\\"));
                    break;
                case RuntimePlatform.OSXEditor:
                    System.Diagnostics.Process.Start("open", path);
                    break;
                
                default:
                    Debug.LogError("Not working, LOL");
                    break;
            }
        }
    }
}