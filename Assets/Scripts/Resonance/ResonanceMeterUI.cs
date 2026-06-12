using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class ResonanceMeterUI : MonoBehaviour
{
    [Header("UI")]
    public Image meterFill;
    public TextMeshProUGUI meterText;
    public GameObject meterContainer;

    void Start()
    {
        // Hide until unlocked
        if (meterContainer != null)
            meterContainer.SetActive(false);
    }

    void Update()
    {
        if (ResonanceManager.Instance == null) return;

        // Show only when unlocked
        bool unlocked = ResonanceManager.MeterUnlocked;
        if (meterContainer != null && meterContainer.activeSelf != unlocked)
            meterContainer.SetActive(unlocked);

        if (!unlocked) return;

        float meter = ResonanceManager.Instance.currentMeter;

        if (meterFill != null)
        {
            meterFill.fillAmount = meter / 100f;
            meterFill.color = meter >= 100f
                ? new Color(1f, 0.5f, 1f)   // bright pink-purple when full
                : new Color(0.5f, 0f, 0.8f); // normal purple
        }

        if (meterText != null)
            meterText.text = meter >= 100f ? "RESONANCE READY" : $"Resonance {meter:F0}%";
    }
}