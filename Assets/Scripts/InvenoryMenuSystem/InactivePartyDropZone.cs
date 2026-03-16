using UnityEngine;
using UnityEngine.EventSystems;

public class InactivePartyDropZone : MonoBehaviour, IDropHandler
{
    public void OnDrop(PointerEventData eventData)
    {
        var card = eventData.pointerDrag?.GetComponent<DraggableMemberCard>();
        if (card == null) return;

        var active = PartyManager.Instance.activeParty;

        // Must keep at least 1 in active party
        if (active.Contains(card.member) && active.Count <= 1)
        {
            Debug.Log("Must have at least 1 member in active party!");
            return;
        }

        active.Remove(card.member);
        card.isInActiveParty = false;
        card.transform.SetParent(transform);
        card.transform.localPosition = Vector3.zero;

        Debug.Log($"{card.member.Name} moved to inactive ({active.Count} active remaining)");
        Object.FindFirstObjectByType<PartyMenuPanel>()?.Refresh();
    }
}