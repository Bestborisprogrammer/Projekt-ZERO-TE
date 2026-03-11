using UnityEngine;
using TMPro;

public class PartyStatusUI : MonoBehaviour
{
    public GameObject statusPanel;
    public TextMeshProUGUI memberInfoText;

    void Start()
    {
        statusPanel.SetActive(false);
        memberInfoText.gameObject.SetActive(false);
    }

    public void TogglePanel()
    {
        bool isOpening = !statusPanel.activeSelf;
        statusPanel.SetActive(isOpening);
        memberInfoText.gameObject.SetActive(isOpening);

        if (isOpening) RefreshStats();
    }

    void RefreshStats()
    {
        if (PartyManager.Instance == null) return;

        string info = "";
        foreach (var member in PartyManager.Instance.activeParty)
        {
            float xpPercent = (float)member.currentXP / member.xpToNextLevel * 100f;
            info += $"══════════════════\n";
            info += $"{member.Name}  |  Lv. {member.level}\n";
            info += $"══════════════════\n";
            info += $"HP:  {member.currentHP} / {member.MaxHP}\n";
            info += $"MP:  {member.currentMana} / {member.MaxMana}\n";
            info += $"ATK: {member.Attack}\n";
            info += $"DEF: {member.Defense}\n";
            info += $"SPD: {member.Speed}\n";
            info += $"XP:  {member.currentXP} / {member.xpToNextLevel}  ({xpPercent:F1}%)\n\n";
        }
        memberInfoText.text = info;
    }
}