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
    public Button skillsButton;
    public Button blockButton;
    public Image blockButtonImage;
    public Sprite blockSprite;
    public Sprite evadeSprite;

    [Header("Skill Panel")]
    public GameObject skillPanel;
    public Transform skillButtonParent;
    public GameObject skillButtonPrefab;
    public Button skillPrevButton;
    public Button skillNextButton;
    public TextMeshProUGUI skillPageText;

    [Header("Result Panel")]
    public GameObject victoryPanel;
    public TextMeshProUGUI victoryXPText;

    [Header("Scene")]
    public string overworldScene = "OverworldScene";

    private List<Button> enemyButtons = new();
    private int highlightedIndex = 0;

    private List<ManaAttackSO> currentSpells = new();
    private int spellPage = 0;
    private const int spellsPerPage = 3;
    private int currentMana = 0;

    private Queue<string> logQueue = new Queue<string>();
    private bool isShowingLog = false;
    private float logDelay = 2.2f; // 1.2s read time + 1s gap

    void Start()
    {
        victoryPanel.SetActive(false);
        skillPanel.SetActive(false);
        skillPrevButton.gameObject.SetActive(false);
        skillNextButton.gameObject.SetActive(false);
        skillPageText.gameObject.SetActive(false);

        basicAttackButton.onClick.AddListener(OnBasicAttack);
        skillsButton.onClick.AddListener(ToggleSkillPanel);
        skillPrevButton.onClick.AddListener(SpellPagePrev);
        skillNextButton.onClick.AddListener(SpellPageNext);
    }

    void OnBasicAttack()
    {
        skillPanel.SetActive(false);
        skillPageText.gameObject.SetActive(false);
        skillPrevButton.gameObject.SetActive(false);
        skillNextButton.gameObject.SetActive(false);
        TurnCombatManager.Instance.PlayerBasicAttack();
    }

    void OnBlock()
    {
        skillPanel.SetActive(false);
        skillPageText.gameObject.SetActive(false);
        skillPrevButton.gameObject.SetActive(false);
        skillNextButton.gameObject.SetActive(false);
        TurnCombatManager.Instance.PlayerBlock();
    }

    void OnEvade()
    {
        skillPanel.SetActive(false);
        skillPageText.gameObject.SetActive(false);
        skillPrevButton.gameObject.SetActive(false);
        skillNextButton.gameObject.SetActive(false);
        TurnCombatManager.Instance.PlayerEvade();
    }

    void ToggleSkillPanel()
    {
        bool isOpening = !skillPanel.activeSelf;
        skillPanel.SetActive(isOpening);
        skillPageText.gameObject.SetActive(isOpening);

        if (!isOpening)
        {
            skillPrevButton.gameObject.SetActive(false);
            skillNextButton.gameObject.SetActive(false);
        }
        else
        {
            RebuildSpellPage();
        }
    }

    public void ShowSpellButtons(List<ManaAttackSO> spells, int mana)
    {
        currentSpells = spells ?? new List<ManaAttackSO>();
        currentMana = mana;
        spellPage = 0;
    }

    void SpellPagePrev()
    {
        if (spellPage > 0) spellPage--;
        RebuildSpellPage();
    }

    void SpellPageNext()
    {
        int maxPage = Mathf.CeilToInt((float)currentSpells.Count / spellsPerPage) - 1;
        if (spellPage < maxPage) spellPage++;
        RebuildSpellPage();
    }

    void RebuildSpellPage()
    {
        foreach (Transform child in skillButtonParent)
            Destroy(child.gameObject);

        int start = spellPage * spellsPerPage;
        int end = Mathf.Min(start + spellsPerPage, currentSpells.Count);

        for (int i = start; i < end; i++)
        {
            var spell = currentSpells[i];
            GameObject btn = Instantiate(skillButtonPrefab, skillButtonParent);
            var tmp = btn.GetComponentInChildren<TextMeshProUGUI>();
            bool canAfford = currentMana >= spell.manaCost;

            tmp.text = $"{spell.spellName}  |  MP: {spell.manaCost}\n{spell.description}";
            tmp.color = canAfford ? Color.white : Color.grey;

            var button = btn.GetComponent<Button>();
            button.interactable = canAfford;

            var capturedSpell = spell;
            button.onClick.AddListener(() =>
            {
                skillPanel.SetActive(false);
                skillPageText.gameObject.SetActive(false);
                skillPrevButton.gameObject.SetActive(false);
                skillNextButton.gameObject.SetActive(false);
                TurnCombatManager.Instance.PlayerManaAttack(capturedSpell);
            });
        }

        int totalPages = Mathf.Max(1, Mathf.CeilToInt((float)currentSpells.Count / spellsPerPage));
        skillPageText.text = $"{spellPage + 1}/{totalPages}";

        bool multiPage = totalPages > 1;
        skillPrevButton.gameObject.SetActive(multiPage);
        skillNextButton.gameObject.SetActive(multiPage);
    }

    public void ShowCombatLog(string message)
    {
        logQueue.Enqueue(message);
        if (!isShowingLog)
            StartCoroutine(ProcessLogQueue());
    }

    IEnumerator ProcessLogQueue()
    {
        isShowingLog = true;
        while (logQueue.Count > 0)
        {
            combatLogText.text = logQueue.Dequeue();
            yield return new WaitForSeconds(logDelay);
        }
        isShowingLog = false;
    }

    public void UpdateTurnText(string name)
    {
        turnText.text = $"{name}'s Turn";
    }

    public void UpdateAllHP(List<Combatant> party, List<Combatant> enemies)
    {
        foreach (Transform child in partyHPParent)
            Destroy(child.gameObject);

        foreach (var member in party)
        {
            GameObject entry = Instantiate(partyHPEntryPrefab, partyHPParent);
            var tmp = entry.GetComponent<TextMeshProUGUI>();
            string indicator = "";
            if (member.IsBlocking) indicator = " B!";
            else if (member.CombatStyle == CombatStyle.Evade) indicator = " E!";
            tmp.text = $"{member.Name}  HP: {member.CurrentHP}/{member.MaxHP}  MP: {member.GetCurrentMana()}{indicator}";
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
            string indicator = "";
            if (enemies[i].IsBlocking) indicator = " B!";
            else if (enemies[i].CombatStyle == CombatStyle.Evade) indicator = " E!";
            tmp.text = $"{enemies[i].Name}{indicator}\nHP: {enemies[i].CurrentHP}/{enemies[i].MaxHP}";

            var button = btn.GetComponent<Button>();
            button.onClick.AddListener(() =>
            {
                TurnCombatManager.Instance.SelectEnemy(enemyIndex);
                HighlightSelectedEnemy(enemyButtons.IndexOf(button));
            });

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
            colors.normalColor = (i == index) ? Color.green : Color.white;
            colors.highlightedColor = (i == index) ? Color.green : new Color(0.9f, 0.9f, 0.9f);
            colors.selectedColor = (i == index) ? Color.green : Color.white;
            enemyButtons[i].colors = colors;
        }
    }

    public void SetPlayerButtonsActive(bool active, CombatStyle style = CombatStyle.Block)
    {
        basicAttackButton.interactable = active;
        skillsButton.interactable = active;
        blockButton.interactable = active;

        blockButton.onClick.RemoveAllListeners();
        if (style == CombatStyle.Block)
        {
            if (blockSprite != null) blockButtonImage.sprite = blockSprite;
            blockButton.onClick.AddListener(OnBlock);
        }
        else
        {
            if (evadeSprite != null) blockButtonImage.sprite = evadeSprite;
            blockButton.onClick.AddListener(OnEvade);
        }

        if (!active)
        {
            skillPanel.SetActive(false);
            skillPageText.gameObject.SetActive(false);
            skillPrevButton.gameObject.SetActive(false);
            skillNextButton.gameObject.SetActive(false);
        }

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