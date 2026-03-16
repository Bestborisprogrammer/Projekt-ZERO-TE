using UnityEngine;
using System.Collections.Generic;
using System.Linq;

[System.Serializable]
public class CharacterInstance
{
    public CharacterStatsSO baseData;

    public int currentHP;
    public int currentMana;
    public int level = 1;
    public int currentXP = 0;
    public int xpToNextLevel;
    public bool isBlocking = false;

    public List<ActiveStatusEffect> activeEffects = new();
    public bool isFrozen => activeEffects.Exists(e => e.type == StatusEffectType.Freeze && e.turnsRemaining > 0);
    public bool isWet => activeEffects.Exists(e => e.type == StatusEffectType.Wet && e.turnsRemaining > 0);
    public bool isBurning => activeEffects.Exists(e => e.type == StatusEffectType.Burn && e.turnsRemaining > 0);
    public bool isParalyzed => activeEffects.Exists(e => e.type == StatusEffectType.Paralyze && e.turnsRemaining > 0);

    public int MaxHP => baseData.maxHP + (level - 1) * baseData.hpGrowth;
    public int Attack => baseData.attack + (level - 1) * baseData.attackGrowth;

    public int Defense
    {
        get
        {
            int def = baseData.defense + (level - 1) * baseData.defenseGrowth;
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
            int spd = baseData.speed + (level - 1) * baseData.speedGrowth;
            var wetEffect = activeEffects.FirstOrDefault(e => e.type == StatusEffectType.Wet);
            if (wetEffect != null)
                spd = Mathf.Max(1, spd - wetEffect.speedReduction);
            return spd;
        }
    }

    public int MaxMana => baseData.maxMana + (level - 1) * baseData.manaGrowth;
    public string Name => baseData.characterName;
    public bool IsAlive => currentHP > 0;
    public CombatStyle CombatStyle => baseData.combatStyle;

    // Block reduction: 30% base + (defense * 0.2%)
    public float BlockReduction => Mathf.Min(0.9f, 0.30f + (Defense * 0.002f));

    // Evade chance: 20% base + (speed * 0.2%)
    public float EvadeChance => Mathf.Min(0.9f, 0.20f + (Speed * 0.002f));

    public void Initialize()
    {
        level = baseData.startingLevel;
        currentHP = MaxHP;
        currentMana = MaxMana;
        xpToNextLevel = baseData.baseXPToNextLevel;
        for (int i = 1; i < level; i++)
            xpToNextLevel = Mathf.RoundToInt(xpToNextLevel * 1.4f);
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

    public void GainXP(int amount)
    {
        currentXP += amount;
        while (currentXP >= xpToNextLevel)
            LevelUp();
    }

    private void LevelUp()
    {
        currentXP -= xpToNextLevel;
        level++;
        xpToNextLevel = Mathf.RoundToInt(xpToNextLevel * 1.4f);
        currentHP = MaxHP;
        currentMana = MaxMana;
        Debug.Log($"{Name} reached Level {level}!");
    }
}