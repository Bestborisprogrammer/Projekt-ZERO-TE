using UnityEngine;
using System.Collections.Generic;

[System.Serializable]
public class CharacterGear
{
    public string characterName;
    public GearSO weapon;
    public GearSO helmet;
    public GearSO torso;
    public GearSO legs;
    public GearSO feet;
    public GearSO ring1;
    public GearSO ring2;


    public CharacterGear(string name) { characterName = name; }

    public GearSO GetSlot(GearSlot slot, bool isRing2 = false)
    {
        return slot switch
        {
            GearSlot.Weapon => weapon,
            GearSlot.Helmet => helmet,
            GearSlot.Torso => torso,
            GearSlot.Legs => legs,
            GearSlot.Feet => feet,
            GearSlot.Ring => isRing2 ? ring2 : ring1,
            _ => null
        };
    }

    public void SetSlot(GearSlot slot, GearSO gear, bool isRing2 = false)
    {
        switch (slot)
        {
            case GearSlot.Weapon: weapon = gear; break;
            case GearSlot.Helmet: helmet = gear; break;
            case GearSlot.Torso: torso = gear; break;
            case GearSlot.Legs: legs = gear; break;
            case GearSlot.Feet: feet = gear; break;
            case GearSlot.Ring:
                if (isRing2) ring2 = gear;
                else ring1 = gear;
                break;
        }
    }

    public int TotalBonusHP => SumStat(g => g.bonusHP);
    public int TotalBonusATK => SumStat(g => g.bonusATK);
    public int TotalBonusDEF => SumStat(g => g.bonusDEF);
    public int TotalBonusSPD => SumStat(g => g.bonusSPD);
    public int TotalBonusMP => SumStat(g => g.bonusMP);

    int SumStat(System.Func<GearSO, int> selector)
    {
        int total = 0;
        GearSO[] all = { weapon, helmet, torso, legs, feet, ring1, ring2 };
        foreach (var g in all)
            if (g != null) total += selector(g);
        return total;
    }
}

[System.Serializable]
public class GearStack
{
    public GearSO gear;
    public int quantity;

    public GearStack(GearSO g, int qty = 1)
    {
        gear = g;
        quantity = qty;
    }
}

public class GearManager : MonoBehaviour
{
    public static GearManager Instance;
    public List<CharacterGear> allCharacterGear = new();
    public List<GearStack> gearInventory = new(); // tracks quantities

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else Destroy(gameObject);
    }

    public void AddGearToInventory(GearSO gear, int qty = 1)
    {
        var existing = gearInventory.Find(g => g.gear == gear);
        if (existing != null)
            existing.quantity += qty;
        else
            gearInventory.Add(new GearStack(gear, qty));

        Debug.Log($"Added {gear.gearName} x{qty} to gear inventory");
    }

    public void RemoveGearFromInventory(GearSO gear, int qty = 1)
    {
        var existing = gearInventory.Find(g => g.gear == gear);
        if (existing == null) return;
        existing.quantity -= qty;
        if (existing.quantity <= 0)
            gearInventory.Remove(existing);
    }

    public int GetGearQuantity(GearSO gear)
    {
        var existing = gearInventory.Find(g => g.gear == gear);
        return existing?.quantity ?? 0;
    }

    public CharacterGear GetGearFor(string characterName)
    {
        var gear = allCharacterGear.Find(g => g.characterName == characterName);
        if (gear == null)
        {
            gear = new CharacterGear(characterName);
            allCharacterGear.Add(gear);
        }
        return gear;
    }

    public void EquipGear(string characterName, GearSO gear, bool isRing2 = false)
    {
        var charGear = GetGearFor(characterName);

        // Unequip existing and return to inventory
        var current = charGear.GetSlot(gear.slot, isRing2);
        if (current != null)
        {
            AddGearToInventory(current);
            Debug.Log($"Returned {current.gearName} to inventory");
        }

        charGear.SetSlot(gear.slot, gear, isRing2);
        RemoveGearFromInventory(gear);
        Debug.Log($"{characterName} equipped {gear.gearName}!");
    }

    public void UnequipGear(string characterName, GearSlot slot, bool isRing2 = false)
    {
        var charGear = GetGearFor(characterName);
        var current = charGear.GetSlot(slot, isRing2);
        if (current == null) return;

        AddGearToInventory(current);
        charGear.SetSlot(slot, null, isRing2);
        Debug.Log($"{characterName} unequipped {current.gearName}!");
    }
}