using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class GearTooltip : MonoBehaviour
{
    public static GearTooltip Instance;

    public GameObject tooltipPanel;
    public TextMeshProUGUI tooltipText;

    void Awake()
    {
        Instance = this;
        tooltipPanel.SetActive(false);
    }

    void Update()
    {
        // Follow mouse
        if (tooltipPanel.activeSelf)
            transform.position = Input.mousePosition + new Vector3(15, -15, 0);
    }

    public void Show(GearSO gear, CharacterInstance hoveredMember = null)
    {
        tooltipPanel.SetActive(true);

        string stats =
            $"{gear.gearName}\n" +
            $"{gear.description}\n" +
            $"Slot: {gear.slot}\n" +
            $"──────────\n" +
            $"{(gear.bonusHP != 0 ? $"HP  +{gear.bonusHP}\n" : "")}" +
            $"{(gear.bonusATK != 0 ? $"ATK +{gear.bonusATK}\n" : "")}" +
            $"{(gear.bonusDEF != 0 ? $"DEF +{gear.bonusDEF}\n" : "")}" +
            $"{(gear.bonusSPD != 0 ? $"SPD +{gear.bonusSPD}\n" : "")}" +
            $"{(gear.bonusMP != 0 ? $"MP  +{gear.bonusMP}\n" : "")}";

        // Show stat change vs currently equipped
        if (hoveredMember != null)
        {
            var currentGear = GearManager.Instance.GetGearFor(hoveredMember.Name);
            var equipped = currentGear.GetSlot(gear.slot);

            stats += $"──────────\n";
            if (equipped != null)
            {
                stats += $"Replaces: {equipped.gearName}\n";
                int hpDiff = gear.bonusHP - equipped.bonusHP;
                int atkDiff = gear.bonusATK - equipped.bonusATK;
                int defDiff = gear.bonusDEF - equipped.bonusDEF;
                int spdDiff = gear.bonusSPD - equipped.bonusSPD;
                int mpDiff = gear.bonusMP - equipped.bonusMP;

                if (hpDiff != 0) stats += $"HP  {hpDiff:+#;-#}\n";
                if (atkDiff != 0) stats += $"ATK {atkDiff:+#;-#}\n";
                if (defDiff != 0) stats += $"DEF {defDiff:+#;-#}\n";
                if (spdDiff != 0) stats += $"SPD {spdDiff:+#;-#}\n";
                if (mpDiff != 0) stats += $"MP  {mpDiff:+#;-#}\n";
            }
            else
                stats += "Slot is empty";
        }

        tooltipText.text = stats;
    }

    public void Hide()
    {
        tooltipPanel.SetActive(false);
    }
}