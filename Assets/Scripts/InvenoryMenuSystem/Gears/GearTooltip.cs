using UnityEngine;
using TMPro;

public class GearTooltip : MonoBehaviour
{
    public static GearTooltip Instance;
    public GameObject tooltipPanel;
    public TextMeshProUGUI tooltipText;
    private RectTransform rt;

    void Awake()
    {
        Instance = this;
        rt = GetComponent<RectTransform>();
        if (tooltipPanel != null)
            tooltipPanel.SetActive(false);
    }

    void Update()
    {
        if (tooltipPanel != null && tooltipPanel.activeSelf)
        {
            Vector2 pos = Input.mousePosition;
            pos.x += 15;
            pos.y -= 15;
            rt.position = pos;
        }
    }

    public void Show(GearSO gear, CharacterInstance member = null)
    {
        if (gear == null) return;
        tooltipPanel.SetActive(true);

        string text =
            $"{gear.gearName}\n" +
            $"{gear.description}\n" +
            $"Slot: {gear.slot}\n" +
            $"──────────\n" +
            $"{(gear.bonusHP != 0 ? $"HP  +{gear.bonusHP}\n" : "")}" +
            $"{(gear.bonusATK != 0 ? $"ATK +{gear.bonusATK}\n" : "")}" +
            $"{(gear.bonusDEF != 0 ? $"DEF +{gear.bonusDEF}\n" : "")}" +
            $"{(gear.bonusSPD != 0 ? $"SPD +{gear.bonusSPD}\n" : "")}" +
            $"{(gear.bonusMP != 0 ? $"MP  +{gear.bonusMP}\n" : "")}";

        if (member != null)
        {
            var equipped = GearManager.Instance.GetGearFor(member.Name).GetSlot(gear.slot);
            text += "──────────\n";
            if (equipped != null)
            {
                text += $"Replaces: {equipped.gearName}\n";
                int hpD = gear.bonusHP - equipped.bonusHP;
                int atkD = gear.bonusATK - equipped.bonusATK;
                int defD = gear.bonusDEF - equipped.bonusDEF;
                int spdD = gear.bonusSPD - equipped.bonusSPD;
                int mpD = gear.bonusMP - equipped.bonusMP;
                if (hpD != 0) text += $"HP  {hpD:+#;-#}\n";
                if (atkD != 0) text += $"ATK {atkD:+#;-#}\n";
                if (defD != 0) text += $"DEF {defD:+#;-#}\n";
                if (spdD != 0) text += $"SPD {spdD:+#;-#}\n";
                if (mpD != 0) text += $"MP  {mpD:+#;-#}\n";
            }
            else
                text += "Slot is empty";
        }

        tooltipText.text = text;
    }

    public void Hide()
    {
        if (tooltipPanel != null)
            tooltipPanel.SetActive(false);
    }
}