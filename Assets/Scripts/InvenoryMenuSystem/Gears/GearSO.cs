using UnityEngine;

public enum GearSlot { Weapon, Helmet, Torso, Legs, Feet, Ring }

[CreateAssetMenu(fileName = "NewGear", menuName = "Zero-Te/Gear")]
public class GearSO : ScriptableObject
{
    [Header("Identity")]
    public string gearName = "Iron Sword";
    public string description = "A basic sword.";
    public Sprite icon;
    public GearSlot slot = GearSlot.Weapon;

    [Header("Shop")]
    public int buyPrice = 100;
    public int sellPrice = 50;

    [Header("Stat Bonuses")]
    public int bonusHP = 0;
    public int bonusATK = 0;
    public int bonusDEF = 0;
    public int bonusSPD = 0;
    public int bonusMP = 0;
}