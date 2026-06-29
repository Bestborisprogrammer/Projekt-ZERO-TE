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
        // NEVER build the default starting party if:
        // 1. We've already initialized once this session (DontDestroyOnLoad means
        //    this should basically only ever happen ONCE per real game session anyway)
        // 2. A save is actively being loaded (SaveManager will populate allMembers itself)
        if (hasInitializedDefaultParty)
        {
            Debug.Log("[PARTY MANAGER] Start - already initialized, skipping default party setup");
            return;
        }

        if (SaveManager.IsLoadingSave)
        {
            Debug.Log("[PARTY MANAGER] Start - save is loading, skipping default party setup " +
                "(SaveManager will populate allMembers/activeParty instead)");
            hasInitializedDefaultParty = true; // mark so we never run default setup even after load finishes
            return;
        }

        Debug.Log("[PARTY MANAGER] Start - building default starting party");
        foreach (var data in partyDataList)
        {
            var instance = new CharacterInstance { baseData = data };
            instance.Initialize();
            allMembers.Add(instance);
        }

        for (int i = 0; i < Mathf.Min(4, allMembers.Count); i++)
            activeParty.Add(allMembers[i]);

        hasInitializedDefaultParty = true;
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