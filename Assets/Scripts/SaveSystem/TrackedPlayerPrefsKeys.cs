using System.Collections.Generic;
using UnityEngine;

public static class TrackedPlayerPrefsKeys
{
    private const string MasterListKey = "zzte_tracked_keys_master";

    // AllKnownKeys property so existing code that references it still compiles
    public static IReadOnlyList<string> AllKnownKeys => GetAllTrackedKeys();

    public static void Register(string key)
    {
        var existing = GetMasterList();
        if (!existing.Contains(key))
        {
            existing.Add(key);
            SaveMasterList(existing);
            Debug.Log($"[TRACKED PREFS] Registered new key: {key} (total: {existing.Count})");
        }
    }

    public static List<string> GetAllTrackedKeys()
    {
        return GetMasterList();
    }

    public static void WipeAllTrackedKeys()
    {
        var keys = GetMasterList();
        foreach (var key in keys)
            PlayerPrefs.DeleteKey(key);
        Debug.Log($"[TRACKED PREFS] Wiped {keys.Count} tracked keys");
    }

    static List<string> GetMasterList()
    {
        string raw = PlayerPrefs.GetString(MasterListKey, "");
        if (string.IsNullOrEmpty(raw))
            return new List<string>();
        var list = new List<string>(raw.Split('|'));
        list.RemoveAll(string.IsNullOrEmpty);
        return list;
    }

    static void SaveMasterList(List<string> keys)
    {
        PlayerPrefs.SetString(MasterListKey, string.Join("|", keys));
        PlayerPrefs.Save();
    }

    // Call this on New Game to reset the master list too
    public static void ResetMasterList()
    {
        PlayerPrefs.DeleteKey(MasterListKey);
        PlayerPrefs.Save();
        Debug.Log("[TRACKED PREFS] Master list reset");
    }
}