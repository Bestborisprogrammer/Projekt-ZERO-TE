using UnityEngine;
using System.Collections.Generic;
using System.Linq;

public class TurnCombatManager : MonoBehaviour
{
    public static TurnCombatManager Instance;

    [Header("UI")]
    public CombatUI combatUI;

    public List<Combatant> turnOrder = new();
    private int currentTurnIndex = 0;
    private bool combatActive = false;

    public int selectedEnemyIndex = 0;
    public List<Combatant> enemies = new();
    public List<Combatant> party = new();
    private Dictionary<string, EnemyInstance> enemyInstanceDict = new();

    public int CurrentTurnIndex => currentTurnIndex;

    public EnemyInstance GetEnemyInstance(string name) =>
        enemyInstanceDict.ContainsKey(name) ? enemyInstanceDict[name] : null;

    void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    void Start() => SetupCombat();

    void SetupCombat()
    {
        turnOrder.Clear();
        enemies.Clear();
        party.Clear();
        enemyInstanceDict.Clear();

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
            enemyInstanceDict[enemyData.enemyName] = enemyInstance;
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
        UpdateStatusIndicators();

        // Only play recruit dialogue if this IS the recruit battle
        if (EncounterManager.ActiveRecruitCutscene != null && combatUI.recruitBattleDialogue != null)
        {
            combatUI.SetPlayerButtonsActive(false);
            combatUI.PlayRecruitBattleDialogue(() => StartTurn());
        }
        else
        {
            StartTurn();
        }
    }

    public void UpdateStatusIndicatorsPublic() => UpdateStatusIndicators();

    void UpdateStatusIndicators()
    {
        var all = new List<Combatant>();
        all.AddRange(party);
        all.AddRange(enemies);
        CombatSpriteManager.Instance?.UpdateStatusIndicators(all);
    }

    void StartTurn()
    {
        if (!combatActive) return;

        Combatant current = turnOrder[currentTurnIndex];
        current.SetBlocking(false);
        current.SetEvading(false);

        combatUI.UpdateTurnText(current.Name);
        combatUI.UpdateAllHP(party, enemies);
        CombatSpriteManager.Instance?.UpdateEnemyLabels(enemies);

        List<string> modLogs = TickCombatantModifiers(current);

        if (modLogs.Count > 0)
        {
            combatUI.ShowCombatLogs(modLogs, () =>
            {
                combatUI.ClearCombatLog();
                UpdateStatusIndicators();
                SetupTurnUI(current);
                ContinueStartTurn(current);
            });
            return;
        }

        UpdateStatusIndicators();
        SetupTurnUI(current);
        ContinueStartTurn(current);
    }

    void SetupTurnUI(Combatant current)
    {
        if (!current.IsEnemy)
        {
            // Reset and re-setup to ensure block/evade sprite is correct
            combatUI.SetPlayerButtonsActive(false);
            combatUI.SetPlayerButtonsActive(true, current.CombatStyle);
        }
    }

    List<string> TickCombatantModifiers(Combatant combatant)
    {
        if (combatant.IsEnemy)
        {
            var inst = GetEnemyInstance(combatant.Name);
            return inst?.TickStatModifiers() ?? new List<string>();
        }
        var member = PartyManager.Instance.activeParty.Find(m => m.Name == combatant.Name);
        return member?.TickStatModifiers() ?? new List<string>();
    }

    void ContinueStartTurn(Combatant current)
    {
        if (!combatActive) return;

        if (current.IsFrozen)
        {
            current.ConsumeFreezeIfActive();
            combatUI.ShowCombatLog($"{current.Name} is frozen and cannot move!", () => NextTurn());
            return;
        }

        if (current.IsParalyzed)
        {
            bool skips = Random.value < 0.5f;
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

    string HandleElementalCombos(Combatant attacker, Combatant target,
        SpellAffinity affinity, ref float damageMult)
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
            comboMsg = "Fire melts the ice! Target thawed! Bonus damage!";
        }
        else if (affinity == SpellAffinity.Water && target.IsBurning)
        {
            damageMult *= 1.5f;
            target.RemoveStatusEffect(StatusEffectType.Burn);
            comboMsg = "Water extinguishes the flames! Bonus damage!";
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
                attacker.Refresh();
                CombatSpriteManager.Instance?.ShowDamageNumber(attacker.Name, healAmount, true);
                combatUI.ShowCombatLog($"{attacker.Name} absorbs {healAmount} HP from the light!");
                combatUI.UpdateAllHP(party, enemies);
            }
        }

        bool wasJustThawed = spell.affinity == SpellAffinity.Fire && !target.IsFrozen;
        if (!wasJustThawed && spell.statusEffect != StatusEffectType.None)
        {
            int speedReduction = spell.affinity == SpellAffinity.Water ? 3 : 0;
            target.ApplyStatusEffect(spell.statusEffect, spell.statusChance, spell.statusDuration,
                spell.dotPercent, spell.defenseReduction, speedReduction);
            bool wasApplied = target.HasStatusEffect(spell.statusEffect);

            if (wasApplied)
            {
                if (spell.statusEffect == StatusEffectType.Wet)
                    combatUI.ShowCombatLog($"{target.Name} is Wet! Speed reduced for {spell.statusDuration} turns!");
                else
                    combatUI.ShowCombatLog($"{target.Name} is afflicted with {spell.statusEffect} for {spell.statusDuration} turns!");
            }
            else
                combatUI.ShowCombatLog($"{spell.spellName} effect missed on {target.Name}!");
        }

        UpdateStatusIndicators();
    }

    void ApplyEnemySpellEffects(EnemyManaAttackSO spell, Combatant attacker,
        Combatant target, int damage)
    {
        if (spell.statusEffect != StatusEffectType.None)
        {
            int speedReduction = spell.affinity == SpellAffinity.Water ? 3 : 0;
            target.ApplyStatusEffect(spell.statusEffect, spell.statusChance, spell.statusDuration,
                spell.dotPercent, spell.defenseReduction, speedReduction);
            if (target.HasStatusEffect(spell.statusEffect))
                combatUI.ShowCombatLog($"{target.Name} is afflicted with {spell.statusEffect} for {spell.statusDuration} turns!");
        }
        UpdateStatusIndicators();
    }

    bool ResolveAttack(Combatant attacker, Combatant target,
        int damage, string attackName, bool canCrit = false)
    {
        if (target.CombatStyle == CombatStyle.Evade && target.TryEvade())
        {
            combatUI.ShowCombatLog($"{target.Name} evaded the attack!");
            return false;
        }

        bool isCrit = canCrit && Random.value < attacker.CritRate;
        if (isCrit)
            damage = Mathf.RoundToInt(damage * attacker.CritDamage);

        string critTag = isCrit ? " CRITICAL HIT!" : "";

        if (target.CombatStyle == CombatStyle.Block && target.IsBlocking)
        {
            int reduced = Mathf.RoundToInt(damage * (1f - target.BlockReduction));
            target.TakeDamage(reduced);
            CombatSpriteManager.Instance?.PlayHitEffect(target.Name, reduced, isCrit);
            combatUI.ShowCombatLog($"{attacker.Name} hits {target.Name} for {damage} damage!{critTag} " +
                $"(B! reduced to {reduced})");
        }
        else
        {
            target.TakeDamage(damage);
            CombatSpriteManager.Instance?.PlayHitEffect(target.Name, damage, isCrit);
            combatUI.ShowCombatLog($"{attacker.Name} hits {target.Name} for {damage} damage!{critTag}");
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
        bool hit = ResolveAttack(attacker, target, damage, "basic attack", true);

        combatUI.UpdateAllHP(party, enemies);
        combatUI.BuildEnemyTargetButtons(enemies);
        combatUI.HighlightSelectedEnemy(selectedEnemyIndex);
        CombatSpriteManager.Instance?.UpdateEnemyLabels(enemies);
        UpdateStatusIndicators();

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

        combatUI.ShowCombatLog(" ", () => NextTurn());
    }

    public void PlayerBlock()
    {
        Combatant blocker = turnOrder[currentTurnIndex];
        blocker.SetBlocking(true);
        combatUI.UpdateAllHP(party, enemies);
        CombatSpriteManager.Instance?.UpdateEnemyLabels(enemies);
        UpdateStatusIndicators();
        combatUI.ShowCombatLog(
            $"{blocker.Name} guards! Damage reduction: {blocker.BlockReduction * 100f:F1}%",
            () => combatUI.ShowCombatLog(" ", () => NextTurn()));
    }

    public void PlayerEvade()
    {
        Combatant evader = turnOrder[currentTurnIndex];
        evader.SetEvading(true);
        combatUI.UpdateAllHP(party, enemies);
        CombatSpriteManager.Instance?.UpdateEnemyLabels(enemies);
        UpdateStatusIndicators();
        combatUI.ShowCombatLog(
            $"{evader.Name} readies an evade! Dodge chance: {evader.EvadeChance * 100f:F1}%",
            () => combatUI.ShowCombatLog(" ", () => NextTurn()));
    }

    public void PlayerManaAttack(ManaAttackSO spell)
    {
        Combatant attacker = turnOrder[currentTurnIndex];

        if (!attacker.SpendMana(spell.manaCost))
        {
            combatUI.ShowCombatLog("Not enough mana!");
            return;
        }

        if (spell.spellType == SpellType.Heal)
        {
            combatUI.OpenSpellMemberSelect(spell, attacker, (selectedMember) =>
                ExecuteHealSpell(spell, attacker, selectedMember));
            return;
        }

        if (spell.spellType == SpellType.Buff)
        {
            combatUI.OpenSpellMemberSelect(spell, attacker, (selectedMember) =>
                ExecuteBuffSpell(spell, attacker, selectedMember));
            return;
        }

        if (spell.spellType == SpellType.Debuff)
        {
            ExecuteDebuffSpell(spell, attacker);
            return;
        }

        // AOE damage
        if (spell.isAOE)
        {
            ExecuteAOESpell(spell, attacker);
            return;
        }

        // Single target damage
        while (selectedEnemyIndex < enemies.Count && !enemies[selectedEnemyIndex].IsAlive)
            selectedEnemyIndex++;
        if (selectedEnemyIndex >= enemies.Count) return;

        Combatant target = enemies[selectedEnemyIndex];
        float affinityMult = attacker.Affinities.Contains(spell.affinity) &&
            spell.affinity != SpellAffinity.None ? 1.5f : 1f;
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
        UpdateStatusIndicators();

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

        combatUI.ShowCombatLog(" ", () => NextTurn());
    }

    void ExecuteAOESpell(ManaAttackSO spell, Combatant attacker)
    {
        var aliveEnemies = enemies.Where(e => e.IsAlive).ToList();
        if (aliveEnemies.Count == 0) return;

        float affinityMult = attacker.Affinities.Contains(spell.affinity) &&
            spell.affinity != SpellAffinity.None ? 1.5f : 1f;

        combatUI.ShowCombatLog($"{attacker.Name} uses {spell.spellName}! Hits all enemies!");

        bool anyDefeated = false;

        foreach (var target in aliveEnemies)
        {
            float mult = affinityMult;
            string comboMsg = HandleElementalCombos(attacker, target, spell.affinity, ref mult);

            int damage = Mathf.Max(1, Mathf.RoundToInt((spell.flatDamage - target.Defense) * mult));

            if (!string.IsNullOrEmpty(comboMsg))
                combatUI.ShowCombatLog(comboMsg);

            // AOE can't crit
            if (target.CombatStyle == CombatStyle.Evade && target.TryEvade())
            {
                combatUI.ShowCombatLog($"{target.Name} evaded!");
                continue;
            }

            if (target.CombatStyle == CombatStyle.Block && target.IsBlocking)
            {
                int reduced = Mathf.RoundToInt(damage * (1f - target.BlockReduction));
                target.TakeDamage(reduced);
                CombatSpriteManager.Instance?.PlayHitEffect(target.Name, reduced);
                combatUI.ShowCombatLog($"{target.Name} takes {reduced} damage! (B!)");
            }
            else
            {
                target.TakeDamage(damage);
                CombatSpriteManager.Instance?.PlayHitEffect(target.Name, damage);
                combatUI.ShowCombatLog($"{target.Name} takes {damage} damage!");
            }

            // Apply status to each target
            ApplySpellEffects(spell, attacker, target, damage);

            if (!target.IsAlive)
            {
                CombatSpriteManager.Instance?.PlayDefeatedEffect(target.Name);
                combatUI.ShowCombatLog($"{target.Name} was defeated!");
                anyDefeated = true;
            }
        }

        combatUI.UpdateAllHP(party, enemies);
        combatUI.BuildEnemyTargetButtons(enemies);
        CombatSpriteManager.Instance?.UpdateEnemyLabels(enemies);
        UpdateStatusIndicators();

        if (enemies.All(e => !e.IsAlive))
        {
            combatUI.ShowCombatLog(" ", () => HandleVictory());
            return;
        }

        if (anyDefeated)
        {
            selectedEnemyIndex = enemies.FindIndex(e => e.IsAlive);
            combatUI.HighlightSelectedEnemy(selectedEnemyIndex);
        }

        combatUI.ShowCombatLog(" ", () => NextTurn());
    }

    public void ExecuteHealSpell(ManaAttackSO spell, Combatant caster, CharacterInstance target)
    {
        int heal = spell.flatHeal + Mathf.RoundToInt(target.MaxHP * spell.percentHeal);
        heal = Mathf.Max(0, Mathf.Min(heal, target.MaxHP - target.currentHP));
        target.currentHP = Mathf.Min(target.MaxHP, target.currentHP + heal);

        var combatant = party.Find(p => p.Name == target.Name);
        combatant?.Refresh();

        CombatSpriteManager.Instance?.ShowDamageNumber(target.Name, heal, true);
        combatUI.ShowCombatLog($"{caster.Name} uses {spell.spellName} on {target.Name}! Recovered {heal} HP!");
        combatUI.UpdateAllHP(party, enemies);
        UpdateStatusIndicators();
        combatUI.ShowCombatLog(" ", () => NextTurn());
    }

    public void ExecuteBuffSpell(ManaAttackSO spell, Combatant caster, CharacterInstance target)
    {
        target.statModifiers.Add(new StatModifier(
            spell.statType, spell.statModifier, spell.modifierDuration));

        var combatant = party.Find(p => p.Name == target.Name);
        combatant?.Refresh();

        combatUI.ShowCombatLog($"{caster.Name} uses {spell.spellName} on {target.Name}! " +
            $"{spell.statType} +{spell.statModifier} for {spell.modifierDuration} turns!");
        combatUI.UpdateAllHP(party, enemies);
        UpdateStatusIndicators();
        combatUI.ShowCombatLog(" ", () => NextTurn());
    }

    void ExecuteDebuffSpell(ManaAttackSO spell, Combatant caster)
    {
        while (selectedEnemyIndex < enemies.Count && !enemies[selectedEnemyIndex].IsAlive)
            selectedEnemyIndex++;
        if (selectedEnemyIndex >= enemies.Count) return;

        Combatant target = enemies[selectedEnemyIndex];
        var enemyInst = GetEnemyInstance(target.Name);

        combatUI.ShowCombatLog($"{caster.Name} uses {spell.spellName} on {target.Name}!");

        if (enemyInst != null && spell.statModifier != 0)
        {
            int mod = -Mathf.Abs(spell.statModifier);
            enemyInst.statModifiers.Add(new StatModifier(spell.statType, mod, spell.modifierDuration));
            target.Refresh();
            combatUI.ShowCombatLog($"{target.Name}'s {spell.statType} {mod} for {spell.modifierDuration} turns!");
        }

        if (spell.statusEffect != StatusEffectType.None)
        {
            target.ApplyStatusEffect(spell.statusEffect, spell.statusChance,
                spell.statusDuration, spell.dotPercent, spell.defenseReduction, 0);
            if (target.HasStatusEffect(spell.statusEffect))
                combatUI.ShowCombatLog($"{target.Name} is afflicted with {spell.statusEffect}!");
        }

        combatUI.UpdateAllHP(party, enemies);
        CombatSpriteManager.Instance?.UpdateEnemyLabels(enemies);
        UpdateStatusIndicators();
        combatUI.ShowCombatLog(" ", () => NextTurn());
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
                UpdateStatusIndicators();
                combatUI.ShowCombatLog($"{attacker.Name} guards!",
                    () => ProcessDotsAndNextTurn(attacker));
            }
            else
            {
                attacker.SetEvading(true);
                UpdateStatusIndicators();
                combatUI.ShowCombatLog($"{attacker.Name} readies an evade!",
                    () => ProcessDotsAndNextTurn(attacker));
            }
            return;
        }

        Combatant target = aliveParty[Random.Range(0, aliveParty.Count)];
        var availableSpells = GetEnemyAvailableSpells();
        bool useSpell = availableSpells != null &&
            availableSpells.Count > 0 && Random.value > 0.5f;

        if (useSpell)
        {
            var spell = availableSpells[Random.Range(0, availableSpells.Count)];
            attacker.SpendMana(spell.manaCost);

            float affinityMult = attacker.Affinities.Contains(spell.affinity) &&
                spell.affinity != SpellAffinity.None ? 1.5f : 1f;
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
            ResolveAttack(attacker, target, damage, "basic attack", true);
        }

        combatUI.UpdateAllHP(party, enemies);
        CombatSpriteManager.Instance?.UpdateEnemyLabels(enemies);
        UpdateStatusIndicators();

        combatUI.ShowCombatLog(" ", () => ProcessDotsAndNextTurn(attacker));
    }

    void ProcessDotsAndNextTurn(Combatant attacker)
    {
        var detailed = attacker.ProcessStatusEffectsDetailed();

        foreach (var (log, damage, isDot) in detailed)
            if (isDot && damage > 0)
                CombatSpriteManager.Instance?.ShowDamageNumber(attacker.Name, damage);

        var dotLogs = detailed.ConvertAll(d => d.log);

        combatUI.UpdateAllHP(party, enemies);
        combatUI.BuildEnemyTargetButtons(enemies);
        CombatSpriteManager.Instance?.UpdateEnemyLabels(enemies);
        UpdateStatusIndicators();

        if (!attacker.IsAlive)
        {
            if (attacker.IsEnemy)
                CombatSpriteManager.Instance?.PlayDefeatedEffect(attacker.Name);
            else
                CombatSpriteManager.Instance?.PlayPartyDefeatedEffect(attacker.Name);

            if (enemies.All(e => !e.IsAlive))
            {
                if (dotLogs.Count > 0)
                    combatUI.ShowCombatLogs(dotLogs,
                        () => combatUI.ShowCombatLog(" ", () => HandleVictory()));
                else
                    HandleVictory();
                return;
            }

            if (!attacker.IsEnemy && PartyManager.Instance.IsGameOver())
            {
                foreach (var m in party)
                    if (!m.IsAlive)
                        CombatSpriteManager.Instance?.PlayPartyDefeatedEffect(m.Name);

                if (dotLogs.Count > 0)
                    combatUI.ShowCombatLogs(dotLogs, () =>
                        combatUI.ShowCombatLog(" ", () =>
                        {
                            combatUI.ShowGameOver();
                            combatActive = false;
                        }));
                else { combatUI.ShowGameOver(); combatActive = false; }
                return;
            }

            if (attacker.IsEnemy)
                selectedEnemyIndex = enemies.FindIndex(e => e.IsAlive);
        }

        if (PartyManager.Instance.IsGameOver())
        {
            foreach (var m in party)
                if (!m.IsAlive)
                    CombatSpriteManager.Instance?.PlayPartyDefeatedEffect(m.Name);

            if (dotLogs.Count > 0)
                combatUI.ShowCombatLogs(dotLogs, () =>
                    combatUI.ShowCombatLog(" ", () =>
                    {
                        combatUI.ShowGameOver();
                        combatActive = false;
                    }));
            else { combatUI.ShowGameOver(); combatActive = false; }
            return;
        }

        if (dotLogs.Count > 0)
            combatUI.ShowCombatLogs(dotLogs,
                () => combatUI.ShowCombatLog(" ", () => NextTurn()));
        else
            NextTurn();
    }

    List<EnemyManaAttackSO> GetEnemyAvailableSpells()
    {
        Combatant current = turnOrder[currentTurnIndex];
        if (!current.IsEnemy) return null;
        int idx = enemies.IndexOf(current);
        if (idx < 0 || idx >= EncounterManager.CurrentEnemies.Count) return null;
        return EncounterManager.CurrentEnemies[idx].spells
            .FindAll(s => current.GetCurrentMana() >= s.manaCost);
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
                if (Random.Range(0f, 100f) <= drop.dropChance)
                {
                    InventoryManager.Instance.AddItem(drop.item);
                    drops.itemsDropped.Add(drop.item);
                }

            foreach (var drop in enemyData.gearDrops)
                if (Random.Range(0f, 100f) <= drop.dropChance)
                {
                    GearManager.Instance.AddGearToInventory(drop.gear);
                    drops.gearDropped.Add(drop.gear);
                }
        }

        GoldManager.Instance.AddGold(totalGold);
        PartyManager.Instance.GiveXPToAll(totalXP);
        combatUI.ShowVictory(totalXP, totalGold, drops);
        combatActive = false;
    }
}