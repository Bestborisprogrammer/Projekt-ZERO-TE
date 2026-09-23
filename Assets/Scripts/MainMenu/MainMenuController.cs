using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using TMPro;

public class MainMenuController : MonoBehaviour
{
    private enum State { Intro, Transition, Menu }

    [Header("Screens")]
    public GameObject introScreen;
    public GameObject mainMenuScreen;

    [Header("Backgrounds")]
    public RectTransform introBackground;
    public RectTransform menuBackground;

    [Header("UI")]
    public CanvasGroup logoGroup;
    public RectTransform buttonPanel;
    public TextMeshProUGUI continueText;

    [Header("Flash")]
    public Image flashImage;

    [Header("Transition overlay - fullscreen black Image in Canvas")]
    public Image transitionOverlay;
    public float transitionDuration = 0.5f;

    [Header("Save Panels")]
    public GameObject loadGamePanel;   // existing save slot panel, isLoadOnlyMode = true
    public Button continueButton;      // picks most recent save automatically
    public Button loadGameButton;      // opens all slots
    public Button newGameButton;

    [Header("Scene")]
    public string gameScene = "overworldScene";

    private State state = State.Intro;
    private bool transitioning = false;
    private Vector3 introBaseScale;
    private Vector3 menuBaseScale;
    private Vector2 buttonPanelTargetPos;

    void Start()
    {
        introBaseScale = introBackground.localScale;
        menuBaseScale = menuBackground.localScale;

        introScreen.SetActive(true);
        mainMenuScreen.SetActive(false);
        logoGroup.alpha = 0f;

        buttonPanelTargetPos = buttonPanel.anchoredPosition;
        buttonPanel.anchoredPosition = new Vector2(-700f, buttonPanelTargetPos.y);

        SetFlash(0f);
        StartCoroutine(PulseText());

        if (loadGamePanel != null)
            loadGamePanel.SetActive(false);

        if (transitionOverlay != null)
        {
            transitionOverlay.color = new Color(0, 0, 0, 0);
            transitionOverlay.gameObject.SetActive(false);
        }

        // Disable Continue if no saves exist
        bool hasSave = HasAnySave();
        if (continueButton != null) continueButton.interactable = hasSave;
        if (loadGameButton != null) loadGameButton.interactable = hasSave;
    }

    bool HasAnySave()
    {
        if (SaveManager.Instance == null) return false;
        if (SaveManager.Instance.SlotExists(SaveManager.AutoSaveSlot)) return true;
        for (int i = 0; i < SaveManager.MaxSlots; i++)
            if (SaveManager.Instance.SlotExists(i)) return true;
        return false;
    }

    void Update()
    {
        if (state == State.Intro && !transitioning)
            if (Input.anyKeyDown)
                StartCoroutine(Transition());
    }

    // ── BUTTONS ──────────────────────────────────────────────────────────────

    public void StartGame()
    {
        Debug.Log("[MAINMENU] New Game");
        StartCoroutine(TransitionThenAction(() =>
        {
            SaveManager.ForceResetLoadingState();
            if (SaveManager.Instance != null)
            {
                SaveManager.Instance.currentSlot = -2;
                SaveManager.Instance.sessionPlaytime = 0f;
            }

            if (PartyManager.Instance != null)
                PartyManager.Instance.ResetForNewGame();

            if (EncounterManager.CurrentEnemies != null)
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

            PlayerPrefs.DeleteAll();
            TrackedPlayerPrefsKeys.ResetMasterList();
            PlayerPrefs.SetInt("session_initialized_flag", 1);
            PlayerPrefs.Save();

            GearMenuPanel.ResetInitialized();

            Debug.Log("[MAINMENU] All state cleared, loading game scene");
            SceneManager.LoadScene(gameScene);
        }));
    }

    // Continue: automatically loads the most recent save
    public void ContinueMostRecent()
    {
        int slot = FindMostRecentSlot();
        if (slot == int.MinValue)
        {
            Debug.LogWarning("[MAINMENU] Continue pressed but no save found");
            return;
        }

        Debug.Log($"[MAINMENU] Continue - loading most recent slot: {slot}");
        StartCoroutine(TransitionThenAction(() =>
        {
            SaveManager.Instance.LoadFromSlot(slot);
        }));
    }

    // Load Game: opens the slot selection panel
    public void OpenLoadGamePanel()
    {
        if (loadGamePanel != null)
            loadGamePanel.SetActive(true);
    }

    public void CloseLoadGamePanel()
    {
        if (loadGamePanel != null)
            loadGamePanel.SetActive(false);
    }

    // Called by SaveMenuPanel when player picks a slot in load-only mode
    // (wire this up on the SaveMenuPanel itself to call LoadWithTransition)
    public void LoadSlotWithTransition(int slot)
    {
        StartCoroutine(TransitionThenAction(() =>
        {
            SaveManager.Instance.LoadFromSlot(slot);
        }));
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

    public void QuitGame()
    {
        Application.Quit();
    }

    // ── TRANSITION ────────────────────────────────────────────────────────────

    IEnumerator TransitionThenAction(System.Action action)
    {
        if (transitionOverlay != null)
        {
            transitionOverlay.gameObject.SetActive(true);
            float t = 0f;
            while (t < transitionDuration)
            {
                t += Time.deltaTime;
                transitionOverlay.color = new Color(0, 0, 0, Mathf.Clamp01(t / transitionDuration));
                yield return null;
            }
            transitionOverlay.color = Color.black;
        }
        else yield return new WaitForSeconds(0.1f);

        action?.Invoke();
    }

    // ── CINEMATIC INTRO ───────────────────────────────────────────────────────

    IEnumerator Transition()
    {
        transitioning = true;
        state = State.Transition;
        float t = 0f;
        Vector3 introStart = introBackground.localScale;
        Vector3 introEnd = introBaseScale * 1.20f;
        bool reachedPeak = false;

        while (t < 1f)
        {
            t += Time.deltaTime / 2.2f;
            float s = Mathf.SmoothStep(0f, 1f, t);
            introBackground.localScale = Vector3.Lerp(introStart, introEnd, s);
            float flash = Mathf.Pow(s, 2.5f);
            SetFlash(flash);

            if (flash >= 0.98f && !reachedPeak)
            {
                reachedPeak = true;
                introScreen.SetActive(false);
                mainMenuScreen.SetActive(true);
                menuBackground.localScale = menuBaseScale * 1.12f;
            }
            yield return null;
        }

        SetFlash(1f);
        yield return new WaitForSeconds(0.6f);

        t = 0f;
        Vector3 menuStart = menuBackground.localScale;
        Vector3 menuEnd = menuBaseScale;
        while (t < 1f)
        {
            t += Time.deltaTime / 2.0f;
            float s = Mathf.SmoothStep(0f, 1f, t);
            SetFlash(1f - Mathf.Pow(s, 2f));
            menuBackground.localScale = Vector3.Lerp(menuStart, menuEnd, s);
            yield return null;
        }

        SetFlash(0f);
        state = State.Menu;
        transitioning = false;
        StartCoroutine(AnimateMenu());
    }

    IEnumerator AnimateMenu()
    {
        float t = 0f;
        while (t < 1f)
        {
            t += Time.deltaTime * 1.3f;
            logoGroup.alpha = t;
            yield return null;
        }
        logoGroup.alpha = 1f;

        Vector2 start = new Vector2(-700f, buttonPanelTargetPos.y);
        Vector2 end = buttonPanelTargetPos;
        t = 0f;
        while (t < 1f)
        {
            t += Time.deltaTime * 1.4f;
            buttonPanel.anchoredPosition = Vector2.Lerp(start, end, Mathf.SmoothStep(0f, 1f, t));
            yield return null;
        }
        buttonPanel.anchoredPosition = end;
    }

    IEnumerator PulseText()
    {
        while (state == State.Intro)
        {
            float a = Mathf.PingPong(Time.time * 1.1f, 1f);
            Color c = continueText.color;
            c.a = a;
            continueText.color = c;
            yield return null;
        }
    }

    void SetFlash(float a)
    {
        if (flashImage == null) return;
        Color c = flashImage.color;
        c.a = a;
        flashImage.color = c;
    }

    public void OpenContinueMenu() => OpenLoadGamePanel();
    public void CloseContinueMenu() => CloseLoadGamePanel();
}