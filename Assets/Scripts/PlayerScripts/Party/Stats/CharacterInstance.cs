using UnityEngine;
using System.Collections.Generic;

[System.Serializable]
public class CharacterInstance
{
    public CharacterStatsSO baseData;

    public int currentHP;
    public int currentMana;
    public int level = 1;
    public int currentXP = 0;
    public int xpToNextLevel;

    public List<ActiveStatusEffect> activeEffects = new();
    public bool isFrozen => activeEffects.Exists(e => e.type == StatusEffectType.Freeze && e.turnsRemaining > 0);

    public int MaxHP => baseData.maxHP + (level - 1) * baseData.hpGrowth;
    public int Attack => baseData.attack + (level - 1) * baseData.attackGrowth;
    public int Defense => baseData.defense + (level - 1) * baseData.defenseGrowth;
    public int Speed => baseData.speed + (level - 1) * baseData.speedGrowth;
    public int MaxMana => baseData.maxMana + (level - 1) * baseData.manaGrowth;
    public string Name => baseData.characterName;
    public bool IsAlive => currentHP > 0;

    public void Initialize()
    {
        level = baseData.startingLevel;
        currentHP = MaxHP;
        currentMana = MaxMana;
        xpToNextLevel = baseData.baseXPToNextLevel;

        // Scale XP threshold for starting level
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

    public void ApplyStatusEffect(StatusEffectType type, float chance, int duration, float dotPercent)
    {
        if (UnityEngine.Random.value <= chance)
        {
            // Remove existing same effect and replace
            activeEffects.RemoveAll(e => e.type == type);
            activeEffects.Add(new ActiveStatusEffect(type, duration, dotPercent));
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
        Debug.Log($"🎉 {Name} reached Level {level}!");
    }
}