using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class ItemEntryUI : MonoBehaviour
{
    public TextMeshProUGUI itemText;
    public Button useButton;
    public Button discardButton;

    private InventoryItem inventoryItem;
    private InventoryMenuPanel panel;

    public void Setup(InventoryItem item, InventoryMenuPanel menuPanel)
    {
        inventoryItem = item;
        panel = menuPanel;

        string targetTag = item.itemData.itemTarget == ItemTarget.Enemy ? "[ENEMY]" : "[ALLY]";
        string effectInfo = "";

        if (item.itemData.itemType == ItemType.Heal)
            effectInfo = $"Heal: {item.itemData.flatHeal} HP" +
                (item.itemData.percentHeal > 0 ? $" +{item.itemData.percentHeal * 100f:F0}%" : "");
        else if (item.itemData.itemType == ItemType.Buff)
            effectInfo = $"+{item.itemData.statModifier} {item.itemData.statType} ({item.itemData.modifierDuration} turns)";
        else if (item.itemData.itemType == ItemType.Debuff)
        {
            effectInfo = $"-{item.itemData.statModifier} {item.itemData.statType} ({item.itemData.modifierDuration} turns)";
            if (item.itemData.statusEffect != StatusEffectType.None)
                effectInfo += $" | {item.itemData.statusEffect} {item.itemData.statusChance * 100f:F0}%";
        }

        itemText.text =
            $"{item.itemData.itemName} x{item.quantity} {targetTag}\n" +
            $"{item.itemData.description}\n" +
            $"{effectInfo}";

        // Enemy items cant be used outside combat
        useButton.interactable = item.itemData.itemTarget == ItemTarget.Ally;
        useButton.onClick.AddListener(() => panel.OpenTargetPanel(inventoryItem.itemData));
        discardButton.onClick.AddListener(DiscardItem);
    }

    void DiscardItem()
    {
        InventoryManager.Instance.RemoveItem(inventoryItem.itemData);
        Debug.Log($"Discarded {inventoryItem.itemData.itemName}");
        panel.Refresh();
    }
}