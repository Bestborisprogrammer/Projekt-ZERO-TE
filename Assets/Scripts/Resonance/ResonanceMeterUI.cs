using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class ResonanceMeterUI : MonoBehaviour
{
    [Header("UI")]
    public GameObject meterContainer; // ResonancePanel
    public Image meterFill;           // ResonanceMeter (child, Image Filled)
    public TextMeshProUGUI meterText;

    private bool lastUnlockedState = false;

    void Start()
    {
        if (meterContainer != null)
        {
            meterContainer.SetActive(false);
            Debug.Log("[METER UI] Start - container forced inactive");
        }
        else
        {
            Debug.LogError("[METER UI] meterContainer is NOT assigned in Inspector!");
        }

        if (meterFill == null)
            Debug.LogError("[METER UI] meterFill is NOT assigned in Inspector!");
    }

    void Update()
    {
        if (ResonanceManager.Instance == null) return;

        bool unlocked = ResonanceManager.MeterUnlocked;

        // Log only when state changes, not every frame
        if (unlocked != lastUnlockedState)
        {
            Debug.Log($"[METER UI] MeterUnlocked changed to {unlocked}");
            lastUnlockedState = unlocked;

            if (meterContainer != null)
                meterContainer.SetActive(unlocked);
        }

        if (!unlocked) return;

        float meter = ResonanceManager.Instance.currentMeter;

        if (meterFill != null)
        {
            meterFill.fillAmount = meter / 100f;
            meterFill.color = meter >= 100f
                ? new Color(1f, 0.5f, 1f)
                : new Color(0.5f, 0f, 0.8f);
        }

        if (meterText != null)
            meterText.text = meter >= 100f ? "RESONANCE READY" : $"Resonance {meter:F0}%";
    }
}