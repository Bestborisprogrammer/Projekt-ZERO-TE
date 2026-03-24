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
    public Transform memberSlotsParent;
    public GameObject memberGearBlockPrefab;
    public GameObject gearSlotPrefab;

    [Header("Starting Gear")]
    public List<GearSO> startingGear = new();

    public CharacterInstance selectedMember;

    // Static so it persists across scene loads
    static bool initialized = false;

    void OnEnable()
    {
        if (!initialized)
        {
            foreach (var gear in startingGear)
                GearManager.Instance.AddGearToInventory(gear);
            initialized = true;
            Debug.Log($"Gear inventory initialized with {startingGear.Count} items");
        }
        Refresh();
    }

    // Call this from GameInitializer to reset on new play session
    public static void ResetInitialized()
    {
        initialized = false;
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
        foreach (Transform child in memberSlotsParent)
            Destroy(child.gameObject);

        foreach (var member in PartyManager.Instance.activeParty)
        {
            GameObject block = Instantiate(memberGearBlockPrefab, memberSlotsParent);

            var nameText = block.GetComponentInChildren<TextMeshProUGUI>();
            if (nameText != null)
                nameText.text = $"{member.Name}\nLv.{member.level}";

            var slotsContainer = block.transform.Find("Slots");
            if (slotsContainer == null) continue;

            foreach (Transform child in slotsContainer)
                Destroy(child.gameObject);

            SpawnSlot(slotsContainer, GearSlot.Weapon, false, member);
            SpawnSlot(slotsContainer, GearSlot.Helmet, false, member);
            SpawnSlot(slotsContainer, GearSlot.Torso, false, member);
            SpawnSlot(slotsContainer, GearSlot.Legs, false, member);
            SpawnSlot(slotsContainer, GearSlot.Feet, false, member);
            SpawnSlot(slotsContainer, GearSlot.Ring, false, member);
            SpawnSlot(slotsContainer, GearSlot.Ring, true, member);
        }
    }

    void SpawnSlot(Transform parent, GearSlot slot, bool isRing2, CharacterInstance member)
    {
        GameObject slotObj = Instantiate(gearSlotPrefab, parent);
        var dropZone = slotObj.GetComponent<GearSlotDropZone>();
        if (dropZone != null)
            dropZone.Setup(slot, isRing2, member);
        else
            Debug.LogWarning("GearSlotDropZone missing from gearSlotPrefab!");
    }
}