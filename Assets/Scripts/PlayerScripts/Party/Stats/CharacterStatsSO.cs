using UnityEngine;
using System.Collections.Generic;

public enum CombatStyle { Block, Evade }

[CreateAssetMenu(fileName = "NewCharacter", menuName = "Zero-Te/Character")]
public class CharacterStatsSO : ScriptableObject
{
    [Header("Identity")]
    public string characterName = "Hero";
    public Sprite portrait;

    [Header("Starting Level")]
    public int startingLevel = 1;

    [Header("Base Stats")]
    public int maxHP = 100;
    public int attack = 10;
    public int defense = 5;
    public int speed = 8;
    public int maxMana = 30;

    [Header("Stat Growth per Level")]
    public int hpGrowth = 15;
    public int attackGrowth = 3;
    public int defenseGrowth = 2;
    public int speedGrowth = 1;
    public int manaGrowth = 5;

    [Header("XP")]
    public int baseXPToNextLevel = 100;

    [Header("Affinities")]
    public List<SpellAffinity> affinities = new();

    [Header("Spells")]
    public List<ManaAttackSO> spells = new();

    [Header("Combat Style")]
    public CombatStyle combatStyle = CombatStyle.Block;
}