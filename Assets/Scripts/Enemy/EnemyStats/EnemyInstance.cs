using UnityEngine;

[System.Serializable]
public class EnemyInstance
{
    public EnemyStatsSO baseData;

    public int currentHP;
    public int currentMana;

    public string Name => baseData.enemyName;
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
        Debug.Log($"{Name} took {damage} damage! HP: {currentHP}/{MaxHP}");
    }

    public bool UseMana(int cost)
    {
        if (currentMana < cost) return false;
        currentMana -= cost;
        return true;
    }
}