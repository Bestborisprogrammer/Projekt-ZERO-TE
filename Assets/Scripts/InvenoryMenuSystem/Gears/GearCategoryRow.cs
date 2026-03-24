using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;

public class GearCategoryRow : MonoBehaviour
{
    [Header("UI")]
    public TextMeshProUGUI categoryLabel;
    public GameObject itemListContainer;
    public Transform itemListParent;
    public GameObject gearCardPrefab;

    [Header("Settings")]
    public GearSlot slot;

    private bool isExpanded = false;
    private List<GearStack> gearList = new();

    public void Setup(GearSlot s, List<GearStack> gear, GameObject cardPrefab)
    {
        slot = s;
        gearList = gear;
        gearCardPrefab = cardPrefab;
        isExpanded = false;
        itemListContainer.SetActive(false);
        RefreshLabel();
    }

    public void ToggleExpand()
    {
        isExpanded = !isExpanded;
        itemListContainer.SetActive(isExpanded);
        if (isExpanded) BuildList();
        RefreshLabel();
    }

    void RefreshLabel()
    {
        int total = 0;
        foreach (var g in gearList) total += g.quantity;
        if (categoryLabel != null)
            categoryLabel.text = $"{slot} ({total}) {(isExpanded ? "▲" : "▼")}";
    }

    void BuildList()
    {
        foreach (Transform child in itemListParent)
            Destroy(child.gameObject);

        foreach (var stack in gearList)
        {
            if (stack.quantity <= 0) continue;
            GameObject card = Instantiate(gearCardPrefab, itemListParent);
            card.GetComponent<GearCard>()?.Setup(stack.gear, stack.quantity);
        }
    }

    public void RefreshGearList(List<GearStack> gear)
    {
        gearList = gear;
        RefreshLabel();
        if (isExpanded) BuildList();
    }
}