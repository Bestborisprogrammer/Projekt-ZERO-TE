using UnityEngine;
using System.Collections.Generic;

public class InventoryManager : MonoBehaviour
{
    public static InventoryManager Instance;

    public List<InventoryItem> items = new();
    public const int MaxSlots = 30;

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else Destroy(gameObject);
    }

    public bool AddItem(ItemSO item, int quantity = 1)
    {
        // Check if already exists
        var existing = items.Find(i => i.itemData == item);
        if (existing != null)
        {
            existing.quantity += quantity;
            return true;
        }

        // Check slot limit
        if (items.Count >= MaxSlots)
        {
            Debug.Log("Inventory full!");
            return false;
        }

        items.Add(new InventoryItem(item, quantity));
        return true;
    }

    public bool RemoveItem(ItemSO item, int quantity = 1)
    {
        var existing = items.Find(i => i.itemData == item);
        if (existing == null || existing.quantity < quantity) return false;

        existing.quantity -= quantity;
        if (existing.quantity <= 0)
            items.Remove(existing);

        return true;
    }

    public bool HasItem(ItemSO item) => items.Exists(i => i.itemData == item && i.quantity > 0);
}