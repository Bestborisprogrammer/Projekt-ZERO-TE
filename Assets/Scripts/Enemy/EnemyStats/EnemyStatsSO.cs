using UnityEngine;

[CreateAssetMenu(fileName = "NewEnemy", menuName = "Zero-Te/Enemy")]
public class EnemyStatsSO : ScriptableObject
{
    [Header("Identity")]
    public string enemyName = "Slime";
    public Sprite sprite;

    [Header("Stats")]
    public int maxHP = 40;
    public int attack = 6;
    public int defense = 2;
    public int speed = 4;
    public int maxMana = 10;

    [Header("Rewards")]
    public int xpReward = 30;
}