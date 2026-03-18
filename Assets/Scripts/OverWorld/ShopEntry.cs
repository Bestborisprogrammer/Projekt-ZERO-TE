using UnityEngine;

public enum ShopEntryType { Item, Gear }

[System.Serializable]
public class ShopEntry
{
    public ShopEntryType entryType;
    public ItemSO item;
    public GearSO gear;
    public int stock = -1; // -1 = unlimited
}