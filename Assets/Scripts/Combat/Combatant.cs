using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class Combatant
{
    public string Name { get; private set; }
    public int Speed { get; private set; }
    public int Attack { get; private set; }
    public int Defense { get; private set; }
    public int MaxHP { get; private set; }
    public int CurrentHP { get; private set; }
    public int XPReward { get; private set; }
    public bool IsEnemy { get; private set; }
    public bool IsAlive => CurrentHP > 0;
    public bool IsFrozen { get; private set; }
    public bool IsWet { get; private set; }
    public bool IsBurning { get; private set; }
    public bool IsParalyzed { get; private set; }
    public List<SpellAffinity> Affinities { get; private set; }

    private CharacterInstance characterRef;
    private EnemyInstance enemyRef;

    public Combatant(CharacterInstance c)
    {
        characterRef = c;
        IsEnemy = false;
        XPReward = 0;
        Refresh();
    }

    public Combatant(EnemyInstance e)
    {
        enemyRef = e;
        IsEnemy = true;
        XPReward = e.XPReward;
        Refresh();
    }

    public void TakeDamage(int damage)
    {
        if (IsEnemy) enemyRef.TakeDamage(damage);
        else characterRef.TakeDamage(damage);
        Refresh();
    }

    public void ApplyStatusEffect(StatusEffectType type, float chance, int duration, float dotPercent = 0f, float defenseReduction = 0f, int speedReduction = 0)
    {
        if (IsEnemy) enemyRef.ApplyStatusEffect(type, chance, duration, dotPercent, defenseReduction, speedReduction);
        else characterRef.ApplyStatusEffect(type, chance, duration, dotPercent, defenseReduction, speedReduction);
        Refresh();
    }

    public bool HasStatusEffect(StatusEffectType type)
    {
        List<ActiveStatusEffect> effects = IsEnemy ? enemyRef.activeEffects : characterRef.activeEffects;
        return effects.Exists(e => e.type == type && e.turnsRemaining > 0);
    }

    public void RemoveStatusEffect(StatusEffectType type)
    {
        List<ActiveStatusEffect> effects = IsEnemy ? enemyRef.activeEffects : characterRef.activeEffects;
        effects.RemoveAll(e => e.type == type);
        Refresh();
    }

    public List<string> ProcessStatusEffects()
    {
        List<string> logs = new();
        List<ActiveStatusEffect> effects = IsEnemy ? enemyRef.activeEffects : characterRef.activeEffects;

        for (int i = effects.Count - 1; i >= 0; i--)
        {
            var effect = effects[i];

            if (effect.type == StatusEffectType.Burn || effect.type == StatusEffectType.Poison)
            {
                int dotDamage;
                if (effect.type == StatusEffectType.Poison)
                    // Poison ignores defense
                    dotDamage = Mathf.Max(1, Mathf.RoundToInt(MaxHP * effect.dotPercent));
                else
                    // Burn goes through normal damage
                    dotDamage = Mathf.Max(1, Mathf.RoundToInt(MaxHP * effect.dotPercent));

                TakeDamage(dotDamage);
                effect.turnsRemaining--;

                if (effect.turnsRemaining <= 0)
                {
                    logs.Add($"{Name} takes {dotDamage} {effect.type} damage! {effect.type} wore off!");
                    effects.RemoveAt(i);
                }
                else
                {
                    logs.Add($"{Name} takes {dotDamage} {effect.type} damage! ({effect.turnsRemaining} turns remaining)");
                }
            }
            else if (effect.type == StatusEffectType.Wet)
            {
                effect.turnsRemaining--;
                if (effect.turnsRemaining <= 0)
                {
                    logs.Add($"{Name}'s Wet effect wore off! Speed returns to normal.");
                    Debug.Log($"[WET] {Name}'s Wet wore off!");
                    effects.RemoveAt(i);
                }
                else
                {
                    logs.Add($"{Name} is Wet! Speed reduced by {effect.speedReduction} ({effect.turnsRemaining} turns remaining)");
                    Debug.Log($"[WET] {Name} Speed reduced by {effect.speedReduction} | {effect.turnsRemaining} turns remaining");
                }
            }
            else if (effect.type == StatusEffectType.Paralyze)
            {
                effect.turnsRemaining--;
                if (effect.turnsRemaining <= 0)
                {
                    logs.Add($"{Name}'s paralysis wore off!");
                    effects.RemoveAt(i);
                }
            }
        }

        Refresh();
        return logs;
    }

    public bool ConsumeFreezeIfActive()
    {
        List<ActiveStatusEffect> effects = IsEnemy ? enemyRef.activeEffects : characterRef.activeEffects;
        var freeze = effects.Find(e => e.type == StatusEffectType.Freeze);
        if (freeze == null) return false;
        freeze.turnsRemaining--;
        if (freeze.turnsRemaining <= 0) effects.Remove(freeze);
        Refresh();
        return true;
    }

    public void Refresh()
    {
        if (IsEnemy)
        {
            Name = enemyRef.Name;
            Speed = enemyRef.Speed;
            Attack = enemyRef.Attack;
            Defense = enemyRef.Defense;
            MaxHP = enemyRef.MaxHP;
            CurrentHP = enemyRef.currentHP;
            IsFrozen = enemyRef.isFrozen;
            IsWet = enemyRef.isWet;
            IsBurning = enemyRef.isBurning;
            IsParalyzed = enemyRef.isParalyzed;
            Affinities = enemyRef.baseData.affinities;
        }
        else
        {
            Name = characterRef.Name;
            Speed = characterRef.Speed;
            Attack = characterRef.Attack;
            Defense = characterRef.Defense;
            MaxHP = characterRef.MaxHP;
            CurrentHP = characterRef.currentHP;
            IsFrozen = characterRef.isFrozen;
            IsWet = characterRef.isWet;
            IsBurning = characterRef.isBurning;
            IsParalyzed = characterRef.isParalyzed;
            Affinities = characterRef.baseData.affinities;
        }
    }

    public List<ManaAttackSO> GetPartySpells(int level)
    {
        if (IsEnemy) return null;
        return characterRef.baseData.spells.FindAll(s => s.levelRequirement <= level);
    }

    public int GetCurrentMana()
    {
        return IsEnemy ? enemyRef.currentMana : characterRef.currentMana;
    }

    public int GetCurrentLevel()
    {
        return IsEnemy ? enemyRef.Level : characterRef.level;
    }

    public bool SpendMana(int cost)
    {
        if (IsEnemy) return enemyRef.UseMana(cost);
        return characterRef.UseMana(cost);
    }
}