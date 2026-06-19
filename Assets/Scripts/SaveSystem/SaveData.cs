using System;
using System.Collections.Generic;

[Serializable]
public class SaveData
{
    public string saveName;        // "Edward's Save Slot 1"
    public float playtimeSeconds;
    public string dateTime;        // when it was saved
    public bool isEmpty = true;

    public string sceneName;
    public float playerX, playerY, playerZ;

    public List<CharacterSaveEntry> allMembers = new();
    public List<string> activePartyNames = new();

    public List<InventoryItemSave> inventoryItems = new();
    public List<GearStackSave> gearStacks = new();
    public List<EquippedGearSave> equippedGear = new();

    public int gold;

    public List<string> playerPrefsKeys = new();
    public List<int> playerPrefsValues = new();

    public bool resonanceMeterUnlocked;
    public float resonanceMeterValue;
}

[Serializable]
public class CharacterSaveEntry
{
    public string characterSOName;
    public int level;
    public int currentXP;
    public int xpToNextLevel;
    public int currentHP;
    public int currentMana;
}

[Serializable]
public class InventoryItemSave
{
    public string itemSOName;
    public int quantity;
}

[Serializable]
public class GearStackSave
{
    public string gearSOName;
    public int quantity;
}

[Serializable]
public class EquippedGearSave
{
    public string characterName;
    public string weaponName, helmetName, torsoName, legsName, feetName, ring1Name, ring2Name;
}