using UnityEngine;
using TMPro;
using UnityEngine.UI;
using System.Collections.Generic;

public class InventoryMenuPanel : MonoBehaviour
{
    [Header("Item List")]
    public Transform itemListParent;
    public GameObject itemEntryPrefab;
    public TextMeshProUGUI inventoryCountText;

    [Header("Target Panel")]
    public GameObject targetPanel;
    public Transform targetParent;
    public GameObject targetMemberPrefab;
    public TextMeshProUGUI targetTitleText;

    private ItemSO pendingItem;

    void OnEnable() => Refresh();

    public void Refresh()
    {
        foreach (Transform child in itemListParent)
            Destroy(child.gameObject);

        foreach (var item in InventoryManager.Instance.items)
        {
            GameObject entry = Instantiate(itemEntryPrefab, itemListParent);
            var entryUI = entry.GetComponent<ItemEntryUI>();
            if (entryUI != null)
                entryUI.Setup(item, this);
            else
                Debug.LogWarning("ItemEntryUI component missing from prefab!");
        }

        int count = InventoryManager.Instance.items.Count;
        if (inventoryCountText != null)
            inventoryCountText.text = $"Items: {count}/{InventoryManager.MaxSlots}";

        if (targetPanel != null)
            targetPanel.SetActive(false);
    }

    public void OpenTargetPanel(ItemSO item)
    {
        if (item.itemTarget == ItemTarget.Enemy)
        {
            Debug.Log("Enemy items can only be used in combat!");
            return;
        }

        pendingItem = item;

        if (targetTitleText != null)
            targetTitleText.text = $"Use {item.itemName} on who?";

        targetPanel.SetActive(true);

        foreach (Transform child in targetParent)
            Destroy(child.gameObject);

        foreach (var member in PartyManager.Instance.activeParty)
        {
            GameObject btn = Instantiate(targetMemberPrefab, targetParent);
            var tmps = btn.GetComponentsInChildren<TextMeshProUGUI>();
            if (tmps.Length > 0)
            {
                string preview = "";
                if (pendingItem.itemType == ItemType.Heal)
                {
                    int heal = pendingItem.flatHeal + Mathf.RoundToInt(member.MaxHP * pendingItem.percentHeal);
                    int actualHeal = Mathf.Min(heal, member.MaxHP - member.currentHP);
                    preview = $"+{actualHeal} HP";
                }
                else if (pendingItem.itemType == ItemType.Buff)
                    preview = $"+{pendingItem.statModifier} {pendingItem.statType} ({pendingItem.modifierDuration} turns)";

                tmps[0].text = $"{member.Name} Lv.{member.level}\nHP: {member.currentHP}/{member.MaxHP}\n{preview}";
            }

            var capturedMember = member;
            btn.GetComponent<Button>()?.onClick.AddListener(() => UseItemOnMember(capturedMember));
        }
    }

    public void CloseTargetPanel()
    {
        if (targetPanel != null)
            targetPanel.SetActive(false);
        pendingItem = null;
    }

    void UseItemOnMember(CharacterInstance member)
    {
        if (pendingItem == null) return;

        if (pendingItem.itemType == ItemType.Heal)
        {
            member.HealHP(pendingItem.flatHeal, pendingItem.percentHeal);
            int heal = pendingItem.flatHeal +
                Mathf.RoundToInt(member.MaxHP * pendingItem.percentHeal);
            Debug.Log($"Used {pendingItem.itemName} on {member.Name} for {heal} HP!");
        }
        else if (pendingItem.itemType == ItemType.Buff)
        {
            member.ApplyStatModifier(pendingItem.statType,
                pendingItem.statModifier, pendingItem.modifierDuration);
            Debug.Log($"Applied {pendingItem.statType} +{pendingItem.statModifier} " +
                $"to {member.Name} for {pendingItem.modifierDuration} turns!");
        }

        InventoryManager.Instance.RemoveItem(pendingItem);
        pendingItem = null;
        targetPanel.SetActive(false);
        Refresh();
    }
}