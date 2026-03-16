using UnityEngine;
using UnityEngine.EventSystems;

public class BenchDropZone : MonoBehaviour, IDropHandler
{
    public void OnDrop(PointerEventData eventData)
    {
        var dragged = eventData.pointerDrag?.GetComponent<DraggableMemberCard>();
        if (dragged == null) return;

        var active = PartyManager.Instance.activeParty;
        if (active.Contains(dragged.member))
        {
            active.Remove(dragged.member);
            Debug.Log($"{dragged.member.Name} removed from active party");
        }

        dragged.transform.SetParent(transform);
        dragged.originalParent = transform;

        Object.FindFirstObjectByType<PartyMenuPanel>()?.Refresh();
    }
}