using UnityEngine;
using TMPro;

public class GearTooltip : MonoBehaviour
{
    public static GearTooltip Instance;
    public GameObject tooltipPanel;
    public TextMeshProUGUI tooltipText;
    private RectTransform panelRT;
    private Canvas rootCanvas;

    void Awake()
    {
        Instance = this;
        panelRT = tooltipPanel.GetComponent<RectTransform>();
        rootCanvas = GetComponentInParent<Canvas>();

        var canvasGroup = tooltipPanel.GetComponent<CanvasGroup>();
        if (canvasGroup == null)
            canvasGroup = tooltipPanel.AddComponent<CanvasGroup>();
        canvasGroup.blocksRaycasts = false;
        canvasGroup.interactable = false;

        if (tooltipPanel != null)
            tooltipPanel.SetActive(false);
    }

    void Update()
    {
        if (tooltipPanel != null && tooltipPanel.activeSelf)
        {
            Vector2 mousePos = Input.mousePosition;
            float xPos = mousePos.x + 15f;
            float yPos = mousePos.y;

            float panelWidth = panelRT.rect.width;
            float panelHeight = panelRT.rect.height;
            if (xPos + panelWidth > Screen.width)
                xPos = mousePos.x - panelWidth - 10f;
            yPos = Mathf.Min(yPos, Screen.height - panelHeight);

            panelRT.position = new Vector2(xPos, yPos);
        }
    }

    public void Show(GearSO gear, CharacterInstance member = null)
    {
        if (gear == null)
        {
            Hide();
            return;
        }

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
                text += "Slot is empty\n";
        }

        tooltipText.text = text;
        tooltipPanel.SetActive(true);
    }

    public void Hide()
    {
        if (tooltipPanel != null)
            tooltipPanel.SetActive(false);
    }
}