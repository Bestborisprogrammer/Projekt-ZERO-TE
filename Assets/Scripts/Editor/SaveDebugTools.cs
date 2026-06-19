using UnityEngine;
using UnityEditor;
using System.IO;

public class SaveDebugTools
{
    [MenuItem("Tools/Save System/Delete ALL Save Files")]
    public static void DeleteAllSaves()
    {
        string path = Application.persistentDataPath;
        var files = Directory.GetFiles(path, "save_slot_*.json");
        var autosave = Path.Combine(path, "autosave.json");

        int count = 0;
        foreach (var file in files)
        {
            File.Delete(file);
            count++;
        }

        if (File.Exists(autosave))
        {
            File.Delete(autosave);
            count++;
        }

        Debug.Log($"[SAVE DEBUG] Deleted {count} save file(s) from {path}");
    }

    [MenuItem("Tools/Save System/Open Save Folder")]
    public static void OpenSaveFolder()
    {
        EditorUtility.RevealInFinder(Application.persistentDataPath);
    }

    [MenuItem("Tools/Save System/Delete PlayerPrefs (dialogue/encounter flags)")]
    public static void DeletePlayerPrefs()
    {
        PlayerPrefs.DeleteAll();
        Debug.Log("[SAVE DEBUG] PlayerPrefs wiped (dialogue triggers, defeated enemies, etc.)");
    }
}