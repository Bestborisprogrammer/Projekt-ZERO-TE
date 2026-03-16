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
    public List<StatModifier> statModifiers = new();

    public bool isFrozen => activeEffects.Exists(e => e.type == StatusEffectType.Freeze && e.turnsRemaining > 0);
    public bool isWet => activeEffects.Exists(e => e.type == StatusEffectType.Wet && e.turnsRemaining > 0);
    public bool isBurning => activeEffects.Exists(e => e.type == StatusEffectType.Burn && e.turnsRemaining > 0);
    public bool isParalyzed => activeEffects.Exists(e => e.type == StatusEffectType.Paralyze && e.turnsRemaining > 0);

    CharacterGear Gear => GearManager.Instance?.GetGearFor(baseData.characterName);

    public int MaxHP
    {
        get
        {
            int val = baseData.maxHP + (level - 1) * baseData.hpGrowth;
            val += Gear?.TotalBonusHP ?? 0;
            val += statModifiers.Where(m => m.statType == StatType.HP).Sum(m => m.modifier);
            return val;
        }
    }

    public int Attack
    {
        get
        {
            int val = baseData.attack + (level - 1) * baseData.attackGrowth;
            val += Gear?.TotalBonusATK ?? 0;
            val += statModifiers.Where(m => m.statType == StatType.ATK).Sum(m => m.modifier);
            return Mathf.Max(0, val);
        }
    }

    public int Defense
    {
        get
        {
            int val = baseData.defense + (level - 1) * baseData.defenseGrowth;
            val += Gear?.TotalBonusDEF ?? 0;
            var darkEffect = activeEffects.FirstOrDefault(e => e.type == StatusEffectType.Dark);
            if (darkEffect != null)
                val = Mathf.RoundToInt(val * (1f - darkEffect.defenseReduction));
            val += statModifiers.Where(m => m.statType == StatType.DEF).Sum(m => m.modifier);
            return Mathf.Max(0, val);
        }
    }

    public int Speed
    {
        get
        {
            int val = baseData.speed + (level - 1) * baseData.speedGrowth;
            val += Gear?.TotalBonusSPD ?? 0;
            var wetEffect = activeEffects.FirstOrDefault(e => e.type == StatusEffectType.Wet);
            if (wetEffect != null)
                val = Mathf.Max(1, val - wetEffect.speedReduction);
            val += statModifiers.Where(m => m.statType == StatType.SPD).Sum(m => m.modifier);
            return Mathf.Max(1, val);
        }
    }

    public int MaxMana
    {
        get
        {
            int val = baseData.maxMana + (level - 1) * baseData.manaGrowth;
            val += Gear?.TotalBonusMP ?? 0;
            val += statModifiers.Where(m => m.statType == StatType.MP).Sum(m => m.modifier);
            return val;
        }
    }

    public string Name => baseData.characterName;
    public bool IsAlive => currentHP > 0;
    public CombatStyle CombatStyle => baseData.combatStyle;
    public float BlockReduction => Mathf.Min(0.9f, 0.30f + (Defense * 0.002f));
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

    public void HealHP(int flatAmount, float percentAmount)
    {
        int heal = flatAmount + Mathf.RoundToInt(MaxHP * percentAmount);
        currentHP = Mathf.Min(MaxHP, currentHP + heal);
        Debug.Log($"{Name} healed for {heal} HP! ({currentHP}/{MaxHP})");
    }

    public void ApplyStatModifier(StatType type, int modifier, int duration)
    {
        statModifiers.Add(new StatModifier(type, modifier, duration));
        Debug.Log($"{Name} stat modifier: {type} {modifier:+#;-#} for {duration} turns");
    }

    public void TickStatModifiers()
    {
        for (int i = statModifiers.Count - 1; i >= 0; i--)
        {
            statModifiers[i].turnsRemaining--;
            if (statModifiers[i].turnsRemaining <= 0)
            {
                Debug.Log($"{Name}'s {statModifiers[i].statType} modifier wore off!");
                statModifiers.RemoveAt(i);
            }
        }
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