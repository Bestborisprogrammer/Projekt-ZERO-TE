using UnityEngine;
using TMPro;
using UnityEngine.UI;
using System.Collections.Generic;

public class InventoryMenuPanel : MonoBehaviour
{
    public Transform itemListParent;
    public GameObject itemEntryPrefab;

    // Target selection for using items outside combat
    public GameObject targetPanel;
    public Transform targetParent;
    public GameObject targetButtonPrefab;

    private ItemSO pendingItem;

    void Start()
    {
        targetPanel.SetActive(false);
    }

    public void Refresh()
    {
        foreach (Transform child in itemListParent)
            Destroy(child.gameObject);

        foreach (var item in InventoryManager.Instance.items)
        {
            GameObject entry = Instantiate(itemEntryPrefab, itemListParent);
            var tmp = entry.GetComponentInChildren<TextMeshProUGUI>();
            tmp.text = $"{item.itemData.itemName} x{item.quantity}\n{item.itemData.description}";

            var btn = entry.GetComponent<Button>();
            var capturedItem = item.itemData;
            btn?.onClick.AddListener(() => SelectItem(capturedItem));
        }
    }

    void SelectItem(ItemSO item)
    {
        if (item.itemTarget == ItemTarget.Enemy)
        {
            Debug.Log("Can't use enemy-targeted items outside combat!");
            return;
        }

        pendingItem = item;
        ShowTargetSelection();
    }

    void ShowTargetSelection()
    {
        targetPanel.SetActive(true);

        foreach (Transform child in targetParent)
            Destroy(child.gameObject);

        foreach (var member in PartyManager.Instance.activeParty)
        {
            GameObject btn = Instantiate(targetButtonPrefab, targetParent);
            var tmp = btn.GetComponentInChildren<TextMeshProUGUI>();
            tmp.text = $"{member.Name}\nHP: {member.currentHP}/{member.MaxHP}";

            var capturedMember = member;
            btn.GetComponent<Button>()?.onClick.AddListener(() => UseItemOnMember(capturedMember));
        }
    }

    void UseItemOnMember(CharacterInstance member)
    {
        if (pendingItem == null) return;

        if (pendingItem.itemType == ItemType.Heal)
        {
            member.HealHP(pendingItem.flatHeal, pendingItem.percentHeal);
            Debug.Log($"Used {pendingItem.itemName} on {member.Name}!");
        }
        else if (pendingItem.itemType == ItemType.Buff)
        {
            member.ApplyStatModifier(pendingItem.statType, pendingItem.statModifier, pendingItem.modifierDuration);
            Debug.Log($"Applied {pendingItem.statType} +{pendingItem.statModifier} to {member.Name} for {pendingItem.modifierDuration} turns!");
        }

        InventoryManager.Instance.RemoveItem(pendingItem);
        pendingItem = null;
        targetPanel.SetActive(false);
        Refresh();
    }
}