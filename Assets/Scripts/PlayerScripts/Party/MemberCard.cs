using UnityEngine;
using TMPro;
using UnityEngine.UI;

public class MemberCard : MonoBehaviour
{
    public TextMeshProUGUI nameText;
    public TextMeshProUGUI levelText;
    public TextMeshProUGUI hpText;
    public TextMeshProUGUI manaText;
    public TextMeshProUGUI attackText;
    public TextMeshProUGUI defenseText;
    public TextMeshProUGUI speedText;
    public Slider xpSlider;
    public TextMeshProUGUI xpText;

    public void Setup(CharacterInstance s)
    {
        nameText.text = s.Name;
        levelText.text = $"Lv. {s.level}";
        hpText.text = $"HP: {s.currentHP}/{s.MaxHP}";
        manaText.text = $"MP: {s.currentMana}/{s.MaxMana}";
        attackText.text = $"ATK: {s.Attack}";
        defenseText.text = $"DEF: {s.Defense}";
        speedText.text = $"SPD: {s.Speed}";
        xpSlider.minValue = 0;
        xpSlider.maxValue = s.xpToNextLevel;
        xpSlider.value = s.currentXP;
        xpText.text = $"XP: {s.currentXP}/{s.xpToNextLevel}";
    }
}