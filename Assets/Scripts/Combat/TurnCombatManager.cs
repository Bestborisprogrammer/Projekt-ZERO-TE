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
        current.SetBlocking(false);

        Debug.Log($"--- {current.Name}'s Turn --- HP:{current.CurrentHP}/{current.MaxHP} MP:{current.GetCurrentMana()} Style:{current.CombatStyle}");

        combatUI.UpdateTurnText(current.Name);
        combatUI.UpdateAllHP(party, enemies);

        if (current.IsFrozen)
        {
            current.ConsumeFreezeIfActive();
            Debug.Log($"{current.Name} is frozen and skips their turn!");
            combatUI.ShowCombatLog($"{current.Name} is frozen and cannot move!");
            NextTurn();
            return;
        }

        if (current.IsParalyzed)
        {
            bool skips = Random.value < 0.5f;
            Debug.Log($"{current.Name} is paralyzed - skips: {skips}");
            if (skips)
            {
                combatUI.ShowCombatLog($"{current.Name} is paralyzed and cannot move!");
                NextTurn();
                return;
            }
            else
            {
                combatUI.ShowCombatLog($"{current.Name} breaks through the paralysis!");
            }
        }

        if (current.IsEnemy)
        {
            combatUI.SetPlayerButtonsActive(false);
            Invoke(nameof(EnemyTurn), 3f);
        }
        else
        {
            combatUI.SetPlayerButtonsActive(true, current.CombatStyle);
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

    string HandleElementalCombos(Combatant attacker, Combatant target, SpellAffinity affinity, ref float damageMult)
    {
        string comboMsg = "";

        if (affinity == SpellAffinity.Thunder && target.IsWet)
        {
            damageMult = 2.5f;
            target.RemoveStatusEffect(StatusEffectType.Wet);
            comboMsg = "THUNDERSTRUCK! Wet target takes 2.5x damage!";
            Debug.Log($"[COMBO] Thunder + Wet on {target.Name}!");
        }
        else if (affinity == SpellAffinity.Fire && target.IsFrozen)
        {
            damageMult *= 1.5f;
            target.RemoveStatusEffect(StatusEffectType.Freeze);
            comboMsg = "Fire melts the ice! Bonus fire damage!";
            Debug.Log($"[COMBO] Fire + Freeze on {target.Name}!");
        }
        else if (affinity == SpellAffinity.Water && target.IsBurning)
        {
            damageMult *= 1.5f;
            target.RemoveStatusEffect(StatusEffectType.Burn);
            comboMsg = "Water extinguishes the flames! Bonus water damage!";
            Debug.Log($"[COMBO] Water + Burn on {target.Name}!");
        }

        return comboMsg;
    }

    void ApplySpellEffects(ManaAttackSO spell, Combatant attacker, Combatant target, int damage)
    {
        if (spell.affinity == SpellAffinity.Light)
        {
            int healAmount = Mathf.RoundToInt(damage * 0.3f);
            var charRef = PartyManager.Instance.activeParty.Find(m => m.Name == attacker.Name);
            if (charRef != null)
            {
                charRef.currentHP = Mathf.Min(charRef.MaxHP, charRef.currentHP + healAmount);
                Debug.Log($"[LIGHT] {attacker.Name} healed for {healAmount}!");
                combatUI.ShowCombatLog($"{attacker.Name} absorbs {healAmount} HP from the light!");
            }
        }

        if (spell.statusEffect != StatusEffectType.None)
        {
            int speedReduction = spell.affinity == SpellAffinity.Water ? 3 : 0;
            target.ApplyStatusEffect(spell.statusEffect, spell.statusChance, spell.statusDuration, spell.dotPercent, spell.defenseReduction, speedReduction);
            bool wasApplied = target.HasStatusEffect(spell.statusEffect);
            Debug.Log($"Status effect {spell.statusEffect} on {target.Name}: applied={wasApplied}");

            if (wasApplied)
            {
                if (spell.statusEffect == StatusEffectType.Wet)
                {
                    combatUI.ShowCombatLog($"{target.Name} is Wet! Speed reduced by 3 for {spell.statusDuration} turns!");
                    Debug.Log($"[WET] {target.Name} Speed reduced by 3 for {spell.statusDuration} turns!");
                }
                else
                    combatUI.ShowCombatLog($"{target.Name} is afflicted with {spell.statusEffect} for {spell.statusDuration} turns!");
            }
            else
                combatUI.ShowCombatLog($"{spell.spellName} effect missed on {target.Name}!");
        }
    }

    void ApplyEnemySpellEffects(EnemyManaAttackSO spell, Combatant attacker, Combatant target, int damage)
    {
        if (spell.statusEffect != StatusEffectType.None)
        {
            int speedReduction = spell.affinity == SpellAffinity.Water ? 3 : 0;
            target.ApplyStatusEffect(spell.statusEffect, spell.statusChance, spell.statusDuration, spell.dotPercent, spell.defenseReduction, speedReduction);
            if (target.HasStatusEffect(spell.statusEffect))
                combatUI.ShowCombatLog($"{target.Name} is afflicted with {spell.statusEffect} for {spell.statusDuration} turns!");
        }
    }

    // Shared attack resolution – handles evade and block for any target
    bool ResolveAttack(Combatant attacker, Combatant target, int damage, string attackName)
    {
        if (target.CombatStyle == CombatStyle.Evade && target.TryEvade())
        {
            Debug.Log($"{target.Name} evaded {attackName}! (Evade chance: {target.EvadeChance * 100f:F1}%)");
            combatUI.ShowCombatLog($"{target.Name} evaded the attack! (E! - Dodge chance: {target.EvadeChance * 100f:F1}%)");
            return false;
        }

        if (target.CombatStyle == CombatStyle.Block && target.IsBlocking)
        {
            int originalDamage = damage;
            int reducedDamage = Mathf.RoundToInt(damage * (1f - target.BlockReduction));
            target.TakeDamage(reducedDamage);
            Debug.Log($"{target.Name} blocked! {originalDamage} -> {reducedDamage} damage (B! - {target.BlockReduction * 100f:F1}% reduction)");
            combatUI.ShowCombatLog($"{attacker.Name} hits {target.Name} for {originalDamage} damage! (B! reduced to {reducedDamage} - {target.BlockReduction * 100f:F1}% reduction)");
        }
        else
        {
            target.TakeDamage(damage);
            Debug.Log($"{attacker.Name} hits {target.Name} for {damage} damage! | {target.Name} HP: {target.CurrentHP}/{target.MaxHP}");
            combatUI.ShowCombatLog($"{attacker.Name} hits {target.Name} for {damage} damage!");
        }

        return true;
    }

    public void PlayerBasicAttack()
    {
        Combatant attacker = turnOrder[currentTurnIndex];

        while (selectedEnemyIndex < enemies.Count && !enemies[selectedEnemyIndex].IsAlive)
            selectedEnemyIndex++;

        if (selectedEnemyIndex >= enemies.Count) return;

        Combatant target = enemies[selectedEnemyIndex];
        int damage = Mathf.Max(1, attacker.Attack - target.Defense);

        bool hit = ResolveAttack(attacker, target, damage, "basic attack");

        combatUI.UpdateAllHP(party, enemies);
        combatUI.BuildEnemyTargetButtons(enemies);
        combatUI.HighlightSelectedEnemy(selectedEnemyIndex);

        if (hit && !target.IsAlive)
        {
            combatUI.ShowCombatLog($"{target.Name} was defeated!");
            if (enemies.All(e => !e.IsAlive)) { HandleVictory(); return; }
            selectedEnemyIndex = enemies.FindIndex(e => e.IsAlive);
            combatUI.BuildEnemyTargetButtons(enemies);
            combatUI.HighlightSelectedEnemy(selectedEnemyIndex);
        }

        NextTurn();
    }

    public void PlayerBlock()
    {
        Combatant blocker = turnOrder[currentTurnIndex];
        blocker.SetBlocking(true);
        Debug.Log($"{blocker.Name} is guarding! Block reduction: {blocker.BlockReduction * 100f:F1}%");
        combatUI.ShowCombatLog($"{blocker.Name} guards! Damage reduction: {blocker.BlockReduction * 100f:F1}%");
        combatUI.UpdateAllHP(party, enemies);
        NextTurn();
    }

    public void PlayerEvade()
    {
        Combatant evader = turnOrder[currentTurnIndex];
        Debug.Log($"{evader.Name} is ready to evade! Dodge chance: {evader.EvadeChance * 100f:F1}%");
        combatUI.ShowCombatLog($"{evader.Name} readies an evade! Dodge chance: {evader.EvadeChance * 100f:F1}%");
        combatUI.UpdateAllHP(party, enemies);
        NextTurn();
    }

    public void PlayerManaAttack(ManaAttackSO spell)
    {
        Combatant attacker = turnOrder[currentTurnIndex];

        if (!attacker.SpendMana(spell.manaCost))
        {
            Debug.Log($"{attacker.Name} not enough mana for {spell.spellName}!");
            combatUI.ShowCombatLog("Not enough mana!");
            return;
        }

        while (selectedEnemyIndex < enemies.Count && !enemies[selectedEnemyIndex].IsAlive)
            selectedEnemyIndex++;

        if (selectedEnemyIndex >= enemies.Count) return;

        Combatant target = enemies[selectedEnemyIndex];

        float affinityMult = attacker.Affinities.Contains(spell.affinity) && spell.affinity != SpellAffinity.None ? 1.5f : 1f;
        string comboMsg = HandleElementalCombos(attacker, target, spell.affinity, ref affinityMult);

        int damage = Mathf.Max(1, Mathf.RoundToInt((spell.flatDamage - target.Defense) * affinityMult));
        string affinityNote = affinityMult > 1f ? $" (x{affinityMult:F1}!)" : "";

        combatUI.ShowCombatLog($"{attacker.Name} uses {spell.spellName}!{affinityNote}");
        if (!string.IsNullOrEmpty(comboMsg)) combatUI.ShowCombatLog(comboMsg);

        bool hit = ResolveAttack(attacker, target, damage, spell.spellName);

        if (hit) ApplySpellEffects(spell, attacker, target, damage);

        combatUI.UpdateAllHP(party, enemies);
        combatUI.BuildEnemyTargetButtons(enemies);
        combatUI.HighlightSelectedEnemy(selectedEnemyIndex);

        if (hit && !target.IsAlive)
        {
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

        // 20% chance enemy blocks or evades depending on their style
        if (Random.value < 0.2f)
        {
            if (attacker.CombatStyle == CombatStyle.Block)
            {
                attacker.SetBlocking(true);
                Debug.Log($"{attacker.Name} is guarding! Block reduction: {attacker.BlockReduction * 100f:F1}%");
                combatUI.ShowCombatLog($"{attacker.Name} guards! (B! - {attacker.BlockReduction * 100f:F1}% reduction)");
            }
            else
            {
                Debug.Log($"{attacker.Name} is ready to evade! Dodge chance: {attacker.EvadeChance * 100f:F1}%");
                combatUI.ShowCombatLog($"{attacker.Name} readies an evade! Dodge chance: {attacker.EvadeChance * 100f:F1}%");
            }

            var defenseDotLogs = attacker.ProcessStatusEffects();
            foreach (var log in defenseDotLogs)
            {
                Debug.Log($"[DOT] {log}");
                combatUI.ShowCombatLog(log);
            }

            combatUI.UpdateAllHP(party, enemies);
            NextTurn();
            return;
        }

        Combatant target = aliveParty[Random.Range(0, aliveParty.Count)];

        var availableSpells = GetEnemyAvailableSpells();
        bool useSpell = availableSpells != null && availableSpells.Count > 0 && Random.value > 0.5f;

        if (useSpell)
        {
            var spell = availableSpells[Random.Range(0, availableSpells.Count)];
            attacker.SpendMana(spell.manaCost);

            float affinityMult = attacker.Affinities.Contains(spell.affinity) && spell.affinity != SpellAffinity.None ? 1.5f : 1f;
            string comboMsg = HandleElementalCombos(attacker, target, spell.affinity, ref affinityMult);

            int damage = Mathf.Max(1, Mathf.RoundToInt((spell.flatDamage - target.Defense) * affinityMult));

            combatUI.ShowCombatLog($"{attacker.Name} uses {spell.spellName}!");
            if (!string.IsNullOrEmpty(comboMsg)) combatUI.ShowCombatLog(comboMsg);

            bool hit = ResolveAttack(attacker, target, damage, spell.spellName);
            if (hit) ApplyEnemySpellEffects(spell, attacker, target, damage);
        }
        else
        {
            int damage = Mathf.Max(1, attacker.Attack - target.Defense);
            ResolveAttack(attacker, target, damage, "basic attack");
        }

        combatUI.UpdateAllHP(party, enemies);

        Debug.Log($"[DOT CHECK] Processing dots for {attacker.Name}...");
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
            Debug.Log("GAME OVER!");
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