using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.SceneManagement;
using System.Collections;
using System.Collections.Generic;

public class GameOverManager : MonoBehaviour
{
    public static GameOverManager Instance;

    [Header("UI")]
    public GameObject gameOverPanel;
    public TextMeshProUGUI gameOverTitleText;
    public Button retryButton;
    public Button loadLastSaveButton;
    public Button mainMenuButton;

    [Header("Scene")]
    public string mainMenuScene = "MainMenu";

    [Header("Timing")]
    public float minimumDisplayTime = 1.5f; // game over text stays up at least this long before buttons can be used

    private static List<CombatSnapshot> partySnapshot = new();
    private static List<InventoryItemSave> inventorySnapshot = new();
    private static int goldSnapshot = 0;
    private bool buttonsEnabled = false;

    void Awake()
    {
        if (Instance == null) Instance = this;
        else { Destroy(gameObject); return; }

        if (gameOverPanel != null) gameOverPanel.SetActive(false);
    }

    void Start()
    {
        retryButton?.onClick.AddListener(OnRetry);
        loadLastSaveButton?.onClick.AddListener(OnLoadLastSave);
        mainMenuButton?.onClick.AddListener(OnMainMenu);
    }

    public static void SnapshotBeforeBattle()
    {
        partySnapshot.Clear();
        inventorySnapshot.Clear();

        if (PartyManager.Instance != null)
        {
            foreach (var member in PartyManager.Instance.activeParty)
            {
                partySnapshot.Add(new CombatSnapshot
                {
                    characterName = member.Name,
                    hp = member.currentHP,
                    mana = member.currentMana
                });
            }
        }

        if (InventoryManager.Instance != null)
        {
            foreach (var item in InventoryManager.Instance.items)
            {
                inventorySnapshot.Add(new InventoryItemSave
                {
                    itemSOName = item.itemData.itemName,
                    quantity = item.quantity
                });
            }
        }

        goldSnapshot = GoldManager.Instance != null ? GoldManager.Instance.gold : 0;

        Debug.Log($"[GAME OVER] Snapshot: {partySnapshot.Count} members, " +
            $"{inventorySnapshot.Count} item stacks, Gold:{goldSnapshot}");
    }

    public void ShowGameOver()
    {
        Debug.Log("[GAME OVER] Showing screen");
        gameOverPanel.SetActive(true);
        gameOverPanel.transform.SetAsLastSibling();
        gameOverTitleText.text = "Your party has fallen...";

        // FIXED: disable buttons briefly so the screen doesn't feel
        // instantly clickable/jarring the moment it appears
        buttonsEnabled = false;
        retryButton.interactable = false;
        loadLastSaveButton.interactable = false;
        mainMenuButton.interactable = false;

        Time.timeScale = 0f;
        StartCoroutine(EnableButtonsAfterDelay());
    }

    IEnumerator EnableButtonsAfterDelay()
    {
        // Use unscaled time since Time.timeScale is 0
        yield return new WaitForSecondsRealtime(minimumDisplayTime);

        buttonsEnabled = true;
        retryButton.interactable = true;
        mainMenuButton.interactable = true;

        bool hasAnySave = SaveManager.Instance.SlotExists(SaveManager.AutoSaveSlot);
        for (int i = 0; i < SaveManager.MaxSlots && !hasAnySave; i++)
            if (SaveManager.Instance.SlotExists(i)) hasAnySave = true;

        loadLastSaveButton.interactable = hasAnySave;
    }

    void OnRetry()
    {
        if (!buttonsEnabled) return;
        Debug.Log("[GAME OVER] Retry pressed - restarting battle in place");
        Time.timeScale = 1f;
        gameOverPanel.SetActive(false);

        if (PartyManager.Instance != null)
        {
            foreach (var member in PartyManager.Instance.activeParty)
            {
                var snap = partySnapshot.Find(s => s.characterName == member.Name);
                if (snap != null)
                {
                    member.currentHP = Mathf.Max(1, Mathf.RoundToInt(snap.hp * 0.5f));
                    member.currentMana = snap.mana;
                }
                else
                {
                    member.currentHP = Mathf.Max(1, member.MaxHP / 2);
                    member.currentMana = member.MaxMana;
                }

                // FIXED: clear lingering status effects/stat debuffs from the
                // failed attempt so base stats (like Speed) are accurate again
                member.activeEffects.Clear();
                member.statModifiers.Clear();
            }
        }

        if (InventoryManager.Instance != null && inventorySnapshot.Count > 0)
        {
            InventoryManager.Instance.items.Clear();
            foreach (var itemSave in inventorySnapshot)
            {
                var so = SaveManager.Instance.FindItemSO(itemSave.itemSOName);
                if (so == null) continue;
                InventoryManager.Instance.items.Add(new InventoryItem(so, itemSave.quantity));
            }
        }

        if (GoldManager.Instance != null)
            GoldManager.Instance.SetGold(goldSnapshot);

        if (TurnCombatManager.Instance != null)
        {
            Debug.Log("[GAME OVER] Restarting TurnCombatManager.SetupCombat()");
            TurnCombatManager.Instance.RestartCombat();
        }
        else
        {
            Debug.LogError("[GAME OVER] TurnCombatManager.Instance is null - cannot retry in place!");
        }
    }

    void OnLoadLastSave()
    {
        if (!buttonsEnabled) return;
        Debug.Log("[GAME OVER] Loading most recent save");
        Time.timeScale = 1f;
        gameOverPanel.SetActive(false);

        EncounterManager.CurrentEnemies.Clear();
        EncounterManager.ActiveCutscene = null;
        EncounterManager.ActiveRecruitCutscene = null;

        int mostRecentSlot = FindMostRecentSlot();
        if (mostRecentSlot != int.MinValue)
            SaveManager.Instance.LoadFromSlot(mostRecentSlot);
        else
            Debug.LogWarning("[GAME OVER] No save found!");
    }

    int FindMostRecentSlot()
    {
        int bestSlot = int.MinValue;
        System.DateTime bestTime = System.DateTime.MinValue;

        for (int i = SaveManager.AutoSaveSlot; i < SaveManager.MaxSlots; i++)
        {
            var preview = SaveManager.Instance.LoadSlotPreview(i);
            if (preview == null || preview.isEmpty) continue;
            if (System.DateTime.TryParse(preview.dateTime, out var parsed))
            {
                if (parsed > bestTime)
                {
                    bestTime = parsed;
                    bestSlot = i;
                }
            }
        }

        Debug.Log($"[GAME OVER] Most recent slot: {bestSlot}");
        return bestSlot;
    }

    void OnMainMenu()
    {
        if (!buttonsEnabled) return;
        Debug.Log("[GAME OVER] Going to main menu - clearing all encounter state");
        Time.timeScale = 1f;
        gameOverPanel.SetActive(false);

        EncounterManager.CurrentEnemies.Clear();
        EncounterManager.ActiveCutscene = null;
        EncounterManager.ActiveRecruitCutscene = null;
        EncounterManager.IsResonanceBattle = false;
        EncounterManager.IsForcedLossBattle = false;
        EncounterManager.IsRecruitBattle = false;
        EncounterManager.PendingRecruitCompletion = false;
        EncounterManager.PendingRecruitMemberName = "";
        EncounterManager.PlayerReturnPosition = Vector3.zero;

        SceneManager.LoadScene(mainMenuScene);
    }
}

[System.Serializable]
public class CombatSnapshot
{
    public string characterName;
    public int hp;
    public int mana;
}