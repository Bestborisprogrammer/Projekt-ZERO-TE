using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.SceneManagement;
using System.Collections;
using System.Collections.Generic;

public class GameOverManager : MonoBehaviour
{
    public static GameOverManager Instance;

    [Header("UI - assign in Inspector")]
    public GameObject gameOverPanel;
    public CanvasGroup panelCanvasGroup;
    public Image blackOverlay;
    public TextMeshProUGUI gameOverTitleText;
    public Button retryButton;
    public Button loadLastSaveButton;
    public Button mainMenuButton;

    [Header("Scene")]
    public string mainMenuScene = "MainMenu";

    [Header("Timing")]
    public float fadeInDuration = 0.4f;
    public float minimumDisplayTime = 1.2f;

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
            foreach (var member in PartyManager.Instance.activeParty)
                partySnapshot.Add(new CombatSnapshot
                {
                    characterName = member.Name,
                    hp = member.currentHP,
                    mana = member.currentMana
                });

        if (InventoryManager.Instance != null)
            foreach (var item in InventoryManager.Instance.items)
                inventorySnapshot.Add(new InventoryItemSave
                {
                    itemSOName = item.itemData.itemName,
                    quantity = item.quantity
                });

        goldSnapshot = GoldManager.Instance != null ? GoldManager.Instance.gold : 0;
        Debug.Log($"[GAME OVER] Snapshot: {partySnapshot.Count} members");
    }

    public void ShowGameOver()
    {
        Debug.Log("[GAME OVER] Showing");
        StopAllCoroutines();

        // Black overlay fully opaque immediately so battle is NEVER visible
        gameOverPanel.SetActive(true);
        gameOverPanel.transform.SetAsLastSibling();

        if (blackOverlay != null)
            blackOverlay.color = Color.black;

        if (panelCanvasGroup != null)
            panelCanvasGroup.alpha = 0f;

        buttonsEnabled = false;
        SetButtonsInteractable(false);

        Time.timeScale = 0f;
        StartCoroutine(GameOverSequence());
    }

    IEnumerator GameOverSequence()
    {
        yield return new WaitForSecondsRealtime(0.4f);

        // Fade black away revealing the game over text/buttons
        float t = 0f;
        while (t < fadeInDuration)
        {
            t += Time.unscaledDeltaTime;
            float p = Mathf.Clamp01(t / fadeInDuration);
            if (panelCanvasGroup != null) panelCanvasGroup.alpha = p;
            if (blackOverlay != null) blackOverlay.color = new Color(0, 0, 0, 1f - p);
            yield return null;
        }
        if (panelCanvasGroup != null) panelCanvasGroup.alpha = 1f;
        if (blackOverlay != null) blackOverlay.color = new Color(0, 0, 0, 0f);

        yield return new WaitForSecondsRealtime(minimumDisplayTime);

        bool hasAnySave = SaveManager.Instance.SlotExists(SaveManager.AutoSaveSlot);
        for (int i = 0; i < SaveManager.MaxSlots && !hasAnySave; i++)
            if (SaveManager.Instance.SlotExists(i)) hasAnySave = true;

        buttonsEnabled = true;
        retryButton.interactable = true;
        loadLastSaveButton.interactable = hasAnySave;
        mainMenuButton.interactable = true;
    }

    void SetButtonsInteractable(bool v)
    {
        if (retryButton) retryButton.interactable = v;
        if (loadLastSaveButton) loadLastSaveButton.interactable = v;
        if (mainMenuButton) mainMenuButton.interactable = v;
    }

    void OnRetry()
    {
        if (!buttonsEnabled) return;
        Debug.Log("[GAME OVER] Retry");
        StartCoroutine(RetrySequence());
    }

    IEnumerator RetrySequence()
    {
        // Fade game over content out, fade black back in
        float t = 0f;
        while (t < 0.3f)
        {
            t += Time.unscaledDeltaTime;
            float p = Mathf.Clamp01(t / 0.3f);
            if (panelCanvasGroup != null) panelCanvasGroup.alpha = 1f - p;
            if (blackOverlay != null) blackOverlay.color = new Color(0, 0, 0, p);
            yield return null;
        }
        if (blackOverlay != null) blackOverlay.color = Color.black;
        if (panelCanvasGroup != null) panelCanvasGroup.alpha = 0f;
        gameOverPanel.SetActive(false);

        Time.timeScale = 1f;

        // Restore pre-battle state FIRST
        RestorePreBattleState();

        // Then build combat behind the black screen
        if (TurnCombatManager.Instance != null)
        {
            Debug.Log("[GAME OVER] Calling RestartCombat");
            TurnCombatManager.Instance.RestartCombat();
        }

        // FadeInAfterRestart in TurnCombatManager handles revealing the new battle
    }

    IEnumerator FadeToBlackThenDo(System.Action action)
    {
        // Fade panel content out + black in simultaneously
        float t = 0f;
        while (t < 0.35f)
        {
            t += Time.unscaledDeltaTime;
            float p = Mathf.Clamp01(t / 0.35f);
            if (panelCanvasGroup != null) panelCanvasGroup.alpha = 1f - p;
            if (blackOverlay != null) blackOverlay.color = new Color(0, 0, 0, p);
            yield return null;
        }
        if (blackOverlay != null) blackOverlay.color = Color.black;
        if (panelCanvasGroup != null) panelCanvasGroup.alpha = 0f;
        gameOverPanel.SetActive(false);

        yield return new WaitForSecondsRealtime(0.1f);
        action?.Invoke();
    }

    void OnLoadLastSave()
    {
        if (!buttonsEnabled) return;
        Debug.Log("[GAME OVER] Load last save");
        StartCoroutine(FadeToBlackThenDo(() =>
        {
            Time.timeScale = 1f;
            ClearAllState();
            int slot = FindMostRecentSlot();
            if (slot != int.MinValue)
                SaveManager.Instance.LoadFromSlot(slot);
        }));
    }

    void OnMainMenu()
    {
        if (!buttonsEnabled) return;
        Debug.Log("[GAME OVER] Main menu");

        // Keep black overlay fully opaque, disable panel content, load immediately
        // No fade-out sequence needed since we're leaving this scene entirely
        if (panelCanvasGroup != null) panelCanvasGroup.alpha = 0f;
        if (blackOverlay != null) blackOverlay.color = Color.black;
        if (gameOverPanel != null) gameOverPanel.SetActive(false);

        Time.timeScale = 1f;
        ClearAllState();
        SceneManager.LoadScene(mainMenuScene);
    }

    void RestorePreBattleState()
    {
        if (PartyManager.Instance != null)
        {
            foreach (var member in PartyManager.Instance.activeParty)
            {
                var snap = partySnapshot.Find(s => s.characterName == member.Name);
                if (snap != null)
                {
                    member.currentHP = snap.hp;
                    member.currentMana = snap.mana;
                }
                else
                {
                    member.currentHP = member.MaxHP;
                    member.currentMana = member.MaxMana;
                }
                member.isBlocking = false;
                member.isEvading = false;
                member.activeEffects.Clear();
                member.statModifiers.Clear();
                Debug.Log($"[GAME OVER] Restored {member.Name}: HP:{member.currentHP} Mana:{member.currentMana}");
            }
        }

        if (InventoryManager.Instance != null && inventorySnapshot.Count > 0)
        {
            InventoryManager.Instance.items.Clear();
            foreach (var s in inventorySnapshot)
            {
                var so = SaveManager.Instance.FindItemSO(s.itemSOName);
                if (so == null) continue;
                InventoryManager.Instance.items.Add(new InventoryItem(so, s.quantity));
            }
        }

        if (GoldManager.Instance != null)
            GoldManager.Instance.SetGold(goldSnapshot);
    }

    void ClearAllState()
    {
        EncounterManager.CurrentEnemies.Clear();
        EncounterManager.ActiveCutscene = null;
        EncounterManager.ActiveRecruitCutscene = null;
        EncounterManager.IsResonanceBattle = false;
        EncounterManager.IsForcedLossBattle = false;
        EncounterManager.IsRecruitBattle = false;
        EncounterManager.PendingRecruitCompletion = false;
        EncounterManager.PendingRecruitMemberName = "";
        EncounterManager.PlayerReturnPosition = Vector3.zero;
        ResonanceCutsceneManager.WaitingForResonanceBattleReturn = false;
        ResonanceCutsceneManager.WaitingForDuelReturn = false;
        PlayerMovement2D.ForceFrozen = false;
        SaveManager.ForceResetLoadingState();
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
                if (parsed > bestTime) { bestTime = parsed; bestSlot = i; }
        }
        return bestSlot;
    }
}

[System.Serializable]
public class CombatSnapshot
{
    public string characterName;
    public int hp;
    public int mana;
}