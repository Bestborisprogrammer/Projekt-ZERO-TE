using UnityEngine;
using TMPro;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using System.Collections;
using System.Collections.Generic;

public class CombatUI : MonoBehaviour
{
    [Header("Turn Info")]
    public TextMeshProUGUI turnText;

    [Header("Combat Log")]
    public TextMeshProUGUI combatLogText;

    [Header("Party HP Panel")]
    public Transform partyHPParent;
    public GameObject partyHPEntryPrefab;

    [Header("Enemy Target Buttons")]
    public Transform enemyButtonParent;
    public GameObject enemyButtonPrefab;

    [Header("Action Buttons")]
    public Button basicAttackButton;

    [Header("Result Panel")]
    public GameObject victoryPanel;
    public TextMeshProUGUI victoryXPText;

    [Header("Scene")]
    public string overworldScene = "OverworldScene";

    private List<Button> enemyButtons = new();
    private int highlightedIndex = 0;

    void Start()
    {
        victoryPanel.SetActive(false);
        basicAttackButton.onClick.AddListener(TurnCombatManager.Instance.PlayerBasicAttack);
    }

    public void UpdateTurnText(string name)
    {
        turnText.text = $"{name}'s Turn";
    }

    public void ShowCombatLog(string message)
    {
        combatLogText.text = message;
    }

    public void UpdateAllHP(List<Combatant> party, List<Combatant> enemies)
    {
        foreach (Transform child in partyHPParent)
            Destroy(child.gameObject);

        foreach (var member in party)
        {
            GameObject entry = Instantiate(partyHPEntryPrefab, partyHPParent);
            var tmp = entry.GetComponent<TextMeshProUGUI>();
            tmp.text = $"{member.Name}  HP: {member.CurrentHP}/{member.MaxHP}";
            tmp.color = member.IsAlive ? Color.white : Color.red;
        }
    }

    public void BuildEnemyTargetButtons(List<Combatant> enemies)
    {
        foreach (Transform child in enemyButtonParent)
            Destroy(child.gameObject);
        enemyButtons.Clear();

        for (int i = 0; i < enemies.Count; i++)
        {
            if (!enemies[i].IsAlive) continue;

            int enemyIndex = i;

            GameObject btn = Instantiate(enemyButtonPrefab, enemyButtonParent);
            var tmp = btn.GetComponentInChildren<TextMeshProUGUI>();
            tmp.text = $"{enemies[i].Name}\nHP: {enemies[i].CurrentHP}/{enemies[i].MaxHP}";

            var button = btn.GetComponent<Button>();
            button.onClick.AddListener(() => TurnCombatManager.Instance.SelectEnemy(enemyIndex));

            enemyButtons.Add(button);
        }

        HighlightSelectedEnemy(highlightedIndex);
    }

    public void HighlightSelectedEnemy(int index)
    {
        highlightedIndex = index;

        for (int i = 0; i < enemyButtons.Count; i++)
        {
            var colors = enemyButtons[i].colors;
            colors.normalColor = (i == index) ? Color.yellow : Color.white;
            colors.highlightedColor = (i == index) ? Color.yellow : new Color(0.9f, 0.9f, 0.9f);
            enemyButtons[i].colors = colors;
        }
    }

    public void SetPlayerButtonsActive(bool active)
    {
        basicAttackButton.interactable = active;
        foreach (var btn in enemyButtons)
            btn.interactable = active;
    }

    public void ShowVictory(int xp)
    {
        victoryPanel.SetActive(true);
        victoryXPText.text = $"Victory! +{xp} XP";
        StartCoroutine(ReturnAfterDelay(2.5f));
    }

    public void ShowGameOver()
    {
        foreach (var member in PartyManager.Instance.activeParty)
            member.currentHP = 1;

        victoryPanel.SetActive(true);
        victoryXPText.text = "Your party has been defeated...";
        StartCoroutine(ReturnAfterDelay(3f));
    }

    IEnumerator ReturnAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);
        SceneManager.LoadScene(overworldScene);
    }
}