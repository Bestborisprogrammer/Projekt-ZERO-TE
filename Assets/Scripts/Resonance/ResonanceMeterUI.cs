using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class ResonanceMeterUI : MonoBehaviour
{
    [Header("UI")]
    public Image meterFill;
    public TextMeshProUGUI meterText;
    public GameObject meterContainer;

    void Update()
    {
        if (ResonanceManager.Instance == null) return;

        float meter = ResonanceManager.Instance.currentMeter;
        if (meterFill != null)
            meterFill.fillAmount = meter / 100f;
        if (meterText != null)
            meterText.text = $"Resonance {meter:F0}%";

        // Purple glow when full
        if (meterFill != null)
            meterFill.color = meter >= 100f
                ? new Color(0.8f, 0f, 1f)
                : new Color(0.5f, 0f, 0.8f);
    }
}