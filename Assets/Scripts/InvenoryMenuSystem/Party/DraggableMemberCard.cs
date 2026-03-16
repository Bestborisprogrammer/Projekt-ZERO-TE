using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;

public class DraggableMemberCard : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
{
    public CharacterInstance member;
    public bool isInActiveParty;

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

    public void Setup(CharacterInstance m, bool active)
    {
        member = m;
        isInActiveParty = active;
        rootCanvas = GetComponentInParent<Canvas>();
        while (rootCanvas != null && !rootCanvas.isRootCanvas)
            rootCanvas = rootCanvas.transform.parent?.GetComponentInParent<Canvas>();
        RefreshText();
    }

    public void RefreshText()
    {
        var tmp = GetComponentInChildren<TextMeshProUGUI>();
        if (tmp == null) return;
        tmp.text = $"{member.Name}\nLv.{member.level}\nHP:{member.currentHP}/{member.MaxHP}";
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        if (rootCanvas == null)
            rootCanvas = GetComponentInParent<Canvas>();

        originalParent = transform.parent;
        originalIndex = transform.GetSiblingIndex();
        originalPosition = rectTransform.anchoredPosition;

        transform.SetParent(rootCanvas.transform, true);
        transform.SetAsLastSibling();

        canvasGroup.alpha = 0.7f;
        canvasGroup.blocksRaycasts = false;

        Debug.Log($"Dragging {member.Name} (active: {isInActiveParty})");
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

        // If not dropped on valid zone return to original
        if (transform.parent == rootCanvas.transform)
        {
            transform.SetParent(originalParent);
            transform.SetSiblingIndex(originalIndex);
            rectTransform.anchoredPosition = originalPosition;
            Debug.Log($"{member.Name} returned to original position");
        }

        Object.FindFirstObjectByType<PartyMenuPanel>()?.Refresh();
    }
}