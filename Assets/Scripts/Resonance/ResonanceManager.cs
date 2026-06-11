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
    private Canvas resonanceCanvas;

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
    }

    void CreateOverlay()
    {
        GameObject canvasObj = new GameObject("ResonanceCanvas");
        canvasObj.transform.SetParent(transform);
        DontDestroyOnLoad(canvasObj);

        resonanceCanvas = canvasObj.AddComponent<Canvas>();
        resonanceCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
        resonanceCanvas.sortingOrder = 998;
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
        Debug.Log($"[RESONANCE METER] {currentMeter:F0}% ({battlesFought}/{battlesRequiredToFill})");

        if (currentMeter >= 100f)
        {
            Debug.Log("[RESONANCE METER] FULL!");
            onMeterFull?.Invoke();
        }
    }

    // Violent purple flash
    public IEnumerator TriggerResonanceFlash(System.Action onComplete = null)
    {
        resonanceOverlay.gameObject.SetActive(true);

        // Rapid violent flashes
        for (int i = 0; i < 8; i++)
        {
            float intensity = 0.5f + (i * 0.06f);
            resonanceOverlay.color = new Color(0.6f, 0f, 1f, intensity);
            yield return new WaitForSeconds(0.04f);
            resonanceOverlay.color = new Color(0.3f, 0f, 0.6f, 0.1f);
            yield return new WaitForSeconds(0.03f);
        }

        // Screen shaking flashes
        for (int i = 0; i < 5; i++)
        {
            resonanceOverlay.color = new Color(0.8f, 0f, 1f, 0.95f);
            yield return new WaitForSeconds(0.06f);
            resonanceOverlay.color = new Color(0.2f, 0f, 0.4f, 0.2f);
            yield return new WaitForSeconds(0.04f);
        }

        // Final blinding flash
        yield return StartCoroutine(FadeOverlay(0.2f, 1f, 0.08f));
        yield return new WaitForSeconds(0.15f);
        yield return StartCoroutine(FadeOverlay(1f, 0.3f, 0.4f));

        // Settle to dim purple tint while resonating
        resonanceOverlay.color = new Color(0.4f, 0f, 0.8f, 0.15f);

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
        if (resonanceOverlay != null)
            resonanceOverlay.gameObject.SetActive(false);
        Debug.Log("[RESONANCE] Deactivated");
        onResonanceEnd?.Invoke();
    }

    public IEnumerator BlackOut(System.Action onComplete = null)
    {
        resonanceOverlay.gameObject.SetActive(true);
        float elapsed = 0f;
        while (elapsed < 1.5f)
        {
            elapsed += Time.deltaTime;
            float a = Mathf.Lerp(0f, 1f, elapsed / 1.5f);
            resonanceOverlay.color = new Color(0f, 0f, 0f, a);
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
            float a = Mathf.Lerp(1f, 0f, elapsed / 1f);
            resonanceOverlay.color = new Color(0f, 0f, 0f, a);
            yield return null;
        }
        resonanceOverlay.gameObject.SetActive(false);
        onComplete?.Invoke();
    }
}