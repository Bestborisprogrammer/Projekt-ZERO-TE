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
    public string enemyName = "Enemy";
    public Sprite sprite;

    [Header("Stats")]
    public int level = 1;
    public int maxHP = 50;
    public int attack = 8;
    public int defense = 3;
    public int speed = 5;
    public int maxMana = 20;

    [Header("Crit")]
    public float critRate = 0.05f;
    public float critDamage = 1.5f;

    [Header("Affinities")]
    public List<SpellAffinity> affinities = new();

    [Header("Combat Style")]
    public CombatStyle combatStyle = CombatStyle.Block;

    [Header("Spells")]
    public List<EnemyManaAttackSO> spells = new();

    [Header("Rewards")]
    public int xpReward = 30;
    public int goldReward = 10;

    [Header("Drops")]
    public List<ItemDrop> itemDrops = new();
    public List<GearDrop> gearDrops = new();
}