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
    }

    void Update()
    {
        if (state == State.Intro && !transitioning)
        {
            if (Input.anyKeyDown)
            {
                StartCoroutine(Transition());
            }
        }
    }

    // ================= CINEMATIC TRANSITION (FIXED TIMING) =================

    IEnumerator Transition()
    {
        transitioning = true;
        state = State.Transition;

        float t = 0f;

        Vector3 introStart = introBackground.localScale;
        Vector3 introEnd = introBaseScale * 1.20f;

       
        bool reachedPeak = false;

        // ================= PHASE 1: SLOW BUILD =================
        while (t < 1f)
        {
            t += Time.deltaTime / 2.2f; // 🔥 MUCH slower now

            float s = Mathf.SmoothStep(0f, 1f, t);

            // slow zoom in
            introBackground.localScale = Vector3.Lerp(introStart, introEnd, s);

            // flash grows slowly
            float flash = Mathf.Pow(s, 2.5f);
            SetFlash(flash);

            // detect TRUE peak (not arbitrary %)
            if (flash >= 0.98f && !reachedPeak)
            {
                reachedPeak = true;

                // switch EXACTLY at near-white moment
                introScreen.SetActive(false);
                mainMenuScreen.SetActive(true);

                menuBackground.localScale = menuBaseScale * 1.12f;
            }

            yield return null;
        }

        // ================= HOLD PEAK (important cinematic pause) =================
        SetFlash(1f);
        yield return new WaitForSeconds(0.6f);

        // ================= PHASE 2: SLOW FADE OUT + SETTLE =================
        t = 0f;

        Vector3 menuStart = menuBackground.localScale;
        Vector3 menuEnd = menuBaseScale;

        while (t < 1f)
        {
            t += Time.deltaTime / 2.0f; // slower fade-out

            float s = Mathf.SmoothStep(0f, 1f, t);

            // slow fade out flash
            SetFlash(1f - Mathf.Pow(s, 2f));

            // slow settle zoom
            menuBackground.localScale = Vector3.Lerp(menuStart, menuEnd, s);

            yield return null;
        }

        SetFlash(0f);

        state = State.Menu;
        transitioning = false;

        StartCoroutine(AnimateMenu());
    }

    // ================= MENU ANIMATION =================

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

    // ================= FLASH =================

    void SetFlash(float a)
    {
        if (flashImage == null) return;

        Color c = flashImage.color;
        c.a = a;
        flashImage.color = c;
    }

    // ================= BUTTONS =================

    public void StartGame()
    {
        SceneManager.LoadScene(gameScene);
    }

    public void QuitGame()
    {
        Application.Quit();
    }
}