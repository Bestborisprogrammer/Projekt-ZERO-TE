using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;
using System.Collections.Generic;

public class GearCategoryRow : MonoBehaviour
{
    [Header("UI")]
    public TextMeshProUGUI categoryLabel;
    public GameObject itemListContainer; // expands/collapses
    public Transform itemListParent;     // gear cards go here
    public GameObject gearCardPrefab;

    [Header("Settings")]
    public GearSlot slot;

    private bool isExpanded = false;
    private List<GearSO> gearList = new();

    public void Setup(GearSlot s, List<GearSO> gear, GameObject cardPrefab)
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

        if (isExpanded)
            BuildList();

        RefreshLabel();
    }

    void RefreshLabel()
    {
        if (categoryLabel != null)
            categoryLabel.text = $"{slot} ({gearList.Count}) {(isExpanded ? "▲" : "▼")}";
    }

    void BuildList()
    {
        foreach (Transform child in itemListParent)
            Destroy(child.gameObject);

        foreach (var gear in gearList)
        {
            GameObject card = Instantiate(gearCardPrefab, itemListParent);
            card.GetComponent<GearCard>()?.Setup(gear);
        }
    }

    public void RefreshGearList(List<GearSO> gear)
    {
        gearList = gear;
        RefreshLabel();
        if (isExpanded) BuildList();
    }
}