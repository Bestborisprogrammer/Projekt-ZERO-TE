using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class ShopItemEntry : MonoBehaviour
{
    private TextMeshProUGUI entryText;
    private Button buyButton;
    private ShopEntry entry;
    private ShopPanel panel;

    public void Setup(ShopEntry e, ShopPanel p)
    {
        entry = e;
        panel = p;

        entryText = GetComponentInChildren<TextMeshProUGUI>();
        buyButton = GetComponentInChildren<Button>();

        if (buyButton != null)
            buyButton.onClick.AddListener(Buy);

        Refresh();
    }

    public void Refresh()
    {
        if (entryText == null) return;

        if (entry.entryType == ShopEntryType.Item && entry.item != null)
        {
            string stock = entry.stock == -1 ? "Unlimited" : entry.stock.ToString();
            entryText.text =
                $"{entry.item.itemName} - {entry.item.buyPrice}G\n" +
                $"{entry.item.description}\n" +
                $"Stock: {stock}";

            if (buyButton != null)
                buyButton.interactable = entry.stock != 0 &&
                    GoldManager.Instance.gold >= entry.item.buyPrice;
        }
        else if (entry.entryType == ShopEntryType.Gear && entry.gear != null)
        {
            string stock = entry.stock == -1 ? "Unlimited" : entry.stock.ToString();
            entryText.text =
                $"{entry.gear.gearName} - {entry.gear.buyPrice}G\n" +
                $"ATK+{entry.gear.bonusATK} DEF+{entry.gear.bonusDEF} " +
                $"HP+{entry.gear.bonusHP} SPD+{entry.gear.bonusSPD} MP+{entry.gear.bonusMP}\n" +
                $"Stock: {stock}";

            if (buyButton != null)
                buyButton.interactable = entry.stock != 0 &&
                    GoldManager.Instance.gold >= entry.gear.buyPrice;
        }
    }

    void Buy()
    {
        if (entry.entryType == ShopEntryType.Item && entry.item != null)
        {
            if (!GoldManager.Instance.SpendGold(entry.item.buyPrice)) return;
            InventoryManager.Instance.AddItem(entry.item);
            Debug.Log($"Bought {entry.item.itemName} for {entry.item.buyPrice}G");
        }
        else if (entry.entryType == ShopEntryType.Gear && entry.gear != null)
        {
            if (!GoldManager.Instance.SpendGold(entry.gear.buyPrice)) return;
            Object.FindFirstObjectByType<GearMenuPanel>()?.allGear.Add(entry.gear);
            Debug.Log($"Bought {entry.gear.gearName} for {entry.gear.buyPrice}G");
        }

        if (entry.stock > 0) entry.stock--;
        Object.FindFirstObjectByType<ShopPanel>()?.SaveStock();
        panel.Refresh();
    }
}