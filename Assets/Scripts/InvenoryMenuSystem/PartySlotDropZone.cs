using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;

public class PartySlotDropZone : MonoBehaviour, IDropHandler
{
    public int slotIndex; // 0-3
    private TextMeshProUGUI slotText;

    void Awake()
    {
        slotText = GetComponentInChildren<TextMeshProUGUI>();
    }

    public void OnDrop(PointerEventData eventData)
    {
        var dragged = eventData.pointerDrag?.GetComponent<DraggableMemberCard>();
        if (dragged == null) return;

        var active = PartyManager.Instance.activeParty;
        var member = dragged.member;

        // Already in this slot – do nothing
        if (slotIndex < active.Count && active[slotIndex] == member) return;

        // Remove from current position if already active
        active.Remove(member);

        // Insert into slot
        if (slotIndex >= active.Count)
        {
            // Pad with nulls if needed
            while (active.Count < slotIndex)
                active.Add(null);
            active.Add(member);
        }
        else
        {
            active.Insert(slotIndex, member);
            // Keep max 4
            while (active.Count > 4)
                active.RemoveAt(active.Count - 1);
        }

        // Remove any nulls
        active.RemoveAll(m => m == null);

        Debug.Log($"{member.Name} placed in slot {slotIndex}");

        // Move card into this slot visually
        dragged.transform.SetParent(transform);
        dragged.transform.localPosition = Vector3.zero;
        dragged.originalParent = transform;

        Object.FindFirstObjectByType<PartyMenuPanel>()?.Refresh();
    }

    public void RefreshSlot(CharacterInstance member)
    {
        if (slotText == null) return;
        slotText.text = member != null
            ? $"Slot {slotIndex + 1}\n{member.Name}\nLv.{member.level}"
            : $"Slot {slotIndex + 1}\nEmpty";
    }
}