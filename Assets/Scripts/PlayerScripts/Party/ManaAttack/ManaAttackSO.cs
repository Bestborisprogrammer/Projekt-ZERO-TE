using UnityEngine;

public enum SpellAffinity { None, Fire, Ice, Thunder, Poison, Dark, Light }
public enum StatusEffectType { None, Freeze, Poison, Burn }

[CreateAssetMenu(fileName = "NewSpell", menuName = "Zero-Te/Party Spell")]
public class ManaAttackSO : ScriptableObject
{
    [Header("Identity")]
    public string spellName = "Fireball";
    public string description = "A basic fire attack.";
    public SpellAffinity affinity = SpellAffinity.Fire;

    [Header("Cost & Requirements")]
    public int manaCost = 10;
    public int levelRequirement = 1;

    [Header("Damage")]
    public int flatDamage = 20;

    [Header("Status Effect")]
    public StatusEffectType statusEffect = StatusEffectType.None;
    [Range(0f, 1f)] public float statusChance = 0f;
    public int statusDuration = 2;
    [Range(0f, 0.1f)] public float dotPercent = 0.05f; // % of max HP for Poison/Burn per turn
}