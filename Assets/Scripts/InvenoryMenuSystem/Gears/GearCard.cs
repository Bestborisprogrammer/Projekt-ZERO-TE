using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;

public class GearCard : MonoBehaviour,
    IBeginDragHandler, IDragHandler, IEndDragHandler,
    IPointerEnterHandler, IPointerExitHandler
{
    public GearSO gear;

    private CanvasGroup canvasGroup;
    private RectTransform rectTransform;
    private Canvas rootCanvas;
    private Transform originalParent;
    private int originalIndex;
    private Vector2 originalPosition;

    void Awake()
    {
        canvasGroup = GetComponent<CanvasGroup>();
        if (canvasGroup == null)
            canvasGroup = gameObject.AddComponent<CanvasGroup>();
        rectTransform = GetComponent<RectTransform>();
    }

    public void Setup(GearSO g)
    {
        gear = g;
        rootCanvas = GetComponentInParent<Canvas>();
        while (rootCanvas != null && !rootCanvas.isRootCanvas)
            rootCanvas = rootCanvas.transform.parent?.GetComponentInParent<Canvas>();

        var tmp = GetComponentInChildren<TextMeshProUGUI>();
        if (tmp != null)
            tmp.text =
                $"{gear.gearName}\n" +
                $"{(gear.bonusATK != 0 ? $"ATK+{gear.bonusATK} " : "")}" +
                $"{(gear.bonusDEF != 0 ? $"DEF+{gear.bonusDEF} " : "")}" +
                $"{(gear.bonusHP != 0 ? $"HP+{gear.bonusHP} " : "")}" +
                $"{(gear.bonusSPD != 0 ? $"SPD+{gear.bonusSPD} " : "")}" +
                $"{(gear.bonusMP != 0 ? $"MP+{gear.bonusMP}" : "")}";
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        var gearPanel = Object.FindFirstObjectByType<GearMenuPanel>();
        GearTooltip.Instance?.Show(gear, gearPanel?.selectedMember);
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        GearTooltip.Instance?.Hide();
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        GearTooltip.Instance?.Hide();

        if (rootCanvas == null)
            rootCanvas = GetComponentInParent<Canvas>();

        originalParent = transform.parent;
        originalIndex = transform.GetSiblingIndex();
        originalPosition = rectTransform.anchoredPosition;

        transform.SetParent(rootCanvas.transform, true);
        transform.SetAsLastSibling();

        canvasGroup.alpha = 0.7f;
        canvasGroup.blocksRaycasts = false;

        Debug.Log($"Dragging gear: {gear.gearName}");
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (rootCanvas == null) return;
        rectTransform.anchoredPosition += eventData.delta / rootCanvas.scaleFactor;
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        canvasGroup.alpha = 1f;
        canvasGroup.blocksRaycasts = true;

        if (transform.parent == rootCanvas.transform)
        {
            transform.SetParent(originalParent);
            transform.SetSiblingIndex(originalIndex);
            rectTransform.anchoredPosition = originalPosition;
            Debug.Log($"{gear.gearName} returned to list");
        }

        Object.FindFirstObjectByType<GearMenuPanel>()?.Refresh();
    }
}