using UnityEngine;

[CreateAssetMenu(fileName = "NewEnemySpell", menuName = "Zero-Te/Enemy Spell")]
public class EnemyManaAttackSO : ScriptableObject
{
    [Header("Identity")]
    public string spellName = "Venom Spit";
    public string description = "Spits venom at a target.";
    public SpellAffinity affinity = SpellAffinity.Poison;

    [Header("Cost")]
    public int manaCost = 8;

    [Header("Damage")]
    public int flatDamage = 15;

    [Header("Status Effect")]
    public StatusEffectType statusEffect = StatusEffectType.None;
    [Range(0f, 1f)] public float statusChance = 0f;
    public int statusDuration = 2;
    [Range(0f, 0.1f)] public float dotPercent = 0.05f;

    [Header("Dark Specific")]
    [Range(0f, 1f)] public float defenseReduction = 0.25f;
}