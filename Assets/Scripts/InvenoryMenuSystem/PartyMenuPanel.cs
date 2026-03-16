using UnityEngine;
using TMPro;
using UnityEngine.UI;
using System.Collections.Generic;

public class PartyMenuPanel : MonoBehaviour
{
    [Header("Active Party Slots (top)")]
    public List<PartySlotDropZone> partySlots; // 4 slots

    [Header("Bench (bottom)")]
    public Transform benchParent;      // BenchDropZone goes here
    public GameObject memberCardPrefab;

    public void Refresh()
    {
        RefreshSlots();
        RefreshBench();
    }

    void RefreshSlots()
    {
        var active = PartyManager.Instance.activeParty;

        for (int i = 0; i < partySlots.Count; i++)
        {
            // Clear slot children except the drop zone itself
            foreach (Transform child in partySlots[i].transform)
                Destroy(child.gameObject);

            if (i < active.Count && active[i] != null)
            {
                partySlots[i].RefreshSlot(active[i]);

                // Spawn card in slot
                GameObject card = Instantiate(memberCardPrefab, partySlots[i].transform);
                var dc = card.GetComponent<DraggableMemberCard>();
                dc.Setup(active[i]);
            }
            else
            {
                partySlots[i].RefreshSlot(null);
            }
        }
    }

    void RefreshBench()
    {
        foreach (Transform child in benchParent)
            Destroy(child.gameObject);

        foreach (var member in PartyManager.Instance.allMembers)
        {
            // Only show bench members
            if (PartyManager.Instance.activeParty.Contains(member)) continue;

            GameObject card = Instantiate(memberCardPrefab, benchParent);
            var dc = card.GetComponent<DraggableMemberCard>();
            dc.Setup(member);
        }
    }
}