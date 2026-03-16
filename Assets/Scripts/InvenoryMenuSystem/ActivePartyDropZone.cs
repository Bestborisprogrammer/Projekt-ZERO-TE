using UnityEngine;
using UnityEngine.EventSystems;

public class ActivePartyDropZone : MonoBehaviour, IDropHandler
{
    public void OnDrop(PointerEventData eventData)
    {
        var card = eventData.pointerDrag?.GetComponent<DraggableMemberCard>();
        if (card == null) return;

        var active = PartyManager.Instance.activeParty;

        // Already in active party
        if (active.Contains(card.member))
        {
            card.transform.SetParent(transform);
            return;
        }

        // Max 4 check
        if (active.Count >= 4)
        {
            Debug.Log("Active party is full! Max 4 members.");
            return;
        }

        active.Add(card.member);
        card.isInActiveParty = true;
        card.transform.SetParent(transform);
        card.transform.localPosition = Vector3.zero;

        Debug.Log($"{card.member.Name} added to active party ({active.Count}/4)");
        Object.FindFirstObjectByType<PartyMenuPanel>()?.Refresh();
    }
}