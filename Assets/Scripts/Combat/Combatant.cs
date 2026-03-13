using System.Collections.Generic;
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

    public void ApplyStatusEffect(StatusEffectType type, float chance, int duration, float dotPercent)
    {
        if (IsEnemy) enemyRef.ApplyStatusEffect(type, chance, duration, dotPercent);
        else characterRef.ApplyStatusEffect(type, chance, duration, dotPercent);
        Refresh();
    }

    public bool HasStatusEffect(StatusEffectType type)
    {
        List<ActiveStatusEffect> effects = IsEnemy ? enemyRef.activeEffects : characterRef.activeEffects;
        return effects.Exists(e => e.type == type && e.turnsRemaining > 0);
    }

    // Called AFTER the combatant has taken their action
    public List<string> ProcessStatusEffects()
    {
        List<string> logs = new();
        List<ActiveStatusEffect> effects = IsEnemy ? enemyRef.activeEffects : characterRef.activeEffects;

        for (int i = effects.Count - 1; i >= 0; i--)
        {
            var effect = effects[i];

            if (effect.type == StatusEffectType.Poison || effect.type == StatusEffectType.Burn)
            {
                int dotDamage = Mathf.Max(1, Mathf.RoundToInt(MaxHP * effect.dotPercent));
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
        if (freeze.turnsRemaining <= 0)
            effects.Remove(freeze);

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