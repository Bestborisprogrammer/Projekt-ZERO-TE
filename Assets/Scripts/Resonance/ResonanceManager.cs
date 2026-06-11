using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class ResonanceManager : MonoBehaviour
{
    public static ResonanceManager Instance;

    [Header("Resonance Meter")]
    public int battlesRequiredToFill = 5;
    public float currentMeter = 0f; // 0-100
    private int battlesFought = 0;

    [Header("State")]
    public static bool IsResonating { get; private set; } = false;
    public static bool ScriptedResonanceActive { get; private set; } = false;

    [Header("Resonance Skills")]
    public List<ManaAttackSO> resonanceSkills = new();

    [Header("Visual")]
    public Canvas resonanceCanvas;
    private UnityEngine.UI.Image resonanceOverlay;

    public System.Action onMeterFull;
    public System.Action onResonanceStart;
    public System.Action onResonanceEnd;

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else Destroy(gameObject);
    }

    void Start()
    {
        CreateResonanceOverlay();
    }

    void CreateResonanceOverlay()
    {
        GameObject canvasObj = new GameObject("ResonanceCanvas");
        canvasObj.transform.SetParent(transform);
        DontDestroyOnLoad(canvasObj);

        var canvas = canvasObj.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 998;
        canvasObj.AddComponent<CanvasScaler>();

        GameObject imgObj = new GameObject("ResonanceOverlay");
        imgObj.transform.SetParent(canvasObj.transform, false);
        resonanceOverlay = imgObj.AddComponent<UnityEngine.UI.Image>();
        resonanceOverlay.color = new Color(0.5f, 0f, 1f, 0f);

        var rt = imgObj.GetComponent<RectTransform>();
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;
        resonanceOverlay.gameObject.SetActive(false);
    }

    // Called after each battle
    public void OnBattleComplete()
    {
        if (IsResonating) return;
        battlesFought++;
        currentMeter = Mathf.Min(100f, (battlesFought / (float)battlesRequiredToFill) * 100f);
        Debug.Log($"[RESONANCE] Meter: {currentMeter:F0}% ({battlesFought}/{battlesRequiredToFill})");

        if (currentMeter >= 100f && !IsResonating)
        {
            Debug.Log("[RESONANCE] Meter full!");
            onMeterFull?.Invoke();
        }
    }

    public IEnumerator TriggerResonanceFlash(System.Action onComplete = null)
    {
        resonanceOverlay.gameObject.SetActive(true);

        // Flash purple multiple times
        for (int i = 0; i < 5; i++)
        {
            yield return StartCoroutine(FadeOverlay(0f, 0.7f, 0.1f));
            yield return StartCoroutine(FadeOverlay(0.7f, 0f, 0.1f));
            yield return new WaitForSeconds(0.05f);
        }

        // Final strong flash
        yield return StartCoroutine(FadeOverlay(0f, 0.9f, 0.15f));
        yield return new WaitForSeconds(0.2f);
        yield return StartCoroutine(FadeOverlay(0.9f, 0f, 0.3f));

        resonanceOverlay.gameObject.SetActive(false);
        onComplete?.Invoke();
    }

    IEnumerator FadeOverlay(float from, float to, float duration)
    {
        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float a = Mathf.Lerp(from, to, elapsed / duration);
            resonanceOverlay.color = new Color(0.5f, 0f, 1f, a);
            yield return null;
        }
        resonanceOverlay.color = new Color(0.5f, 0f, 1f, to);
    }

    public void ActivateResonance(bool scripted = false)
    {
        IsResonating = true;
        ScriptedResonanceActive = scripted;
        battlesFought = 0;
        currentMeter = 0f;
        Debug.Log($"[RESONANCE] Activated! Scripted:{scripted}");
        onResonanceStart?.Invoke();
    }

    public void DeactivateResonance()
    {
        IsResonating = false;
        ScriptedResonanceActive = false;
        Debug.Log("[RESONANCE] Deactivated");
        onResonanceEnd?.Invoke();
    }

    public IEnumerator FadeToBlackOut(System.Action onComplete = null)
    {
        resonanceOverlay.gameObject.SetActive(true);
        resonanceOverlay.color = new Color(0f, 0f, 0f, 0f);

        yield return StartCoroutine(FadeOverlayColor(
            new Color(0f, 0f, 0f, 0f),
            new Color(0f, 0f, 0f, 1f), 1.5f));

        yield return new WaitForSeconds(0.5f);
        onComplete?.Invoke();
    }

    IEnumerator FadeOverlayColor(Color from, Color to, float duration)
    {
        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            resonanceOverlay.color = Color.Lerp(from, to, elapsed / duration);
            yield return null;
        }
        resonanceOverlay.color = to;
    }

    public void FadeIn(System.Action onComplete = null)
    {
        StartCoroutine(FadeInRoutine(onComplete));
    }

    IEnumerator FadeInRoutine(System.Action onComplete)
    {
        yield return StartCoroutine(FadeOverlayColor(
            new Color(0f, 0f, 0f, 1f),
            new Color(0f, 0f, 0f, 0f), 1f));
        resonanceOverlay.gameObject.SetActive(false);
        onComplete?.Invoke();
    }
}