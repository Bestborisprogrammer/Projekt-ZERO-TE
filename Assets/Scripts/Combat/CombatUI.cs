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
    public Button itemsButton;
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

    [Header("Item Panel")]
    public GameObject itemPanel;
    public Transform itemButtonParent;
    public GameObject itemButtonPrefab;
    public Button itemPrevButton;
    public Button itemNextButton;
    public TextMeshProUGUI itemPageText;

    [Header("Member Select Popup")]
    public GameObject memberSelectPopup;
    public Transform memberSelectParent;
    public GameObject memberSelectButtonPrefab;

    [Header("Result Panel")]
    public GameObject victoryPanel;
    public TextMeshProUGUI victoryXPText;

    [Header("Scene")]
    public string overworldScene = "OverworldScene";

    private List<Button> enemyButtons = new();
    private int highlightedIndex = 0;

    // Spell paging
    private List<ManaAttackSO> currentSpells = new();
    private int spellPage = 0;
    private const int spellsPerPage = 3;
    private int currentMana = 0;

    // Item paging
    private List<InventoryItem> currentItems = new();
    private int itemPage = 0;
    private const int itemsPerPage = 3;
    private InventoryItem pendingItem;

    private Queue<string> logQueue = new Queue<string>();
    private bool isShowingLog = false;
    private float logDelay = 2.2f;

    void Start()
    {
        victoryPanel.SetActive(false);
        skillPanel.SetActive(false);
        itemPanel.SetActive(false);
        memberSelectPopup.SetActive(false);
        skillPrevButton.gameObject.SetActive(false);
        skillNextButton.gameObject.SetActive(false);
        skillPageText.gameObject.SetActive(false);
        itemPrevButton.gameObject.SetActive(false);
        itemNextButton.gameObject.SetActive(false);
        itemPageText.gameObject.SetActive(false);

        basicAttackButton.onClick.AddListener(OnBasicAttack);
        skillsButton.onClick.AddListener(ToggleSkillPanel);
        itemsButton.onClick.AddListener(ToggleItemPanel);
        skillPrevButton.onClick.AddListener(SpellPagePrev);
        skillNextButton.onClick.AddListener(SpellPageNext);
        itemPrevButton.onClick.AddListener(ItemPagePrev);
        itemNextButton.onClick.AddListener(ItemPageNext);
    }

    // ── Basic Attack ──────────────────────────────
    void OnBasicAttack()
    {
        CloseAllPanels();
        TurnCombatManager.Instance.PlayerBasicAttack();
    }

    // ── Block / Evade ─────────────────────────────
    void OnBlock()
    {
        CloseAllPanels();
        TurnCombatManager.Instance.PlayerBlock();
    }

    void OnEvade()
    {
        CloseAllPanels();
        TurnCombatManager.Instance.PlayerEvade();
    }

    // ── Skill Panel ───────────────────────────────
    void ToggleSkillPanel()
    {
        bool opening = !skillPanel.activeSelf;
        CloseAllPanels();
        if (opening)
        {
            skillPanel.SetActive(true);
            skillPageText.gameObject.SetActive(true);
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
                CloseAllPanels();
                TurnCombatManager.Instance.PlayerManaAttack(capturedSpell);
            });
        }

        int totalPages = Mathf.Max(1, Mathf.CeilToInt((float)currentSpells.Count / spellsPerPage));
        skillPageText.text = $"{spellPage + 1}/{totalPages}";
        skillPrevButton.gameObject.SetActive(totalPages > 1);
        skillNextButton.gameObject.SetActive(totalPages > 1);
    }

    // ── Item Panel ────────────────────────────────
    void ToggleItemPanel()
    {
        bool opening = !itemPanel.activeSelf;
        CloseAllPanels();
        if (opening)
        {
            itemPanel.SetActive(true);
            itemPageText.gameObject.SetActive(true);
            currentItems = InventoryManager.Instance.items
                .FindAll(i => i.itemData.itemTarget == ItemTarget.Ally ||
                              i.itemData.itemTarget == ItemTarget.Enemy);
            itemPage = 0;
            RebuildItemPage();
        }
    }

    void ItemPagePrev()
    {
        if (itemPage > 0) itemPage--;
        RebuildItemPage();
    }

    void ItemPageNext()
    {
        int maxPage = Mathf.CeilToInt((float)currentItems.Count / itemsPerPage) - 1;
        if (itemPage < maxPage) itemPage++;
        RebuildItemPage();
    }

    void RebuildItemPage()
    {
        foreach (Transform child in itemButtonParent)
            Destroy(child.gameObject);

        int start = itemPage * itemsPerPage;
        int end = Mathf.Min(start + itemsPerPage, currentItems.Count);

        for (int i = start; i < end; i++)
        {
            var item = currentItems[i];
            GameObject btn = Instantiate(itemButtonPrefab, itemButtonParent);
            var tmp = btn.GetComponentInChildren<TextMeshProUGUI>();

            string effectInfo = "";
            if (item.itemData.itemType == ItemType.Heal)
                effectInfo = $"Heal: {item.itemData.flatHeal} HP" +
                    (item.itemData.percentHeal > 0 ? $" +{item.itemData.percentHeal * 100f:F0}%" : "");
            else if (item.itemData.itemType == ItemType.Buff)
                effectInfo = $"+{item.itemData.statModifier} {item.itemData.statType} ({item.itemData.modifierDuration} turns)";
            else if (item.itemData.itemType == ItemType.Debuff)
                effectInfo = $"-{item.itemData.statModifier} {item.itemData.statType} ({item.itemData.modifierDuration} turns)";

            string targetTag = item.itemData.itemTarget == ItemTarget.Enemy ? "[ENEMY]" : "[ALLY]";
            tmp.text = $"{item.itemData.itemName} x{item.quantity} {targetTag}\n{effectInfo}";

            var button = btn.GetComponent<Button>();
            var capturedItem = item;

            button.onClick.AddListener(() =>
            {
                if (capturedItem.itemData.itemTarget == ItemTarget.Ally)
                    OpenMemberSelectPopup(capturedItem);
                else
                    UseItemOnEnemy(capturedItem);
            });
        }

        int totalPages = Mathf.Max(1, Mathf.CeilToInt((float)currentItems.Count / itemsPerPage));
        itemPageText.text = $"{itemPage + 1}/{totalPages}";
        itemPrevButton.gameObject.SetActive(totalPages > 1);
        itemNextButton.gameObject.SetActive(totalPages > 1);
    }

    // ── Member Select Popup ───────────────────────
    void OpenMemberSelectPopup(InventoryItem item)
    {
        pendingItem = item;
        memberSelectPopup.SetActive(true);

        foreach (Transform child in memberSelectParent)
            Destroy(child.gameObject);

        foreach (var member in PartyManager.Instance.activeParty)
        {
            if (!member.IsAlive) continue;

            GameObject btn = Instantiate(memberSelectButtonPrefab, memberSelectParent);
            var tmp = btn.GetComponentInChildren<TextMeshProUGUI>();

            string preview = "";
            if (item.itemData.itemType == ItemType.Heal)
            {
                int heal = item.itemData.flatHeal + Mathf.RoundToInt(member.MaxHP * item.itemData.percentHeal);
                int actual = Mathf.Min(heal, member.MaxHP - member.currentHP);
                preview = $"+{actual} HP";
            }
            else if (item.itemData.itemType == ItemType.Buff)
                preview = $"+{item.itemData.statModifier} {item.itemData.statType}";

            tmp.text = $"{member.Name}\nHP: {member.currentHP}/{member.MaxHP}\n{preview}";

            var capturedMember = member;
            btn.GetComponent<Button>()?.onClick.AddListener(() => UseItemOnAlly(capturedMember));
        }
    }

    void UseItemOnAlly(CharacterInstance member)
    {
        if (pendingItem == null) return;

        string logMsg = "";

        if (pendingItem.itemData.itemType == ItemType.Heal)
        {
            int heal = pendingItem.itemData.flatHeal +
                Mathf.RoundToInt(member.MaxHP * pendingItem.itemData.percentHeal);
            member.HealHP(pendingItem.itemData.flatHeal, pendingItem.itemData.percentHeal);
            logMsg = $"{member.Name} used {pendingItem.itemData.itemName} and recovered {heal} HP!";
        }
        else if (pendingItem.itemData.itemType == ItemType.Buff)
        {
            member.ApplyStatModifier(pendingItem.itemData.statType,
                pendingItem.itemData.statModifier, pendingItem.itemData.modifierDuration);
            logMsg = $"{member.Name} used {pendingItem.itemData.itemName}! " +
                $"{pendingItem.itemData.statType} +{pendingItem.itemData.statModifier} " +
                $"for {pendingItem.itemData.modifierDuration} turns!";
        }

        Debug.Log($"[ITEM] {logMsg}");
        ShowCombatLog(logMsg);
        InventoryManager.Instance.RemoveItem(pendingItem.itemData);
        pendingItem = null;

        memberSelectPopup.SetActive(false);
        CloseAllPanels();
        UpdateAllHP(TurnCombatManager.Instance.party, TurnCombatManager.Instance.enemies);

        // Using item counts as the player's turn
        TurnCombatManager.Instance.NextTurnPublic();
    }

    void UseItemOnEnemy(InventoryItem item)
    {
        var target = TurnCombatManager.Instance.enemies
            .Find(e => e.IsAlive);
        if (target == null) return;

        string logMsg = "";

        if (item.itemData.itemType == ItemType.Debuff)
        {
            target.ApplyStatusEffect(item.itemData.statusEffect,
                item.itemData.statusChance, item.itemData.statusDuration,
                item.itemData.dotPercent);

            int speedReduction = 0;
            target.ApplyStatusEffect(item.itemData.statusEffect,
                item.itemData.statusChance, item.itemData.statusDuration,
                item.itemData.dotPercent, 0f, speedReduction);

            // Apply stat debuff
            if (item.itemData.statModifier != 0)
            {
                // Negative modifier for debuff
                var enemyRef = TurnCombatManager.Instance.enemies.Find(e => e == target);
                logMsg = $"Used {item.itemData.itemName} on {target.Name}! " +
                    $"{item.itemData.statType} -{item.itemData.statModifier} " +
                    $"for {item.itemData.modifierDuration} turns!";
            }
            else
                logMsg = $"Used {item.itemData.itemName} on {target.Name}!";
        }

        Debug.Log($"[ITEM] {logMsg}");
        ShowCombatLog(logMsg);
        InventoryManager.Instance.RemoveItem(item.itemData);

        CloseAllPanels();
        TurnCombatManager.Instance.NextTurnPublic();
    }

    // ── Helpers ───────────────────────────────────
    void CloseAllPanels()
    {
        skillPanel.SetActive(false);
        itemPanel.SetActive(false);
        memberSelectPopup.SetActive(false);
        skillPageText.gameObject.SetActive(false);
        itemPageText.gameObject.SetActive(false);
        skillPrevButton.gameObject.SetActive(false);
        skillNextButton.gameObject.SetActive(false);
        itemPrevButton.gameObject.SetActive(false);
        itemNextButton.gameObject.SetActive(false);
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
            string indicator = member.IsBlocking ? " B!" :
                member.CombatStyle == CombatStyle.Evade ? " E!" : "";
            tmp.text = $"{member.Name}  HP: {member.CurrentHP}/{member.MaxHP}" +
                $"  MP: {member.GetCurrentMana()}{indicator}";
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
            string indicator = enemies[i].IsBlocking ? " B!" :
                enemies[i].CombatStyle == CombatStyle.Evade ? " E!" : "";
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
        itemsButton.interactable = active;
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

        if (!active) CloseAllPanels();

        foreach (var btn in enemyButtons)
            btn.interactable = active;
    }

    public void ShowVictory(int xp, int gold, DropResult drops)
    {
        victoryPanel.SetActive(true);
        string text = $"Victory!\n+{xp} XP  +{gold} Gold\n";

        if (drops.itemsDropped.Count > 0)
        {
            text += "\nItems obtained:\n";
            foreach (var item in drops.itemsDropped)
                text += $"- {item.itemName}\n";
        }

        if (drops.gearDropped.Count > 0)
        {
            text += "\nGear obtained:\n";
            foreach (var gear in drops.gearDropped)
                text += $"- {gear.gearName}\n";
        }

        victoryXPText.text = text;
        StartCoroutine(ReturnAfterDelay(3f));
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