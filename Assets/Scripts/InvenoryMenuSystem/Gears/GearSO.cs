using UnityEngine;

public enum GearSlot { Weapon, Helmet, Torso, Legs, Feet, Ring }

[CreateAssetMenu(fileName = "NewGear", menuName = "Zero-Te/Gear")]
public class GearSO : ScriptableObject
{
    [Header("Identity")]
    public string gearName = "Gear";
    public string description = "";
    public GearSlot slot;
    public Sprite icon;

    [Header("Stat Bonuses")]
    public int bonusHP = 0;
    public int bonusATK = 0;
    public int bonusDEF = 0;
    public int bonusSPD = 0;
    public int bonusMP = 0;
    public int bonusMAG = 0;

    [Header("Price")]
    public int buyPrice = 100;
    public int sellPrice = 50;
}