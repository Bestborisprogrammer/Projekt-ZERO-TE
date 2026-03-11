using UnityEngine;
using System.Collections.Generic;
using System.Linq;

public class TurnCombatManager : MonoBehaviour
{
    public static TurnCombatManager Instance;

    [Header("UI")]
    public CombatUI combatUI;

    // Alle Kämpfer in der Runde
    private List<Combatant> turnOrder = new();
    private int currentTurnIndex = 0;
    private bool combatActive = false;

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

        // Party hinzufügen
        foreach (var member in PartyManager.Instance.activeParty)
        {
            if (member.IsAlive)
                turnOrder.Add(new Combatant(member));
        }

        // Enemy aus EncounterManager holen
        EnemyStatsSO enemyData = EncounterManager.CurrentEnemy;
        if (enemyData != null)
        {
            EnemyInstance enemy = new EnemyInstance { baseData = enemyData };
            enemy.Initialize();
            turnOrder.Add(new Combatant(enemy));
        }

        // Nach Speed sortieren (höchster zuerst)
        turnOrder = turnOrder.OrderByDescending(c => c.Speed).ToList();

        combatActive = true;
        Debug.Log("Combat gestartet!");
        StartTurn();
    }

    void StartTurn()
    {
        if (!combatActive) return;

        Combatant current = turnOrder[currentTurnIndex];
        Debug.Log($"--- {current.Name}'s Turn ---");
        combatUI.UpdateTurnText(current.Name);

        if (current.IsEnemy)
        {
            // Enemy macht automatisch seinen Zug
            combatUI.SetPlayerButtonsActive(false);
            Invoke(nameof(EnemyTurn), 1f); // kurze Pause damit es sich natürlich anfühlt
        }
        else
        {
            // Spieler wartet auf Input
            combatUI.SetPlayerButtonsActive(true);
        }
    }

    // Spieler drückt Basic Attack
    public void PlayerBasicAttack()
    {
        Combatant attacker = turnOrder[currentTurnIndex];
        Combatant target = GetFirstAliveEnemy();

        if (target == null) return;

        int damage = Mathf.Max(1, attacker.Attack - target.Defense);
        target.TakeDamage(damage);

        Debug.Log($"{attacker.Name} greift {target.Name} an! {damage} Schaden!");
        combatUI.ShowDamageText(target.Name, damage);
        combatUI.UpdateEnemyHP(target.CurrentHP, target.MaxHP);

        if (!target.IsAlive)
        {
            HandleEnemyDeath(target);
            return;
        }

        NextTurn();
    }

    void EnemyTurn()
    {
        Combatant attacker = turnOrder[currentTurnIndex];
        Combatant target = GetFirstAlivePartyMember();

        if (target == null) return;

        int damage = Mathf.Max(1, attacker.Attack - target.Defense);
        target.TakeDamage(damage);

        Debug.Log($"{attacker.Name} greift {target.Name} an! {damage} Schaden!");
        combatUI.ShowDamageText(target.Name, damage);
        combatUI.UpdatePartyHP(target.Name, target.CurrentHP, target.MaxHP);

        if (PartyManager.Instance.IsGameOver())
        {
            Debug.Log("GAME OVER");
            combatUI.ShowGameOver();
            combatActive = false;
            return;
        }

        NextTurn();
    }

    void NextTurn()
    {
        // Tote Kämpfer überspringen
        int attempts = 0;
        do
        {
            currentTurnIndex = (currentTurnIndex + 1) % turnOrder.Count;
            attempts++;
        }
        while (!turnOrder[currentTurnIndex].IsAlive && attempts < turnOrder.Count);

        StartTurn();
    }

    void HandleEnemyDeath(Combatant enemy)
    {
        Debug.Log($"{enemy.Name} wurde besiegt!");

        // Merken welcher Enemy besiegt wurde
        EnemyEncounter.DefeatedEnemyID = enemy.Name;

        int xp = enemy.XPReward;
        PartyManager.Instance.GiveXPToAll(xp);
        combatUI.ShowVictory(xp);
        combatActive = false;
    }

    Combatant GetFirstAliveEnemy()
    {
        return turnOrder.FirstOrDefault(c => c.IsEnemy && c.IsAlive);
    }

    Combatant GetFirstAlivePartyMember()
    {
        return turnOrder.FirstOrDefault(c => !c.IsEnemy && c.IsAlive);
    }
}