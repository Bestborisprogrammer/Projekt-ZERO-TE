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
    public bool IsBlocking { get; private set; }
    public bool IsEvading { get; private set; }
    public CombatStyle CombatStyle { get; private set; }
    public float BlockReduction { get; private set; }
    public float EvadeChance { get; private set; }
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

    public bool TryEvade()
    {
        if (CombatStyle != CombatStyle.Evade) return false;
        if (!IsEvading) return false;
        return Random.value < EvadeChance;
    }

    public void TakeDamage(int damage)
    {
        if (IsBlocking)
        {
            int reduced = Mathf.RoundToInt(damage * (1f - BlockReduction));
            damage = reduced;
        }
        if (IsEnemy) enemyRef.TakeDamage(damage);
        else characterRef.TakeDamage(damage);
        Refresh();
    }

    public void SetBlocking(bool value)
    {
        if (IsEnemy) enemyRef.isBlocking = value;
        else characterRef.isBlocking = value;
        Refresh();
    }

    public void SetEvading(bool value)
    {
        if (IsEnemy) enemyRef.isEvading = value;
        else characterRef.isEvading = value;
        Refresh();
    }

    public void ApplyStatusEffect(StatusEffectType type, float chance, int duration,
        float dotPercent = 0f, float defenseReduction = 0f, int speedReduction = 0)
    {
        if (IsEnemy)
            enemyRef.ApplyStatusEffect(type, chance, duration, dotPercent, defenseReduction, speedReduction);
        else
            characterRef.ApplyStatusEffect(type, chance, duration, dotPercent, defenseReduction, speedReduction);
        Refresh();
    }

    public bool HasStatusEffect(StatusEffectType type)
    {
        List<ActiveStatusEffect> effects = IsEnemy
            ? enemyRef.activeEffects
            : characterRef.activeEffects;
        return effects.Exists(e => e.type == type && e.turnsRemaining > 0);
    }

    public void RemoveStatusEffect(StatusEffectType type)
    {
        List<ActiveStatusEffect> effects = IsEnemy
            ? enemyRef.activeEffects
            : characterRef.activeEffects;
        effects.RemoveAll(e => e.type == type);
        Refresh();
    }

    // Returns logs AND dot damage amounts for damage numbers
    public List<(string log, int damage, bool isDot)> ProcessStatusEffectsDetailed()
    {
        List<(string, int, bool)> results = new();
        List<ActiveStatusEffect> effects = IsEnemy
            ? enemyRef.activeEffects
            : characterRef.activeEffects;

        for (int i = effects.Count - 1; i >= 0; i--)
        {
            var effect = effects[i];

            if (effect.type == StatusEffectType.Burn || effect.type == StatusEffectType.Poison)
            {
                int dotDamage = Mathf.Max(1, Mathf.RoundToInt(MaxHP * effect.dotPercent));
                if (IsEnemy) enemyRef.TakeDamage(dotDamage);
                else characterRef.TakeDamage(dotDamage);
                effect.turnsRemaining--;

                if (effect.turnsRemaining <= 0)
                {
                    results.Add(($"{Name} takes {dotDamage} {effect.type} damage! {effect.type} wore off!", dotDamage, true));
                    effects.RemoveAt(i);
                }
                else
                    results.Add(($"{Name} takes {dotDamage} {effect.type} damage! ({effect.turnsRemaining} turns remaining)", dotDamage, true));
            }
            else if (effect.type == StatusEffectType.Wet)
            {
                effect.turnsRemaining--;
                if (effect.turnsRemaining <= 0)
                {
                    results.Add(($"{Name}'s Wet effect wore off!", 0, false));
                    effects.RemoveAt(i);
                }
                else
                    results.Add(($"{Name} is Wet! Speed reduced. ({effect.turnsRemaining} turns remaining)", 0, false));
            }
            else if (effect.type == StatusEffectType.Dark)
            {
                effect.turnsRemaining--;
                if (effect.turnsRemaining <= 0)
                {
                    results.Add(($"{Name}'s Defense reduction wore off!", 0, false));
                    effects.RemoveAt(i);
                }
                else
                    results.Add(($"{Name}'s Defense is reduced! ({effect.turnsRemaining} turns remaining)", 0, false));
            }
            else if (effect.type == StatusEffectType.Paralyze)
            {
                effect.turnsRemaining--;
                if (effect.turnsRemaining <= 0)
                {
                    results.Add(($"{Name}'s paralysis wore off!", 0, false));
                    effects.RemoveAt(i);
                }
                else
                    results.Add(($"{Name} is paralyzed! ({effect.turnsRemaining} turns remaining)", 0, false));
            }
        }

        Refresh();
        return results;
    }

    // Keep old method for compatibility
    public List<string> ProcessStatusEffects()
    {
        var detailed = ProcessStatusEffectsDetailed();
        return detailed.ConvertAll(d => d.log);
    }

    public bool ConsumeFreezeIfActive()
    {
        List<ActiveStatusEffect> effects = IsEnemy
            ? enemyRef.activeEffects
            : characterRef.activeEffects;
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
            IsBlocking = enemyRef.isBlocking;
            IsEvading = enemyRef.isEvading;
            CombatStyle = enemyRef.CombatStyle;
            BlockReduction = enemyRef.BlockReduction;
            EvadeChance = enemyRef.EvadeChance;
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
            IsBlocking = characterRef.isBlocking;
            IsEvading = characterRef.isEvading;
            CombatStyle = characterRef.CombatStyle;
            BlockReduction = characterRef.BlockReduction;
            EvadeChance = characterRef.EvadeChance;
            Affinities = characterRef.baseData.affinities;
        }
    }

    public List<ManaAttackSO> GetPartySpells(int level)
    {
        if (IsEnemy) return null;
        return characterRef.baseData.spells.FindAll(s => s.levelRequirement <= level);
    }

    public int GetCurrentMana() => IsEnemy ? enemyRef.currentMana : characterRef.currentMana;
    public int GetCurrentLevel() => IsEnemy ? enemyRef.Level : characterRef.level;

    public bool SpendMana(int cost)
    {
        if (IsEnemy) return enemyRef.UseMana(cost);
        return characterRef.UseMana(cost);
    }
}