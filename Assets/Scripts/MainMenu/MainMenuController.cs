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

    [Header("Save/Continue")]
    public GameObject continueSavePanel; // SaveMenuPanel in isLoadOnlyMode
    public Button continueButton;

    [Header("Scene")]
    public string gameScene = "OverworldScene";

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

        if (continueSavePanel != null)
            continueSavePanel.SetActive(false);

        // Disable continue button if no saves exist at all
        if (continueButton != null)
            continueButton.interactable = HasAnySave();
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
        {
            if (Input.anyKeyDown)
                StartCoroutine(Transition());
        }
    }

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
            buttonPanel.anchoredPosition =
                Vector2.Lerp(start, end, Mathf.SmoothStep(0f, 1f, t));
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

    // ── BUTTONS ───────────────────────────────────
    public void StartGame()
    {
        Debug.Log("[MAINMENU] New Game started - performing full reset");

        SaveManager.Instance.currentSlot = -2;
        SaveManager.Instance.sessionPlaytime = 0f;

        // Force a full fresh-game wipe regardless of session flag,
        // since this is an explicit "New Game" request
        PlayerPrefs.DeleteAll();
        PlayerPrefs.SetInt("session_initialized_flag", 1);
        PlayerPrefs.Save();

        GearMenuPanel.ResetInitialized();

        SceneManager.LoadScene(gameScene);
    }

    public void OpenContinueMenu()
    {
        if (continueSavePanel == null) return;
        Debug.Log("[MAINMENU] Opening continue/load menu");
        continueSavePanel.SetActive(true);
    }

    public void CloseContinueMenu()
    {
        if (continueSavePanel == null) return;
        continueSavePanel.SetActive(false);
    }

    public void QuitGame()
    {
        Application.Quit();
    }
}