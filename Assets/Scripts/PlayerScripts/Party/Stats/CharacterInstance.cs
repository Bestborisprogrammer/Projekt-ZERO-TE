using UnityEngine;

[System.Serializable]
public class CharacterInstance
{
    public CharacterStatsSO baseData;

    public int currentHP;
    public int currentMana;
    public int level = 1;
    public int currentXP = 0;
    public int xpToNextLevel;

    public int MaxHP => baseData.maxHP + (level - 1) * baseData.hpGrowth;
    public int Attack => baseData.attack + (level - 1) * baseData.attackGrowth;
    public int Defense => baseData.defense + (level - 1) * baseData.defenseGrowth;
    public int Speed => baseData.speed + (level - 1) * baseData.speedGrowth;
    public int MaxMana => baseData.maxMana + (level - 1) * baseData.manaGrowth;
    public string Name => baseData.characterName;
    public bool IsAlive => currentHP > 0;

    public void Initialize()
    {
        currentHP = MaxHP;
        currentMana = MaxMana;
        xpToNextLevel = baseData.baseXPToNextLevel;
    }

    public void TakeDamage(int damage)
    {
        currentHP = Mathf.Max(0, currentHP - damage);
        Debug.Log($"{Name} took {damage} damage! HP: {currentHP}/{MaxHP}");
    }

    public bool UseMana(int cost)
    {
        if (currentMana < cost)
        {
            Debug.Log($"{Name} not enough mana!");
            return false;
        }
        currentMana -= cost;
        return true;
    }

    public void GainXP(int amount)
    {
        currentXP += amount;
        Debug.Log($"{Name} gained {amount} XP! ({currentXP}/{xpToNextLevel})");

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

        Debug.Log($"🎉 {Name} reached Level {level}! ATK:{Attack} DEF:{Defense} SPD:{Speed}");
    }
}