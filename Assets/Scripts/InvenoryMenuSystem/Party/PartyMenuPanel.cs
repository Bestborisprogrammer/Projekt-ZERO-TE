using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class PartyMenuPanel : MonoBehaviour
{
    [Header("Active Party Area")]
    public Transform activePartyParent;     // Has ActivePartyDropZone

    [Header("Inactive Members Area")]
    public Transform inactivePartyParent;   // Has InactivePartyDropZone

    [Header("Prefab")]
    public GameObject memberCardPrefab;

    [Header("Status Text")]
    public TextMeshProUGUI statusText;      // Shows e.g. "Active: 2/4"

    void OnEnable()
    {
        Refresh();
    }

    public void Refresh()
    {
        // Clear both areas
        foreach (Transform child in activePartyParent)
            Destroy(child.gameObject);
        foreach (Transform child in inactivePartyParent)
            Destroy(child.gameObject);

        var active = PartyManager.Instance.activeParty;
        var all = PartyManager.Instance.allMembers;

        // Spawn active members
        foreach (var member in active)
        {
            GameObject card = Instantiate(memberCardPrefab, activePartyParent);
            var dc = card.GetComponent<DraggableMemberCard>();
            dc.Setup(member, true);
        }

        // Spawn inactive members
        foreach (var member in all)
        {
            if (active.Contains(member)) continue;
            GameObject card = Instantiate(memberCardPrefab, inactivePartyParent);
            var dc = card.GetComponent<DraggableMemberCard>();
            dc.Setup(member, false);
        }

        if (statusText != null)
            statusText.text = $"Active Party: {active.Count}/4";

        Debug.Log($"Party refreshed - Active: {active.Count}, Total: {all.Count}");
    }
}