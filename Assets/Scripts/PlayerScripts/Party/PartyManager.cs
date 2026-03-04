using UnityEngine;
using System.Collections.Generic;

public class PartyManager : MonoBehaviour
{
    public static PartyManager Instance;

    public List<CharacterStatsSO> partyDataList;        // Im Inspector zuweisen
    public List<CharacterInstance> activeParty = new(); // Runtime Daten

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
            activeParty.Add(instance);
        }
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