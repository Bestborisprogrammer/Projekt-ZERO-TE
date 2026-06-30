using System.Collections.Generic;
using UnityEngine;

public static class TrackedPlayerPrefsKeys
{
    private static List<string> registeredKeys = new();

    public static IReadOnlyList<string> AllKnownKeys => registeredKeys;

    public static void Register(string key)
    {
        if (!registeredKeys.Contains(key))
            registeredKeys.Add(key);
    }
}