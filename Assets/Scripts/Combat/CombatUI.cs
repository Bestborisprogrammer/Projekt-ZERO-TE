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

    [Header("Sprites")]
    public CombatSpriteManager spriteManager;

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

    private List<InventoryItem> currentItems = new();
    private int itemPage = 0;
    private const int itemsPerPage = 3;
    private InventoryItem pendingItem;

    private Queue<(string message, System.Action callback)> logQueue = new();
    private bool isShowingLog = false;
    private bool waitingForInput = false;
    private bool actionTaken = false;

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

        victoryPanel.transform.SetAsLastSibling();

        basicAttackButton.onClick.AddListener(OnBasicAttack);
        skillsButton.onClick.AddListener(ToggleSkillPanel);
        itemsButton.onClick.AddListener(ToggleItemPanel);
        skillPrevButton.onClick.AddListener(SpellPagePrev);
        skillNextButton.onClick.AddListener(SpellPageNext);
        itemPrevButton.onClick.AddListener(ItemPagePrev);
        itemNextButton.onClick.AddListener(ItemPageNext);
    }

    void Update()
    {
        if (waitingForInput)
        {
            if (Input.GetKeyDown(KeyCode.Space) ||
                Input.GetMouseButtonDown(0) ||
                Input.anyKeyDown)
                waitingForInput = false;
        }
    }

    public void ResetActionTaken() => actionTaken = false;

    public void SetupCombatSprites(List<Combatant> party, List<Combatant> enemies)
    {
        spriteManager?.SetupSprites(party, enemies);
    }

    // ── Log System ────────────────────────────────
    public void ShowCombatLog(string message, System.Action callback = null)
    {
        logQueue.Enqueue((message, callback));
        if (!isShowingLog)
            StartCoroutine(ProcessLogQueue());
    }

    public void ShowCombatLogs(List<string> messages, System.Action callback = null)
    {
        for (int i = 0; i < messages.Count; i++)
        {
            System.Action cb = (i == messages.Count - 1) ? callback : null;
            logQueue.Enqueue((messages[i], cb));
        }
        if (!isShowingLog)
            StartCoroutine(ProcessLogQueue());
    }

    IEnumerator ProcessLogQueue()
    {
        isShowingLog = true;

        while (logQueue.Count > 0)
        {
            var (message, callback) = logQueue.Dequeue();

            // Space character = blank pause that auto-advances then fires callback
            if (message == " ")
            {
                combatLogText.text = "";
                callback?.Invoke();
                continue;
            }

            // Truly empty = just fire callback no display
            if (string.IsNullOrEmpty(message))
            {
                callback?.Invoke();
                continue;
            }

            combatLogText.text = message;
            waitingForInput = true;
            yield return new WaitUntil(() => !waitingForInput);
            callback?.Invoke();
        }

        isShowingLog = false;
    }

    // ── Action Guard ──────────────────────────────
    bool TryTakeAction()
    {
        if (actionTaken) return false;
        actionTaken = true;
        DisableAllActionButtons();
        CloseAllPanels();
        return true;
    }

    void DisableAllActionButtons()
    {
        basicAttackButton.interactable = false;
        skillsButton.interactable = false;
        itemsButton.interactable = false;
        blockButton.interactable = false;
    }

    void OnBasicAttack()
    {
        if (!TryTakeAction()) return;
        TurnCombatManager.Instance.PlayerBasicAttack();
    }

    void OnBlock()
    {
        if (!TryTakeAction()) return;
        TurnCombatManager.Instance.PlayerBlock();
    }

    void OnEvade()
    {
        if (!TryTakeAction()) return;
        TurnCombatManager.Instance.PlayerEvade();
    }

    // ── Skill Panel ───────────────────────────────
    void ToggleSkillPanel()
    {
        if (actionTaken) return;
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
                if (!TryTakeAction()) return;
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
        if (actionTaken) return;
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
                {
                    if (!TryTakeAction()) return;
                    UseItemOnEnemy(capturedItem);
                }
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
                int heal = item.itemData.flatHeal +
                    Mathf.RoundToInt(member.MaxHP * item.itemData.percentHeal);
                int actual = Mathf.Min(heal, member.MaxHP - member.currentHP);
                preview = $"+{actual} HP";
            }
            else if (item.itemData.itemType == ItemType.Buff)
                preview = $"+{item.itemData.statModifier} {item.itemData.statType}";

            tmp.text = $"{member.Name}\nHP: {member.currentHP}/{member.MaxHP}\n{preview}";

            var capturedMember = member;
            btn.GetComponent<Button>()?.onClick.AddListener(() =>
            {
                if (!TryTakeAction()) return;
                UseItemOnAlly(capturedMember);
            });
        }
    }

    void UseItemOnAlly(CharacterInstance member)
    {
        if (pendingItem == null) return;

        string userName = GetCurrentTurnMemberName();
        string logMsg = "";

        if (pendingItem.itemData.itemType == ItemType.Heal)
        {
            int totalHeal = pendingItem.itemData.flatHeal +
                Mathf.RoundToInt(member.MaxHP * pendingItem.itemData.percentHeal);
            totalHeal = Mathf.Max(0, totalHeal);
            int before = member.currentHP;
            member.currentHP = Mathf.Min(member.MaxHP, member.currentHP + totalHeal);
            int actual = member.currentHP - before;
            logMsg = $"{userName} uses {pendingItem.itemData.itemName} on {member.Name}! Recovered {actual} HP!";
            Debug.Log($"[HEAL COMBAT] {member.Name}: +{actual} HP → {member.currentHP}/{member.MaxHP}");
        }
        else if (pendingItem.itemData.itemType == ItemType.Buff)
        {
            member.statModifiers.Add(new StatModifier(
                pendingItem.itemData.statType,
                pendingItem.itemData.statModifier,
                pendingItem.itemData.modifierDuration));
            logMsg = $"{userName} uses {pendingItem.itemData.itemName} on {member.Name}! " +
                $"{pendingItem.itemData.statType} +{pendingItem.itemData.statModifier} " +
                $"for {pendingItem.itemData.modifierDuration} turns!";
        }

        InventoryManager.Instance.RemoveItem(pendingItem.itemData);
        pendingItem = null;
        memberSelectPopup.SetActive(false);

        // Force update HP display
        var partyList = TurnCombatManager.Instance.party;
        var enemyList = TurnCombatManager.Instance.enemies;

        // Refresh combatant HP from instance
        foreach (var c in partyList) c.Refresh();

        UpdateAllHP(partyList, enemyList);
        TurnCombatManager.Instance.UpdateStatusIndicatorsPublic();

        ShowCombatLog(logMsg, () =>
        {
            combatLogText.text = "";
            TurnCombatManager.Instance.NextTurnPublic();
        });
    }

    void UseItemOnEnemy(InventoryItem item)
    {
        var target = TurnCombatManager.Instance.enemies.Find(e => e.IsAlive);
        if (target == null) return;

        string userName = GetCurrentTurnMemberName();
        string logMsg = $"{userName} uses {item.itemData.itemName} on {target.Name}!";

        if (item.itemData.statModifier != 0)
        {
            int mod = item.itemData.itemType == ItemType.Debuff
                ? -Mathf.Abs(item.itemData.statModifier)
                : item.itemData.statModifier;

            var enemyInst = TurnCombatManager.Instance.GetEnemyInstance(target.Name);
            if (enemyInst != null)
            {
                enemyInst.statModifiers.Add(new StatModifier(
                    item.itemData.statType, mod, item.itemData.modifierDuration));
                target.Refresh();
                logMsg += $" {item.itemData.statType} {(mod > 0 ? "+" : "")}{mod} " +
                    $"for {item.itemData.modifierDuration} turns!";
                Debug.Log($"[DEBUFF] {target.Name}: {item.itemData.statType} {mod} for {item.itemData.modifierDuration} turns");
            }
        }

        if (item.itemData.statusEffect != StatusEffectType.None)
        {
            target.ApplyStatusEffect(item.itemData.statusEffect,
                item.itemData.statusChance, item.itemData.statusDuration,
                item.itemData.dotPercent, 0f, 0);
            if (target.HasStatusEffect(item.itemData.statusEffect))
                logMsg += $" {item.itemData.statusEffect} applied!";
        }

        InventoryManager.Instance.RemoveItem(item.itemData);
        UpdateAllHP(TurnCombatManager.Instance.party, TurnCombatManager.Instance.enemies);
        TurnCombatManager.Instance.UpdateStatusIndicatorsPublic();

        ShowCombatLog(logMsg, () =>
        {
            combatLogText.text = "";
            TurnCombatManager.Instance.NextTurnPublic();
        });
    }

    string GetCurrentTurnMemberName()
    {
        var inst = TurnCombatManager.Instance;
        if (inst == null || inst.party.Count == 0) return "Hero";
        int idx = inst.CurrentTurnIndex;
        if (idx < inst.turnOrder.Count && !inst.turnOrder[idx].IsEnemy)
            return inst.turnOrder[idx].Name;
        return inst.party[0].Name;
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
            SetupHPEntry(entry, member);
        }
    }

    void SetupHPEntry(GameObject entry, Combatant member)
    {
        // Search recursively through all children
        var nameText = FindDeep<TextMeshProUGUI>(entry, "NameText");
        var portrait = FindDeep<Image>(entry, "Portrait");
        var hpBar = FindDeep<Image>(entry, "HPBar");
        var hpText = FindDeep<TextMeshProUGUI>(entry, "HPText");
        var mpBar = FindDeep<Image>(entry, "MPBar");
        var mpText = FindDeep<TextMeshProUGUI>(entry, "MPText");

        if (nameText != null)
        {
            string indicator = "";
            if (member.IsBlocking) indicator = " B!";
            else if (member.CombatStyle == CombatStyle.Evade && member.IsEvading) indicator = " E!";
            nameText.text = member.Name + indicator;
            nameText.color = member.IsAlive ? Color.white : Color.red;
        }

        if (portrait != null)
        {
            var so = PartyManager.Instance.allMembers
                .Find(m => m.Name == member.Name)?.baseData;
            if (so != null)
                portrait.sprite = so.headPortrait != null ? so.headPortrait : so.portrait;
        }

        if (hpBar != null)
        {
            float hpFill = member.MaxHP > 0 ? (float)member.CurrentHP / member.MaxHP : 0f;
            hpBar.fillAmount = Mathf.Clamp01(hpFill);
            hpBar.color = hpFill > 0.5f ? Color.red :
                          hpFill > 0.25f ? new Color(1f, 0.5f, 0f) :
                          new Color(0.8f, 0f, 0f);
            Debug.Log($"[HP BAR] {member.Name}: {member.CurrentHP}/{member.MaxHP} fill:{hpFill}");
        }

        if (hpText != null)
            hpText.text = $"{member.CurrentHP}/{member.MaxHP}";

        if (mpBar != null)
        {
            var memberData = PartyManager.Instance.allMembers.Find(m => m.Name == member.Name);
            int maxMP = memberData?.MaxMana ?? 1;
            float mpFill = maxMP > 0 ? (float)member.GetCurrentMana() / maxMP : 0f;
            mpBar.fillAmount = Mathf.Clamp01(mpFill);
            Debug.Log($"[MP BAR] {member.Name}: {member.GetCurrentMana()}/{maxMP} fill:{mpFill}");
        }

        if (mpText != null)
            mpText.text = $"{member.GetCurrentMana()}";
    }

    T FindDeep<T>(GameObject root, string name) where T : Component
    {
        foreach (Transform child in root.GetComponentsInChildren<Transform>())
            if (child.name == name)
            {
                var comp = child.GetComponent<T>();
                if (comp != null) return comp;
            }
        return null;
    }

    public void BuildEnemyTargetButtons(List<Combatant> enemies)
    {
        CombatSpriteManager.Instance?.UpdateEnemyLabels(enemies);
    }

    public void HighlightSelectedEnemy(int index)
    {
        highlightedIndex = index;
        CombatSpriteManager.Instance?.HighlightSelectedEnemy(index);
    }

    public void SetPlayerButtonsActive(bool active, CombatStyle style = CombatStyle.Block)
    {
        if (active) ResetActionTaken();

        basicAttackButton.interactable = active;
        skillsButton.interactable = active;
        itemsButton.interactable = active;
        blockButton.interactable = active;

        if (active)
        {
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
        }

        if (!active) CloseAllPanels();
    }

    public void ShowVictory(int xp, int gold, DropResult drops)
    {
        if (EncounterManager.ActiveCutscene != null)
        {
            EncounterManager.ActiveCutscene.OnBattleComplete();
            EncounterManager.ActiveCutscene = null;
        }

        victoryPanel.SetActive(true);
        victoryPanel.transform.SetAsLastSibling();

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
        victoryPanel.transform.SetAsLastSibling();
        victoryXPText.text = "Your party has been defeated...";
        StartCoroutine(ReturnAfterDelay(3f));
    }

    IEnumerator ReturnAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);
        SceneManager.LoadScene(overworldScene);
    }
}