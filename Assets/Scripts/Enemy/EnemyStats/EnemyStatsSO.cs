using UnityEngine;
using System.Collections.Generic;

[System.Serializable]
public class ItemDrop
{
    public ItemSO item;
    [Range(0f, 100f)] public float dropChance = 50f;
}

[System.Serializable]
public class GearDrop
{
    public GearSO gear;
    [Range(0f, 100f)] public float dropChance = 10f;
}

[CreateAssetMenu(fileName = "NewEnemy", menuName = "Zero-Te/Enemy")]
public class EnemyStatsSO : ScriptableObject
{
    [Header("Identity")]
    public string enemyName = "Slime";
    public Sprite sprite;

    [Header("Level")]
    public int level = 1;

    [Header("Stats")]
    public int maxHP = 40;
    public int attack = 6;
    public int defense = 2;
    public int speed = 4;
    public int maxMana = 10;

    [Header("Rewards")]
    public int xpReward = 30;
    public int goldReward = 10;

    [Header("Drops")]
    public List<ItemDrop> itemDrops = new();
    public List<GearDrop> gearDrops = new();

    [Header("Affinities")]
    public List<SpellAffinity> affinities = new();

    [Header("Spells")]
    public List<EnemyManaAttackSO> spells = new();

    [Header("Combat Style")]
    public CombatStyle combatStyle = CombatStyle.Block;
}