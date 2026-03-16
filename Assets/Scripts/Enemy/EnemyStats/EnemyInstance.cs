using UnityEngine;
using System.Collections.Generic;
using System.Linq;

[System.Serializable]
public class EnemyInstance
{
    public EnemyStatsSO baseData;

    public int currentHP;
    public int currentMana;
    public bool isBlocking = false;

    public List<ActiveStatusEffect> activeEffects = new();
    public bool isFrozen => activeEffects.Exists(e => e.type == StatusEffectType.Freeze && e.turnsRemaining > 0);
    public bool isWet => activeEffects.Exists(e => e.type == StatusEffectType.Wet && e.turnsRemaining > 0);
    public bool isBurning => activeEffects.Exists(e => e.type == StatusEffectType.Burn && e.turnsRemaining > 0);
    public bool isParalyzed => activeEffects.Exists(e => e.type == StatusEffectType.Paralyze && e.turnsRemaining > 0);

    public string Name => baseData.enemyName;
    public int Level => baseData.level;
    public int MaxHP => baseData.maxHP;
    public int Attack => baseData.attack;

    public int Defense
    {
        get
        {
            int def = baseData.defense;
            var darkEffect = activeEffects.FirstOrDefault(e => e.type == StatusEffectType.Dark);
            if (darkEffect != null)
                def = Mathf.RoundToInt(def * (1f - darkEffect.defenseReduction));
            return Mathf.Max(0, def);
        }
    }

    public int Speed
    {
        get
        {
            int spd = baseData.speed;
            var wetEffect = activeEffects.FirstOrDefault(e => e.type == StatusEffectType.Wet);
            if (wetEffect != null)
                spd = Mathf.Max(1, spd - wetEffect.speedReduction);
            return spd;
        }
    }

    public int MaxMana => baseData.maxMana;
    public int XPReward => baseData.xpReward;
    public bool IsAlive => currentHP > 0;
    public CombatStyle CombatStyle => baseData.combatStyle;

    public float BlockReduction => Mathf.Min(0.9f, 0.30f + (Defense * 0.002f));
    public float EvadeChance => Mathf.Min(0.9f, 0.20f + (Speed * 0.002f));

    public void Initialize()
    {
        currentHP = MaxHP;
        currentMana = MaxMana;
    }

    public void TakeDamage(int damage)
    {
        currentHP = Mathf.Max(0, currentHP - damage);
    }

    public bool UseMana(int cost)
    {
        if (currentMana < cost) return false;
        currentMana -= cost;
        return true;
    }

    public void ApplyStatusEffect(StatusEffectType type, float chance, int duration, float dotPercent = 0f, float defenseReduction = 0f, int speedReduction = 0)
    {
        if (UnityEngine.Random.value <= chance)
        {
            activeEffects.RemoveAll(e => e.type == type);
            activeEffects.Add(new ActiveStatusEffect(type, duration, dotPercent, defenseReduction, speedReduction));
        }
    }
}