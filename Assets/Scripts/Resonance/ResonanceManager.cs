using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;

public class ResonanceManager : MonoBehaviour
{
    public static ResonanceManager Instance;

    [Header("Resonance Meter")]
    public int battlesRequiredToFill = 5;
    public float currentMeter = 0f;
    private int battlesFought = 0;
    public static bool MeterUnlocked { get; set; } = false;

    [Header("State")]
    public static bool IsResonating { get; private set; } = false;
    public static bool ScriptedResonanceActive { get; private set; } = false;

    [Header("Resonance Skills")]
    public List<ManaAttackSO> resonanceSkills = new();

    private Image resonanceOverlay;

    // Pending flags
    public static bool WaitingForResonanceBattleReturn { get; set; } = false;
    public static bool WaitingForDuelReturn { get; set; } = false;

    public System.Action onMeterFull;
    public System.Action onResonanceStart;
    public System.Action onResonanceEnd;

    [Header("Basic Attack Recoil")]
    public float basicAttackSelfDamagePercent = 0.05f; // 5% max HP

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
        CreateOverlay();
    }

    void CreateOverlay()
    {
        GameObject canvasObj = new GameObject("ResonanceCanvas");
        canvasObj.transform.SetParent(transform);
        DontDestroyOnLoad(canvasObj);

        var canvas = canvasObj.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 997;
        canvasObj.AddComponent<CanvasScaler>();

        GameObject imgObj = new GameObject("ResonanceOverlay");
        imgObj.transform.SetParent(canvasObj.transform, false);
        resonanceOverlay = imgObj.AddComponent<Image>();
        resonanceOverlay.color = new Color(0.5f, 0f, 1f, 0f);
        resonanceOverlay.raycastTarget = false;

        var rt = imgObj.GetComponent<RectTransform>();
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;
        resonanceOverlay.gameObject.SetActive(false);
    }

    public void OnBattleComplete()
    {
        if (!MeterUnlocked) return;
        if (IsResonating) return;
        battlesFought++;
        currentMeter = Mathf.Min(100f,
            (battlesFought / (float)battlesRequiredToFill) * 100f);
        Debug.Log($"[RESONANCE METER] {currentMeter:F0}%");
        if (currentMeter >= 100f) onMeterFull?.Invoke();
    }

    // Shows a dim purple tint during resonance battle
    public void ShowResonanceTint()
    {
        if (resonanceOverlay == null) return;
        resonanceOverlay.gameObject.SetActive(true);
        resonanceOverlay.color = new Color(0.4f, 0f, 0.8f, 0.18f);
        Debug.Log("[RESONANCE] Tint shown");
    }

    public void HideResonanceTint()
    {
        if (resonanceOverlay == null) return;
        resonanceOverlay.color = new Color(0, 0, 0, 0);
        resonanceOverlay.gameObject.SetActive(false);
        Debug.Log("[RESONANCE] Tint hidden");
    }

    public IEnumerator TriggerResonanceFlash(System.Action onComplete = null)
    {
        resonanceOverlay.gameObject.SetActive(true);
        Debug.Log("[RESONANCE] Flash starting");

        // Each flash: bright white-purple ? black
        // Uses screen-tearing style rapid alternation
        Color purple = new Color(0.7f, 0f, 1f, 1f);
        Color white = new Color(1f, 0.8f, 1f, 1f);
        Color black = new Color(0f, 0f, 0f, 1f);
        Color off = new Color(0f, 0f, 0f, 0f);

        // Phase 1: Stutter flashes
        int[] frameTimes = { 3, 2, 2, 1, 2, 1, 1, 2, 1, 1 };
        foreach (int f in frameTimes)
        {
            resonanceOverlay.color = purple;
            for (int i = 0; i < f; i++) yield return null;
            resonanceOverlay.color = off;
            yield return null;
            yield return null;
        }

        yield return new WaitForSeconds(0.1f);

        // Phase 2: Heavy slams
        for (int i = 0; i < 4; i++)
        {
            resonanceOverlay.color = white;
            yield return new WaitForSeconds(0.06f);
            resonanceOverlay.color = black;
            yield return new WaitForSeconds(0.04f);
            resonanceOverlay.color = purple;
            yield return new WaitForSeconds(0.05f);
            resonanceOverlay.color = off;
            yield return new WaitForSeconds(0.03f);
        }

        // Phase 3: Final blinding white hold
        resonanceOverlay.color = white;
        yield return new WaitForSeconds(0.25f);

        // Fade from white to off
        float elapsed = 0f;
        while (elapsed < 0.4f)
        {
            elapsed += Time.deltaTime;
            float a = Mathf.Lerp(1f, 0f, elapsed / 0.4f);
            resonanceOverlay.color = new Color(0.8f, 0.3f, 1f, a);
            yield return null;
        }

        resonanceOverlay.color = off;
        resonanceOverlay.gameObject.SetActive(false);
        Debug.Log("[RESONANCE] Flash done");
        onComplete?.Invoke();
    }

    public void ActivateResonance(bool scripted = false)
    {
        IsResonating = true;
        ScriptedResonanceActive = scripted;
        Debug.Log($"[RESONANCE] Activated scripted:{scripted}");
        onResonanceStart?.Invoke();
    }

    public void DeactivateResonance()
    {
        IsResonating = false;
        ScriptedResonanceActive = false;
        HideResonanceTint();
        Debug.Log("[RESONANCE] Deactivated");
        onResonanceEnd?.Invoke();
    }

    public IEnumerator BlackOut(System.Action onComplete = null)
    {
        resonanceOverlay.gameObject.SetActive(true);
        resonanceOverlay.color = new Color(0f, 0f, 0f, 0f);
        float elapsed = 0f;
        while (elapsed < 1.5f)
        {
            elapsed += Time.deltaTime;
            resonanceOverlay.color = new Color(0f, 0f, 0f,
                Mathf.Lerp(0f, 1f, elapsed / 1.5f));
            yield return null;
        }
        resonanceOverlay.color = Color.black;
        yield return new WaitForSeconds(0.5f);
        onComplete?.Invoke();
    }

    public IEnumerator FadeIn(System.Action onComplete = null)
    {
        resonanceOverlay.gameObject.SetActive(true);
        resonanceOverlay.color = Color.black;
        float elapsed = 0f;
        while (elapsed < 1f)
        {
            elapsed += Time.deltaTime;
            resonanceOverlay.color = new Color(0f, 0f, 0f,
                Mathf.Lerp(1f, 0f, elapsed / 1f));
            yield return null;
        }
        resonanceOverlay.color = new Color(0, 0, 0, 0);
        resonanceOverlay.gameObject.SetActive(false);
        onComplete?.Invoke();
    }

    public void ForceHideOverlay()
    {
        if (resonanceOverlay == null) return;
        resonanceOverlay.color = new Color(0, 0, 0, 0);
        resonanceOverlay.gameObject.SetActive(false);
    }
}