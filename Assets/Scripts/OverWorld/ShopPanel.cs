using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;

public class ShopPanel : MonoBehaviour
{
    void Awake()
    {
        // Save stock state before scene changes
        UnityEngine.SceneManagement.SceneManager.sceneLoaded += OnSceneLoaded;
    }

    void OnDestroy()
    {
        UnityEngine.SceneManagement.SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    void OnSceneLoaded(UnityEngine.SceneManagement.Scene scene, UnityEngine.SceneManagement.LoadSceneMode mode)
    {
        // Save each entry's stock to PlayerPrefs
        for (int i = 0; i < shopEntries.Count; i++)
            shopEntries[i].stock = PlayerPrefs.GetInt($"shop_stock_{i}", shopEntries[i].stock);
    }

    public void SaveStock()
    {
        for (int i = 0; i < shopEntries.Count; i++)
            PlayerPrefs.SetInt($"shop_stock_{i}", shopEntries[i].stock);
        PlayerPrefs.Save();
    }

    [Header("Shop Stock")]
    public List<ShopEntry> shopEntries = new();

    [Header("UI")]
    public Transform buyListParent;
    public GameObject shopItemEntryPrefab;
    public TextMeshProUGUI shopGoldText;
    public TextMeshProUGUI shopTitleText;

    [Header("Sell Panel")]
    public Transform sellListParent;
    public GameObject sellEntryPrefab;
    public GameObject sellPanel;

    public void OpenShop(string merchantName)
    {
        gameObject.SetActive(true);
        if (shopTitleText != null)
            shopTitleText.text = $"{merchantName}'s Shop";
        Refresh();
    }

    public void CloseShop()
    {
        gameObject.SetActive(false);
    }

    public void Refresh()
    {
        foreach (Transform child in buyListParent)
            Destroy(child.gameObject);

        foreach (var entry in shopEntries)
        {
            if (entry.stock == 0) continue;
            GameObject obj = Instantiate(shopItemEntryPrefab, buyListParent);
            obj.GetComponent<ShopItemEntry>()?.Setup(entry, this);
        }

        if (shopGoldText != null)
            shopGoldText.text = $"Gold: {GoldManager.Instance.gold}G";
    }

    public void OpenSellPanel()
    {
        sellPanel.SetActive(true);
        RefreshSellPanel();
    }

    public void CloseSellPanel()
    {
        sellPanel.SetActive(false);
    }

    void RefreshSellPanel()
    {
        foreach (Transform child in sellListParent)
            Destroy(child.gameObject);

        // Sell items
        foreach (var invItem in InventoryManager.Instance.items)
        {
            var capturedItem = invItem;
            GameObject obj = Instantiate(sellEntryPrefab, sellListParent);
            var tmp = obj.GetComponentInChildren<TextMeshProUGUI>();
            var btn = obj.GetComponent<Button>();

            if (tmp != null)
                tmp.text = $"{capturedItem.itemData.itemName} x{capturedItem.quantity} - {capturedItem.itemData.sellPrice}G each";

            btn?.onClick.AddListener(() =>
            {
                GoldManager.Instance.AddGold(capturedItem.itemData.sellPrice);
                InventoryManager.Instance.RemoveItem(capturedItem.itemData);
                Debug.Log($"Sold {capturedItem.itemData.itemName} for {capturedItem.itemData.sellPrice}G");
                RefreshSellPanel();
                Refresh();
            });
        }

        // Sell gear - check GearMenuPanel allGear list
        foreach (var stack in GearManager.Instance.gearInventory)
        {
            var capturedStack = stack;
            GameObject obj = Instantiate(sellEntryPrefab, sellListParent);
            var tmp = obj.GetComponentInChildren<TextMeshProUGUI>();
            var btn = obj.GetComponent<Button>();

            if (tmp != null)
                tmp.text = $"{capturedStack.gear.gearName} x{capturedStack.quantity} - {capturedStack.gear.sellPrice}G each";

            btn?.onClick.AddListener(() =>
            {
                GearManager.Instance.RemoveGearFromInventory(capturedStack.gear);
                GoldManager.Instance.AddGold(capturedStack.gear.sellPrice);
                Debug.Log($"Sold {capturedStack.gear.gearName} for {capturedStack.gear.sellPrice}G");
                RefreshSellPanel();
                Refresh();
            });
        }

        // Show message if nothing to sell
        if (sellListParent.childCount == 0)
        {
            GameObject obj = Instantiate(sellEntryPrefab, sellListParent);
            var tmp = obj.GetComponentInChildren<TextMeshProUGUI>();
            if (tmp != null) tmp.text = "Nothing to sell!";
            var btn = obj.GetComponent<Button>();
            if (btn != null) btn.interactable = false;
        }
    }
}