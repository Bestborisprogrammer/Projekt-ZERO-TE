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
        combatUI.SetupCombatSprites(party, enemies);
        CombatSpriteManager.Instance?.UpdateEnemyLabels(enemies);
        StartTurn();
    }

    void StartTurn()
    {
        if (!combatActive) return;

        Combatant current = turnOrder[currentTurnIndex];
        current.SetBlocking(false);
        current.SetEvading(false);

        Debug.Log($"--- {current.Name}'s Turn --- HP:{current.CurrentHP}/{current.MaxHP} MP:{current.GetCurrentMana()}");

        combatUI.UpdateTurnText(current.Name);
        combatUI.UpdateAllHP(party, enemies);
        CombatSpriteManager.Instance?.UpdateEnemyLabels(enemies);

        if (current.IsFrozen)
        {
            current.ConsumeFreezeIfActive();
            Debug.Log($"{current.Name} is frozen!");
            combatUI.ShowCombatLog($"{current.Name} is frozen and cannot move!", () => NextTurn());
            return;
        }

        if (current.IsParalyzed)
        {
            bool skips = Random.value < 0.5f;
            Debug.Log($"{current.Name} is paralyzed - skips: {skips}");
            if (skips)
            {
                combatUI.ShowCombatLog($"{current.Name} is paralyzed and cannot move!", () => NextTurn());
                return;
            }
            else
                combatUI.ShowCombatLog($"{current.Name} breaks through the paralysis!");
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
    }

    string HandleElementalCombos(Combatant attacker, Combatant target, SpellAffinity affinity, ref float damageMult)
    {
        string comboMsg = "";
        if (affinity == SpellAffinity.Thunder && target.IsWet)
        {
            damageMult = 2.5f;
            target.RemoveStatusEffect(StatusEffectType.Wet);
            comboMsg = "THUNDERSTRUCK! Wet target takes 2.5x damage!";
        }
        else if (affinity == SpellAffinity.Fire && target.IsFrozen)
        {
            damageMult *= 1.5f;
            target.RemoveStatusEffect(StatusEffectType.Freeze);
            comboMsg = "Fire melts the ice! Bonus fire damage!";
        }
        else if (affinity == SpellAffinity.Water && target.IsBurning)
        {
            damageMult *= 1.5f;
            target.RemoveStatusEffect(StatusEffectType.Burn);
            comboMsg = "Water extinguishes the flames! Bonus water damage!";
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
                combatUI.ShowCombatLog($"{attacker.Name} absorbs {healAmount} HP from the light!");
            }
        }

        if (spell.statusEffect != StatusEffectType.None)
        {
            int speedReduction = spell.affinity == SpellAffinity.Water ? 3 : 0;
            target.ApplyStatusEffect(spell.statusEffect, spell.statusChance, spell.statusDuration,
                spell.dotPercent, spell.defenseReduction, speedReduction);
            bool wasApplied = target.HasStatusEffect(spell.statusEffect);

            if (wasApplied)
            {
                if (spell.statusEffect == StatusEffectType.Wet)
                    combatUI.ShowCombatLog($"{target.Name} is Wet! Speed reduced by 3 for {spell.statusDuration} turns!");
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
            target.ApplyStatusEffect(spell.statusEffect, spell.statusChance, spell.statusDuration,
                spell.dotPercent, spell.defenseReduction, speedReduction);
            if (target.HasStatusEffect(spell.statusEffect))
                combatUI.ShowCombatLog($"{target.Name} is afflicted with {spell.statusEffect} for {spell.statusDuration} turns!");
        }
    }

    bool ResolveAttack(Combatant attacker, Combatant target, int damage, string attackName)
    {
        if (target.CombatStyle == CombatStyle.Evade && target.TryEvade())
        {
            combatUI.ShowCombatLog($"{target.Name} evaded the attack! (E! - Dodge: {target.EvadeChance * 100f:F1}%)");
            return false;
        }

        if (target.CombatStyle == CombatStyle.Block && target.IsBlocking)
        {
            int reduced = Mathf.RoundToInt(damage * (1f - target.BlockReduction));
            target.TakeDamage(reduced);
            CombatSpriteManager.Instance?.PlayHitEffect(target.Name);
            combatUI.ShowCombatLog($"{attacker.Name} hits {target.Name} for {damage} damage! (B! reduced to {reduced} - {target.BlockReduction * 100f:F1}% reduction)");
        }
        else
        {
            target.TakeDamage(damage);
            CombatSpriteManager.Instance?.PlayHitEffect(target.Name);
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
        CombatSpriteManager.Instance?.UpdateEnemyLabels(enemies);

        if (hit && !target.IsAlive)
        {
            CombatSpriteManager.Instance?.PlayDefeatedEffect(target.Name);
            if (enemies.All(e => !e.IsAlive))
            {
                combatUI.ShowCombatLog($"{target.Name} was defeated!", () => HandleVictory());
                return;
            }
            combatUI.ShowCombatLog($"{target.Name} was defeated!");
            selectedEnemyIndex = enemies.FindIndex(e => e.IsAlive);
            combatUI.BuildEnemyTargetButtons(enemies);
            combatUI.HighlightSelectedEnemy(selectedEnemyIndex);
        }

        combatUI.ShowCombatLog("", () => NextTurn());
    }

    public void PlayerBlock()
    {
        Combatant blocker = turnOrder[currentTurnIndex];
        blocker.SetBlocking(true);
        combatUI.UpdateAllHP(party, enemies);
        CombatSpriteManager.Instance?.UpdateEnemyLabels(enemies);
        combatUI.ShowCombatLog($"{blocker.Name} guards! Damage reduction: {blocker.BlockReduction * 100f:F1}%",
            () => NextTurn());
    }

    public void PlayerEvade()
    {
        Combatant evader = turnOrder[currentTurnIndex];
        evader.SetEvading(true);
        combatUI.UpdateAllHP(party, enemies);
        CombatSpriteManager.Instance?.UpdateEnemyLabels(enemies);
        combatUI.ShowCombatLog($"{evader.Name} readies an evade! Dodge chance: {evader.EvadeChance * 100f:F1}%",
            () => NextTurn());
    }

    public void PlayerManaAttack(ManaAttackSO spell)
    {
        Combatant attacker = turnOrder[currentTurnIndex];

        if (!attacker.SpendMana(spell.manaCost))
        {
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
        CombatSpriteManager.Instance?.UpdateEnemyLabels(enemies);

        if (hit && !target.IsAlive)
        {
            CombatSpriteManager.Instance?.PlayDefeatedEffect(target.Name);
            if (enemies.All(e => !e.IsAlive))
            {
                combatUI.ShowCombatLog($"{target.Name} was defeated!", () => HandleVictory());
                return;
            }
            combatUI.ShowCombatLog($"{target.Name} was defeated!");
            selectedEnemyIndex = enemies.FindIndex(e => e.IsAlive);
            combatUI.BuildEnemyTargetButtons(enemies);
            combatUI.HighlightSelectedEnemy(selectedEnemyIndex);
        }

        combatUI.ShowCombatLog("", () => NextTurn());
    }

    void EnemyTurn()
    {
        Combatant attacker = turnOrder[currentTurnIndex];
        if (!attacker.IsAlive) { NextTurn(); return; }

        List<Combatant> aliveParty = party.Where(p => p.IsAlive).ToList();
        if (aliveParty.Count == 0) return;

        if (Random.value < 0.2f)
        {
            if (attacker.CombatStyle == CombatStyle.Block)
            {
                attacker.SetBlocking(true);
                combatUI.ShowCombatLog($"{attacker.Name} guards! (B! - {attacker.BlockReduction * 100f:F1}% reduction)",
                    () => ProcessDotsAndNextTurn(attacker));
            }
            else
            {
                attacker.SetEvading(true);
                combatUI.ShowCombatLog($"{attacker.Name} readies an evade! Dodge: {attacker.EvadeChance * 100f:F1}%",
                    () => ProcessDotsAndNextTurn(attacker));
            }
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
        CombatSpriteManager.Instance?.UpdateEnemyLabels(enemies);

        combatUI.ShowCombatLog("", () => ProcessDotsAndNextTurn(attacker));
    }

    void ProcessDotsAndNextTurn(Combatant attacker)
    {
        var dotLogs = attacker.ProcessStatusEffects();

        if (dotLogs.Count > 0)
        {
            combatUI.ShowCombatLogs(dotLogs, () =>
            {
                combatUI.UpdateAllHP(party, enemies);
                combatUI.BuildEnemyTargetButtons(enemies);
                CombatSpriteManager.Instance?.UpdateEnemyLabels(enemies);

                if (!attacker.IsAlive)
                {
                    CombatSpriteManager.Instance?.PlayDefeatedEffect(attacker.Name);
                    if (enemies.All(e => !e.IsAlive)) { HandleVictory(); return; }
                    selectedEnemyIndex = enemies.FindIndex(e => e.IsAlive);
                }

                if (PartyManager.Instance.IsGameOver())
                {
                    combatUI.ShowGameOver();
                    combatActive = false;
                    return;
                }

                NextTurn();
            });
        }
        else
        {
            combatUI.UpdateAllHP(party, enemies);
            combatUI.BuildEnemyTargetButtons(enemies);
            CombatSpriteManager.Instance?.UpdateEnemyLabels(enemies);

            if (PartyManager.Instance.IsGameOver())
            {
                combatUI.ShowGameOver();
                combatActive = false;
                return;
            }

            NextTurn();
        }
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

    public void NextTurnPublic() => NextTurn();

    void HandleVictory()
    {
        int totalXP = enemies.Sum(e => e.XPReward);
        int totalGold = 0;
        DropResult drops = new DropResult();

        foreach (var enemy in enemies)
        {
            int idx = enemies.IndexOf(enemy);
            if (idx < 0 || idx >= EncounterManager.CurrentEnemies.Count) continue;
            var enemyData = EncounterManager.CurrentEnemies[idx];
            totalGold += enemyData.goldReward;

            foreach (var drop in enemyData.itemDrops)
            {
                float roll = Random.Range(0f, 100f);
                if (roll <= drop.dropChance)
                {
                    InventoryManager.Instance.AddItem(drop.item);
                    drops.itemsDropped.Add(drop.item);
                }
            }

            foreach (var drop in enemyData.gearDrops)
            {
                float roll = Random.Range(0f, 100f);
                if (roll <= drop.dropChance)
                {
                    GearManager.Instance.AddGearToInventory(drop.gear);
                    drops.gearDropped.Add(drop.gear);
                }
            }
        }

        GoldManager.Instance.AddGold(totalGold);
        PartyManager.Instance.GiveXPToAll(totalXP);
        combatUI.ShowVictory(totalXP, totalGold, drops);
        combatActive = false;
    }
}