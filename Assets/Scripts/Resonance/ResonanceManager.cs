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

    // Pending flags – persist across scene loads like recruit system
    public static bool PendingPostResonanceBattle { get; set; } = false;
    public static bool PendingPostDuel { get; set; } = false;

    [Header("Resonance Skills")]
    public List<ManaAttackSO> resonanceSkills = new();

    private Image resonanceOverlay;

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
        CreateOverlay();
        Debug.Log($"[RESONANCE MGR] Start. PendingPostResonance={PendingPostResonanceBattle} PendingPostDuel={PendingPostDuel}");
    }

    void CreateOverlay()
    {
        // Create own canvas separate from everything
        GameObject canvasObj = new GameObject("ResonanceCanvas");
        canvasObj.transform.SetParent(transform);
        DontDestroyOnLoad(canvasObj);

        var canvas = canvasObj.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 997;
        canvasObj.AddComponent<CanvasScaler>();
        canvasObj.AddComponent<GraphicRaycaster>();

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

        // Start fully transparent and inactive
        resonanceOverlay.gameObject.SetActive(false);
    }

    public void OnBattleComplete()
    {
        if (!MeterUnlocked) return;
        if (IsResonating) return;

        battlesFought++;
        currentMeter = Mathf.Min(100f,
            (battlesFought / (float)battlesRequiredToFill) * 100f);
        Debug.Log($"[RESONANCE METER] {currentMeter:F0}% ({battlesFought}/{battlesRequiredToFill})");

        if (currentMeter >= 100f)
        {
            Debug.Log("[RESONANCE METER] FULL!");
            onMeterFull?.Invoke();
        }
    }

    public IEnumerator TriggerResonanceFlash(System.Action onComplete = null)
    {
        resonanceOverlay.gameObject.SetActive(true);
        Debug.Log("[RESONANCE] Starting violent flash");

        // Rapid violent flashes
        for (int i = 0; i < 10; i++)
        {
            float intensity = Mathf.Lerp(0.4f, 1f, i / 10f);
            resonanceOverlay.color = new Color(0.6f, 0f, 1f, intensity);
            yield return new WaitForSeconds(0.035f);
            resonanceOverlay.color = new Color(0.2f, 0f, 0.5f, 0.05f);
            yield return new WaitForSeconds(0.025f);
        }

        // Blinding bursts
        for (int i = 0; i < 6; i++)
        {
            resonanceOverlay.color = new Color(0.9f, 0.1f, 1f, 0.98f);
            yield return new WaitForSeconds(0.05f);
            resonanceOverlay.color = new Color(0.1f, 0f, 0.3f, 0.1f);
            yield return new WaitForSeconds(0.035f);
        }

        // Final sustain then fade
        resonanceOverlay.color = new Color(0.7f, 0f, 1f, 0.95f);
        yield return new WaitForSeconds(0.2f);

        float elapsed = 0f;
        while (elapsed < 0.5f)
        {
            elapsed += Time.deltaTime;
            float a = Mathf.Lerp(0.95f, 0f, elapsed / 0.5f);
            resonanceOverlay.color = new Color(0.5f, 0f, 1f, a);
            yield return null;
        }

        // Make sure it's fully off after flash
        resonanceOverlay.color = new Color(0f, 0f, 0f, 0f);
        resonanceOverlay.gameObject.SetActive(false);

        Debug.Log("[RESONANCE] Flash complete");
        onComplete?.Invoke();
    }

    public void ActivateResonance(bool scripted = false)
    {
        IsResonating = true;
        ScriptedResonanceActive = scripted;
        if (!scripted) { battlesFought = 0; currentMeter = 0f; }
        Debug.Log($"[RESONANCE] Activated scripted:{scripted}");
        onResonanceStart?.Invoke();
    }

    public void DeactivateResonance()
    {
        IsResonating = false;
        ScriptedResonanceActive = false;
        // Make absolutely sure overlay is gone
        if (resonanceOverlay != null)
        {
            resonanceOverlay.color = new Color(0, 0, 0, 0);
            resonanceOverlay.gameObject.SetActive(false);
        }
        Debug.Log("[RESONANCE] Deactivated + overlay cleared");
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
        Debug.Log("[RESONANCE] BlackOut complete");
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
        Debug.Log("[RESONANCE] FadeIn complete");
        onComplete?.Invoke();
    }

    // Force clear overlay – call on scene load just in case
    public void ForceHideOverlay()
    {
        if (resonanceOverlay != null)
        {
            resonanceOverlay.color = new Color(0, 0, 0, 0);
            resonanceOverlay.gameObject.SetActive(false);
        }
    }
}