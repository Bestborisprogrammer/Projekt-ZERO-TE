[System.Serializable]
public class InventoryItem
{
    public ItemSO itemData;
    public int quantity;

    public InventoryItem(ItemSO data, int qty = 1)
    {
        itemData = data;
        quantity = qty;
    }
}