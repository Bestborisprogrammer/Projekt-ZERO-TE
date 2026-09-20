using UnityEngine;
using System.Collections.Generic;

public class PartyManager : MonoBehaviour
{
    public static PartyManager Instance;
    public List<CharacterStatsSO> partyDataList;
    public List<CharacterInstance> activeParty = new();
    public List<CharacterInstance> allMembers = new();

    private bool hasInitializedDefaultParty = false;

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            Debug.Log("[PARTY MANAGER] Awake - Instance set (new persistent instance)");
        }
        else
        {
            Debug.Log("[PARTY MANAGER] Awake - duplicate found, destroying this one");
            Destroy(gameObject);
        }
    }

    void Start()
    {
        if (SaveManager.IsLoadingSave)
        {
            Debug.Log("[PARTY MANAGER] Start - save loading, skipping default party");
            hasInitializedDefaultParty = true;
            return;
        }

        if (hasInitializedDefaultParty)
        {
            Debug.Log("[PARTY MANAGER] Start - already initialized");
            return;
        }

        BuildDefaultParty();
    }

    // PUBLIC so MainMenuController can call it on New Game
    // to force-rebuild the party on the persistent instance
    public void BuildDefaultParty()
    {
        Debug.Log("[PARTY MANAGER] BuildDefaultParty called");
        allMembers.Clear();
        activeParty.Clear();

        foreach (var data in partyDataList)
        {
            var instance = new CharacterInstance { baseData = data };
            instance.Initialize();
            allMembers.Add(instance);
        }

        for (int i = 0; i < Mathf.Min(4, allMembers.Count); i++)
            activeParty.Add(allMembers[i]);

        hasInitializedDefaultParty = true;
        Debug.Log($"[PARTY MANAGER] Default party built: {string.Join(",", activeParty.ConvertAll(m => m.Name))}");
    }

    // Call this to fully reset for New Game
    public void ResetForNewGame()
    {
        hasInitializedDefaultParty = false;
        BuildDefaultParty();
    }

    public bool IsGameOver()
    {
        foreach (var member in activeParty)
            if (member.IsAlive) return false;
        return true;
    }

    public void GiveXPToAll(int xp)
    {
        foreach (var member in activeParty)
            if (member.IsAlive) member.GainXP(xp);
    }
}