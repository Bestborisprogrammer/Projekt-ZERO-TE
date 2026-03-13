using UnityEngine;
using System.Collections.Generic;
using System.Linq;

public class TurnCombatManager : MonoBehaviour
{
    public static TurnCombatManager Instance;

    [Header("UI")]
    public CombatUI combatUI;

    private List<Combatant> turnOrder = new();
    private int currentTurnIndex = 0;
    private bool combatActive = false;

    public int selectedEnemyIndex = 0;
    public List<Combatant> enemies = new();
    public List<Combatant> party = new();

    void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    void Start()
    {
        SetupCombat();
    }

    void SetupCombat()
    {
        turnOrder.Clear();
        enemies.Clear();
        party.Clear();

        foreach (var member in PartyManager.Instance.activeParty)
        {
            if (member.IsAlive)
            {
                var c = new Combatant(member);
                turnOrder.Add(c);
                party.Add(c);
            }
        }

        foreach (var enemyData in EncounterManager.CurrentEnemies)
        {
            var enemyInstance = new EnemyInstance { baseData = enemyData };
            enemyInstance.Initialize();
            var c = new Combatant(enemyInstance);
            turnOrder.Add(c);
            enemies.Add(c);
        }

        turnOrder = turnOrder.OrderByDescending(c => c.Speed).ToList();
        combatActive = true;
        selectedEnemyIndex = 0;

        Debug.Log("=== COMBAT STARTED ===");
        Debug.Log("Turn order: " + string.Join(" -> ", turnOrder.Select(c => c.Name)));

        combatUI.BuildEnemyTargetButtons(enemies);
        combatUI.UpdateAllHP(party, enemies);
        StartTurn();
    }

    void StartTurn()
    {
        if (!combatActive) return;

        Combatant current = turnOrder[currentTurnIndex];
        Debug.Log($"--- {current.Name}'s Turn --- HP:{current.CurrentHP}/{current.MaxHP} MP:{current.GetCurrentMana()}");

        combatUI.UpdateTurnText(current.Name);
        combatUI.UpdateAllHP(party, enemies);

        // Check freeze at start of turn
        if (current.IsFrozen)
        {
            Debug.Log($"{current.Name} is frozen and skips their turn!");
            current.ConsumeFreezeIfActive();
            combatUI.ShowCombatLog($"{current.Name} is frozen and cannot move!");
            NextTurn();
            return;
        }

        if (current.IsEnemy)
        {
            combatUI.SetPlayerButtonsActive(false);
            Invoke(nameof(EnemyTurn), 5f);
        }
        else
        {
            combatUI.SetPlayerButtonsActive(true);
            combatUI.ShowSpellButtons(
                current.GetPartySpells(current.GetCurrentLevel()),
                current.GetCurrentMana());
            combatUI.HighlightSelectedEnemy(selectedEnemyIndex);
        }
    }

    public void SelectEnemy(int index)
    {
        selectedEnemyIndex = index;
        combatUI.HighlightSelectedEnemy(selectedEnemyIndex);
        Debug.Log($"Selected enemy: {enemies[index].Name}");
    }

    public void PlayerBasicAttack()
    {
        Combatant attacker = turnOrder[currentTurnIndex];

        while (selectedEnemyIndex < enemies.Count && !enemies[selectedEnemyIndex].IsAlive)
            selectedEnemyIndex++;

        if (selectedEnemyIndex >= enemies.Count) return;

        Combatant target = enemies[selectedEnemyIndex];
        int damage = Mathf.Max(1, attacker.Attack - target.Defense);
        target.TakeDamage(damage);

        Debug.Log($"{attacker.Name} basic attacks {target.Name} for {damage} damage! (ATK:{attacker.Attack} - DEF:{target.Defense}) | {target.Name} HP: {target.CurrentHP}/{target.MaxHP}");

        combatUI.ShowCombatLog($"{attacker.Name} attacks {target.Name} for {damage} damage!");
        combatUI.UpdateAllHP(party, enemies);
        combatUI.BuildEnemyTargetButtons(enemies);
        combatUI.HighlightSelectedEnemy(selectedEnemyIndex);

        if (!target.IsAlive)
        {
            Debug.Log($"{target.Name} was defeated!");
            combatUI.ShowCombatLog($"{target.Name} was defeated!");
            if (enemies.All(e => !e.IsAlive)) { HandleVictory(); return; }
            selectedEnemyIndex = enemies.FindIndex(e => e.IsAlive);
            combatUI.BuildEnemyTargetButtons(enemies);
            combatUI.HighlightSelectedEnemy(selectedEnemyIndex);
        }

        NextTurn();
    }

    public void PlayerManaAttack(ManaAttackSO spell)
    {
        Combatant attacker = turnOrder[currentTurnIndex];

        if (!attacker.SpendMana(spell.manaCost))
        {
            Debug.Log($"{attacker.Name} tried to use {spell.spellName} but not enough mana! ({attacker.GetCurrentMana()}/{spell.manaCost})");
            combatUI.ShowCombatLog("Not enough mana!");
            return;
        }

        while (selectedEnemyIndex < enemies.Count && !enemies[selectedEnemyIndex].IsAlive)
            selectedEnemyIndex++;

        if (selectedEnemyIndex >= enemies.Count) return;

        Combatant target = enemies[selectedEnemyIndex];

        float affinityMult = attacker.Affinities.Contains(spell.affinity) && spell.affinity != SpellAffinity.None ? 1.5f : 1f;
        int damage = Mathf.Max(1, Mathf.RoundToInt((spell.flatDamage - target.Defense) * affinityMult));
        target.TakeDamage(damage);

        string affinityNote = affinityMult > 1f ? " (Affinity Bonus!)" : "";
        Debug.Log($"{attacker.Name} uses {spell.spellName} on {target.Name} for {damage} damage!{affinityNote} | {target.Name} HP: {target.CurrentHP}/{target.MaxHP}");

        combatUI.ShowCombatLog($"{attacker.Name} uses {spell.spellName} on {target.Name} for {damage} damage!{affinityNote}");

        if (spell.statusEffect != StatusEffectType.None)
        {
            target.ApplyStatusEffect(spell.statusEffect, spell.statusChance, spell.statusDuration, spell.dotPercent);
            bool wasApplied = target.HasStatusEffect(spell.statusEffect);
            Debug.Log($"Status effect {spell.statusEffect} on {target.Name}: applied={wasApplied} chance={spell.statusChance}");

            if (wasApplied)
                combatUI.ShowCombatLog($"{target.Name} is afflicted with {spell.statusEffect} for {spell.statusDuration} turns!");
            else
                combatUI.ShowCombatLog($"{spell.spellName} effect missed on {target.Name}!");
        }

        combatUI.UpdateAllHP(party, enemies);
        combatUI.BuildEnemyTargetButtons(enemies);
        combatUI.HighlightSelectedEnemy(selectedEnemyIndex);

        if (!target.IsAlive)
        {
            Debug.Log($"{target.Name} was defeated!");
            combatUI.ShowCombatLog($"{target.Name} was defeated!");
            if (enemies.All(e => !e.IsAlive)) { HandleVictory(); return; }
            selectedEnemyIndex = enemies.FindIndex(e => e.IsAlive);
            combatUI.BuildEnemyTargetButtons(enemies);
            combatUI.HighlightSelectedEnemy(selectedEnemyIndex);
        }

        NextTurn();
    }

    void EnemyTurn()
    {
        Combatant attacker = turnOrder[currentTurnIndex];
        if (!attacker.IsAlive) { NextTurn(); return; }

        List<Combatant> aliveParty = party.Where(p => p.IsAlive).ToList();
        if (aliveParty.Count == 0) return;

        Combatant target = aliveParty[Random.Range(0, aliveParty.Count)];

        var availableSpells = GetEnemyAvailableSpells();
        bool useSpell = availableSpells != null && availableSpells.Count > 0 && Random.value > 0.5f;

        if (useSpell)
        {
            var spell = availableSpells[Random.Range(0, availableSpells.Count)];
            attacker.SpendMana(spell.manaCost);

            float affinityMult = attacker.Affinities.Contains(spell.affinity) && spell.affinity != SpellAffinity.None ? 1.5f : 1f;
            int damage = Mathf.Max(1, Mathf.RoundToInt((spell.flatDamage - target.Defense) * affinityMult));
            target.TakeDamage(damage);

            Debug.Log($"{attacker.Name} uses {spell.spellName} on {target.Name} for {damage} damage! | {target.Name} HP: {target.CurrentHP}/{target.MaxHP}");
            combatUI.ShowCombatLog($"{attacker.Name} uses {spell.spellName} on {target.Name} for {damage} damage!");

            if (spell.statusEffect != StatusEffectType.None)
            {
                target.ApplyStatusEffect(spell.statusEffect, spell.statusChance, spell.statusDuration, spell.dotPercent);
                if (target.HasStatusEffect(spell.statusEffect))
                {
                    Debug.Log($"{target.Name} afflicted with {spell.statusEffect} for {spell.statusDuration} turns!");
                    combatUI.ShowCombatLog($"{target.Name} is afflicted with {spell.statusEffect} for {spell.statusDuration} turns!");
                }
            }
        }
        else
        {
            int damage = Mathf.Max(1, attacker.Attack - target.Defense);
            target.TakeDamage(damage);
            Debug.Log($"{attacker.Name} basic attacks {target.Name} for {damage} damage! | {target.Name} HP: {target.CurrentHP}/{target.MaxHP}");
            combatUI.ShowCombatLog($"{attacker.Name} attacks {target.Name} for {damage} damage!");
        }

        combatUI.UpdateAllHP(party, enemies);

        // Dot ticks AFTER enemy does their action
        Debug.Log($"Processing dots for {attacker.Name} after action...");
        var dotLogs = attacker.ProcessStatusEffects();
        foreach (var log in dotLogs)
        {
            Debug.Log($"[DOT] {log}");
            combatUI.ShowCombatLog(log);
        }

        combatUI.UpdateAllHP(party, enemies);
        combatUI.BuildEnemyTargetButtons(enemies);

        if (!attacker.IsAlive)
        {
            Debug.Log($"{attacker.Name} was defeated by status effect!");
            combatUI.ShowCombatLog($"{attacker.Name} was defeated by status effect!");
            if (enemies.All(e => !e.IsAlive)) { HandleVictory(); return; }
            selectedEnemyIndex = enemies.FindIndex(e => e.IsAlive);
            NextTurn();
            return;
        }

        if (PartyManager.Instance.IsGameOver())
        {
            Debug.Log("GAME OVER - all party members defeated!");
            combatUI.ShowGameOver();
            combatActive = false;
            return;
        }

        NextTurn();
    }

    List<EnemyManaAttackSO> GetEnemyAvailableSpells()
    {
        Combatant current = turnOrder[currentTurnIndex];
        if (!current.IsEnemy) return null;

        int enemyListIndex = enemies.IndexOf(current);
        if (enemyListIndex < 0 || enemyListIndex >= EncounterManager.CurrentEnemies.Count) return null;

        var enemyData = EncounterManager.CurrentEnemies[enemyListIndex];
        return enemyData.spells.FindAll(s => current.GetCurrentMana() >= s.manaCost);
    }

    void NextTurn()
    {
        if (!combatActive) return;

        int attempts = 0;
        do
        {
            currentTurnIndex = (currentTurnIndex + 1) % turnOrder.Count;
            attempts++;
        }
        while (!turnOrder[currentTurnIndex].IsAlive && attempts < turnOrder.Count);

        StartTurn();
    }

    void HandleVictory()
    {
        Debug.Log($"=== VICTORY === Total XP: {enemies.Sum(e => e.XPReward)}");
        int totalXP = enemies.Sum(e => e.XPReward);
        PartyManager.Instance.GiveXPToAll(totalXP);
        combatUI.ShowVictory(totalXP);
        combatActive = false;
    }
}