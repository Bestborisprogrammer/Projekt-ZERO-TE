using UnityEngine;
using System.Collections.Generic;

public class PartyManager : MonoBehaviour
{
    public static PartyManager Instance;

    public List<CharacterStatsSO> partyDataList;
    public List<CharacterInstance> activeParty = new();
    public List<CharacterInstance> allMembers = new();

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else { Destroy(gameObject); }
    }

    void Start()
    {
        foreach (var data in partyDataList)
        {
            var instance = new CharacterInstance { baseData = data };
            instance.Initialize();
            allMembers.Add(instance);
        }

        // First 4 are default active party
        for (int i = 0; i < Mathf.Min(4, allMembers.Count); i++)
            activeParty.Add(allMembers[i]);
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