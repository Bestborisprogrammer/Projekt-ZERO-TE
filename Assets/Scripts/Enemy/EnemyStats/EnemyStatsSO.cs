using UnityEngine;
using System.Collections.Generic;

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

    [Header("Affinities")]
    public List<SpellAffinity> affinities = new();

    [Header("Spells")]
    public List<EnemyManaAttackSO> spells = new();
}