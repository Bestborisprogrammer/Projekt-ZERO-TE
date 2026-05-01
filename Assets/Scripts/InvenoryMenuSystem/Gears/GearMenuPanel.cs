using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;
using System.Linq;

public class GearMenuPanel : MonoBehaviour
{
    [Header("Left Side - Categories")]
    public Transform categoryParent;
    public GameObject categoryRowPrefab;
    public GameObject gearCardPrefab;

    [Header("Right Side - Member Slots")]
    public Transform slotsContainer;
    public GameObject gearSlotPrefab;
    public TextMeshProUGUI memberNameText;
    public Button prevMemberButton;
    public Button nextMemberButton;

    [Header("Starting Gear")]
    public List<GearSO> startingGear = new();

    public CharacterInstance selectedMember;
    private int selectedMemberIndex = 0;

    static bool initialized = false;

    void OnEnable()
    {
        if (!initialized)
        {
            foreach (var gear in startingGear)
                GearManager.Instance.AddGearToInventory(gear);
            initialized = true;
        }

        prevMemberButton.onClick.RemoveAllListeners();
        nextMemberButton.onClick.RemoveAllListeners();
        prevMemberButton.onClick.AddListener(PrevMember);
        nextMemberButton.onClick.AddListener(NextMember);

        selectedMemberIndex = 0;
        Refresh();
    }

    public static void ResetInitialized() => initialized = false;

    void PrevMember()
    {
        selectedMemberIndex--;
        if (selectedMemberIndex < 0)
            selectedMemberIndex = PartyManager.Instance.activeParty.Count - 1;
        RefreshMemberSlots();
    }

    void NextMember()
    {
        selectedMemberIndex++;
        if (selectedMemberIndex >= PartyManager.Instance.activeParty.Count)
            selectedMemberIndex = 0;
        RefreshMemberSlots();
    }

    public void Refresh()
    {
        RefreshCategories();
        RefreshMemberSlots();
    }

    void RefreshCategories()
    {
        foreach (Transform child in categoryParent)
            Destroy(child.gameObject);

        var slots = new[]
        {
            GearSlot.Weapon, GearSlot.Helmet, GearSlot.Torso,
            GearSlot.Legs, GearSlot.Feet, GearSlot.Ring
        };

        foreach (var slot in slots)
        {
            var gearInSlot = GearManager.Instance.gearInventory
                .Where(g => g.gear.slot == slot && g.quantity > 0)
                .ToList();

            GameObject row = Instantiate(categoryRowPrefab, categoryParent);
            var categoryRow = row.GetComponent<GearCategoryRow>();
            if (categoryRow != null)
                categoryRow.Setup(slot, gearInSlot, gearCardPrefab);
        }
    }

    void RefreshMemberSlots()
    {
        foreach (Transform child in slotsContainer)
            Destroy(child.gameObject);

        var activeParty = PartyManager.Instance.activeParty;
        if (activeParty.Count == 0) return;

        // Clamp index
        selectedMemberIndex = Mathf.Clamp(selectedMemberIndex, 0, activeParty.Count - 1);
        selectedMember = activeParty[selectedMemberIndex];

        // Update name text
        if (memberNameText != null)
            memberNameText.text = $"{selectedMember.Name}  Lv.{selectedMember.level}" +
                $"  ({selectedMemberIndex + 1}/{activeParty.Count})";

        // Show/hide nav buttons
        bool multipleMembers = activeParty.Count > 1;
        prevMemberButton.gameObject.SetActive(multipleMembers);
        nextMemberButton.gameObject.SetActive(multipleMembers);

        // Spawn gear slots
        SpawnSlot("Weapon", GearSlot.Weapon, false);
        SpawnSlot("Helmet", GearSlot.Helmet, false);
        SpawnSlot("Torso", GearSlot.Torso, false);
        SpawnSlot("Legs", GearSlot.Legs, false);
        SpawnSlot("Feet", GearSlot.Feet, false);
        SpawnSlot("Ring 1", GearSlot.Ring, false);
        SpawnSlot("Ring 2", GearSlot.Ring, true);
    }

    void SpawnSlot(string label, GearSlot slot, bool isRing2)
    {
        GameObject slotObj = Instantiate(gearSlotPrefab, slotsContainer);
        var dropZone = slotObj.GetComponent<GearSlotDropZone>();
        if (dropZone != null)
            dropZone.Setup(slot, isRing2, selectedMember);

        // Set slot label
        var texts = slotObj.GetComponentsInChildren<TextMeshProUGUI>();
        if (texts.Length > 0)
        {
            var gear = GearManager.Instance.GetGearFor(selectedMember.Name).GetSlot(slot, isRing2);
            texts[0].text = gear != null
                ? $"{label}: {gear.gearName}"
                : $"{label}: Empty";
        }
    }
}