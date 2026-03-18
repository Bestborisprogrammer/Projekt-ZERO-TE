using UnityEngine;

public enum ItemType { Heal, Buff, Debuff }
public enum ItemTarget { Ally, Enemy }
public enum StatType { HP, ATK, DEF, SPD, MP }

[CreateAssetMenu(fileName = "NewItem", menuName = "Zero-Te/Item")]
public class ItemSO : ScriptableObject
{
    [Header("Identity")]
    public string itemName = "Potion";
    public string description = "Restores HP.";
    public Sprite icon;
    public ItemType itemType = ItemType.Heal;
    public ItemTarget itemTarget = ItemTarget.Ally;

    [Header("Shop")]
    public int buyPrice = 50;
    public int sellPrice = 25;

    [Header("Heal (if Heal type)")]
    public int flatHeal = 50;
    [Range(0f, 1f)] public float percentHeal = 0f;

    [Header("Buff/Debuff Stats")]
    public StatType statType = StatType.ATK;
    public int statModifier = 10;
    public int modifierDuration = 3;

    [Header("Debuff Status Effect (optional)")]
    public StatusEffectType statusEffect = StatusEffectType.None;
    [Range(0f, 1f)] public float statusChance = 0f;
    public int statusDuration = 2;
    [Range(0f, 0.1f)] public float dotPercent = 0f;
}