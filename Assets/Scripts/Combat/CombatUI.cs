using UnityEngine;
using TMPro;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using System.Collections;

public class CombatUI : MonoBehaviour
{
    [Header("Turn Info")]
    public TextMeshProUGUI turnText;

    [Header("Enemy Info")]
    public TextMeshProUGUI enemyNameText;
    public TextMeshProUGUI enemyHPText;
    public Slider enemyHPSlider;

    [Header("Party Info")]
    public TextMeshProUGUI partyHPText;

    [Header("Combat Log")]
    public TextMeshProUGUI combatLogText;

    [Header("Buttons")]
    public Button basicAttackButton;

    [Header("Result Panels")]
    public GameObject victoryPanel;
    public TextMeshProUGUI victoryXPText;
    public GameObject gameOverPanel;

    [Header("Scene")]
    public string overworldScene = "OverworldScene";

    void Start()
    {
        victoryPanel.SetActive(false);
        gameOverPanel.SetActive(false);

        if (EncounterManager.CurrentEnemy != null)
            enemyNameText.text = EncounterManager.CurrentEnemy.enemyName;

        basicAttackButton.onClick.AddListener(TurnCombatManager.Instance.PlayerBasicAttack);
    }

    public void UpdateTurnText(string name)
    {
        turnText.text = $"{name}'s Turn";
    }

    public void ShowDamageText(string target, int damage)
    {
        combatLogText.text = $"{target} erhielt {damage} Schaden!";
    }

    public void UpdateEnemyHP(int current, int max)
    {
        enemyHPText.text = $"HP: {current}/{max}";
        enemyHPSlider.maxValue = max;
        enemyHPSlider.value = current;
    }

    public void UpdatePartyHP(string name, int current, int max)
    {
        partyHPText.text = $"{name} HP: {current}/{max}";
    }

    public void SetPlayerButtonsActive(bool active)
    {
        basicAttackButton.interactable = active;
    }

    public void ShowVictory(int xp)
    {
        victoryPanel.SetActive(true);
        victoryXPText.text = $"Sieg! +{xp} XP";
        StartCoroutine(ReturnToOverworldAfterDelay(1.5f));
    }

    public void ShowGameOver()
    {
        gameOverPanel.SetActive(true);
    }

    IEnumerator ReturnToOverworldAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);
        SceneManager.LoadScene(overworldScene);
    }
}