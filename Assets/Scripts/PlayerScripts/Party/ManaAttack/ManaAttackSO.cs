using UnityEngine;

public enum SpellAffinity { None, Fire, Ice, Thunder, Poison, Dark, Light, Water }
public enum StatusEffectType { None, Burn, Poison, Paralyze, Freeze, Wet, Dark, Light }
public enum SpellType { Damage, Heal, Buff, Debuff }

[CreateAssetMenu(fileName = "NewSpell", menuName = "Zero-Te/Party Spell")]
public class ManaAttackSO : ScriptableObject
{
    [Header("Identity")]
    public string spellName = "Fireball";
    public string description = "A basic fire attack.";
    public SpellAffinity affinity = SpellAffinity.Fire;

    [Header("Spell Type")]
    public SpellType spellType = SpellType.Damage;

    [Header("Cost & Requirements")]
    public int manaCost = 10;
    public int levelRequirement = 1;

    [Header("Damage (Damage type only)")]
    public int flatDamage = 20;

    [Header("Heal (Heal type only)")]
    public int flatHeal = 0;
    public float percentHeal = 0f;

    [Header("Buff / Debuff (Buff or Debuff type only)")]
    public StatType statType;
    public int statModifier = 0;
    public int modifierDuration = 3;

    [Header("Status Effect")]
    public StatusEffectType statusEffect = StatusEffectType.None;
    [Range(0f, 1f)] public float statusChance = 0f;
    public int statusDuration = 2;
    [Range(0f, 0.1f)] public float dotPercent = 0.05f;

    [Header("Dark Specific")]
    [Range(0f, 1f)] public float defenseReduction = 0.25f;
}