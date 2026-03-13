using UnityEngine;
using System.Collections.Generic;

[System.Serializable]
public class EnemyInstance
{
    public EnemyStatsSO baseData;

    public int currentHP;
    public int currentMana;

    public List<ActiveStatusEffect> activeEffects = new();
    public bool isFrozen => activeEffects.Exists(e => e.type == StatusEffectType.Freeze && e.turnsRemaining > 0);

    public string Name => baseData.enemyName;
    public int Level => baseData.level;
    public int MaxHP => baseData.maxHP;
    public int Attack => baseData.attack;
    public int Defense => baseData.defense;
    public int Speed => baseData.speed;
    public int MaxMana => baseData.maxMana;
    public int XPReward => baseData.xpReward;
    public bool IsAlive => currentHP > 0;

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

    public void ApplyStatusEffect(StatusEffectType type, float chance, int duration, float dotPercent)
    {
        if (UnityEngine.Random.value <= chance)
        {
            activeEffects.RemoveAll(e => e.type == type);
            activeEffects.Add(new ActiveStatusEffect(type, duration, dotPercent));
        }
    }
}