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
    public float fadeInDuration = 0.4f;
    public float minimumDisplayTime = 1.5f;

    private CanvasGroup panelCanvasGroup;
    private Image blackOverlay;
    private static List<CombatSnapshot> partySnapshot = new();
    private static List<InventoryItemSave> inventorySnapshot = new();
    private static int goldSnapshot = 0;
    private bool buttonsEnabled = false;

    void Awake()
    {
        if (Instance == null) Instance = this;
        else { Destroy(gameObject); return; }

        if (gameOverPanel != null)
        {
            gameOverPanel.SetActive(false);

            panelCanvasGroup = gameOverPanel.GetComponent<CanvasGroup>();
            if (panelCanvasGroup == null)
                panelCanvasGroup = gameOverPanel.AddComponent<CanvasGroup>();
        }

        // Create a permanent black overlay for scene transitions
        // so the battle is never visible when switching scenes
        CreateBlackOverlay();
    }

    void CreateBlackOverlay()
    {
        var canvas = FindFirstObjectByType<Canvas>();
        if (canvas == null) return;

        var obj = new GameObject("GameOverBlackOverlay");
        obj.transform.SetParent(canvas.transform, false);

        blackOverlay = obj.AddComponent<Image>();
        blackOverlay.color = new Color(0, 0, 0, 0);
        blackOverlay.raycastTarget = false;

        var rt = obj.GetComponent<RectTransform>();
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;

        obj.transform.SetAsLastSibling();
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
        Debug.Log($"[GAME OVER] Snapshot: {partySnapshot.Count} members, {inventorySnapshot.Count} items");
    }

    public void ShowGameOver()
    {
        Debug.Log("[GAME OVER] Showing screen");

        // Make sure black overlay is behind panel
        if (blackOverlay != null)
        {
            blackOverlay.color = new Color(0, 0, 0, 0);
            blackOverlay.raycastTarget = false;
            blackOverlay.transform.SetAsLastSibling();
        }

        gameOverPanel.SetActive(true);
        gameOverPanel.transform.SetAsLastSibling();
        gameOverTitleText.text = "Your party has fallen...";

        panelCanvasGroup.alpha = 0f;
        buttonsEnabled = false;
        retryButton.interactable = false;
        loadLastSaveButton.interactable = false;
        mainMenuButton.interactable = false;

        Time.timeScale = 0f;
        StartCoroutine(FadeInPanel());
    }

    IEnumerator FadeInPanel()
    {
        float t = 0f;
        while (t < fadeInDuration)
        {
            t += Time.unscaledDeltaTime;
            panelCanvasGroup.alpha = Mathf.Clamp01(t / fadeInDuration);
            yield return null;
        }
        panelCanvasGroup.alpha = 1f;

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
        Debug.Log("[GAME OVER] Retry");
        StartCoroutine(RetrySequence());
    }

    IEnumerator RetrySequence()
    {
        // Fade out the game over panel
        float t = 0f;
        while (t < 0.3f)
        {
            t += Time.unscaledDeltaTime;
            panelCanvasGroup.alpha = Mathf.Clamp01(1f - t / 0.3f);
            yield return null;
        }
        panelCanvasGroup.alpha = 0f;
        gameOverPanel.SetActive(false);

        Time.timeScale = 1f;

        // Restore pre-battle state
        RestorePreBattleState();

        // TurnCombatManager handles its own fade-in via RetryWithFadeIn coroutine
        if (TurnCombatManager.Instance != null)
            TurnCombatManager.Instance.RestartCombat();
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

    void OnLoadLastSave()
    {
        if (!buttonsEnabled) return;
        Debug.Log("[GAME OVER] Load last save");
        StartCoroutine(BlackScreenThenAction(() =>
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
        StartCoroutine(BlackScreenThenAction(() =>
        {
            Time.timeScale = 1f;
            ClearAllState();
            SceneManager.LoadScene(mainMenuScene);
        }));
    }

    // Fade game over panel out, fade black overlay IN, THEN do action
    // This ensures the battle is NEVER visible during scene transitions
    IEnumerator BlackScreenThenAction(System.Action action)
    {
        // First fade out the game over panel
        float t = 0f;
        while (t < 0.3f)
        {
            t += Time.unscaledDeltaTime;
            if (panelCanvasGroup != null)
                panelCanvasGroup.alpha = Mathf.Clamp01(1f - t / 0.3f);
            yield return null;
        }
        if (panelCanvasGroup != null) panelCanvasGroup.alpha = 0f;
        if (gameOverPanel != null) gameOverPanel.SetActive(false);

        // Fade in black overlay to cover the battle scene
        if (blackOverlay != null)
        {
            blackOverlay.raycastTarget = true;
            t = 0f;
            while (t < 0.3f)
            {
                t += Time.unscaledDeltaTime;
                blackOverlay.color = new Color(0, 0, 0, Mathf.Clamp01(t / 0.3f));
                yield return null;
            }
            blackOverlay.color = Color.black;
        }

        // Small pause so black fully covers before scene load
        yield return new WaitForSecondsRealtime(0.1f);

        // NOW execute the scene change / load
        action?.Invoke();
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