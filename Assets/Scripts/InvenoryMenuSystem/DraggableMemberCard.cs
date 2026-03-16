using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;

public class DraggableMemberCard : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
{
    public CharacterInstance member;
    public Transform originalParent;
    public int originalIndex;

    private CanvasGroup canvasGroup;
    private RectTransform rectTransform;
    private Canvas canvas;

    void Awake()
    {
        canvasGroup = GetComponent<CanvasGroup>();
        rectTransform = GetComponent<RectTransform>();
        canvas = GetComponentInParent<Canvas>();
    }

    public void Setup(CharacterInstance m)
    {
        member = m;
        originalParent = transform.parent;
        RefreshText();
    }

    public void RefreshText()
    {
        var tmp = GetComponentInChildren<TextMeshProUGUI>();
        if (tmp == null) return;

        bool isActive = PartyManager.Instance.activeParty.Contains(member);
        tmp.text =
            $"{member.Name} Lv.{member.level}\n" +
            $"HP:{member.currentHP}/{member.MaxHP}\n" +
            $"ATK:{member.Attack} DEF:{member.Defense} SPD:{member.Speed}\n" +
            $"{(isActive ? "[ACTIVE]" : "[BENCH]")}";
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        originalParent = transform.parent;
        originalIndex = transform.GetSiblingIndex();

        // Move to top level canvas so it renders above everything
        transform.SetParent(canvas.transform);
        canvasGroup.alpha = 0.7f;
        canvasGroup.blocksRaycasts = false;
    }

    public void OnDrag(PointerEventData eventData)
    {
        rectTransform.anchoredPosition += eventData.delta / canvas.scaleFactor;
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        canvasGroup.alpha = 1f;
        canvasGroup.blocksRaycasts = true;

        // If not dropped on a valid slot, return to original position
        if (transform.parent == canvas.transform)
        {
            transform.SetParent(originalParent);
            transform.SetSiblingIndex(originalIndex);
        }

        Object.FindFirstObjectByType<PartyMenuPanel>()?.Refresh();
    }
}