using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using static Unity.Burst.Intrinsics.X86.Avx;
using static UnityEditor.Searcher.SearcherWindow.Alignment;

public class GearMenuPanel : MonoBehaviour
{
    [Header("Left Side - Categories")]
    public Transform categoryParent;        // Vertical Layout Group
    public GameObject categoryRowPrefab;    // GearCategoryRow prefab
    public GameObject gearCardPrefab;       // GearCard prefab

    [Header("Right Side - Member Slots")]
    public Transform memberSlotsParent;     // Horizontal Layout Group
    public GameObject memberGearBlockPrefab;
    public GameObject gearSlotPrefab;

    [Header("All Gear")]
    public List<GearSO> allGear = new();

    public CharacterInstance selectedMember;

    void OnEnable() => Refresh();

    public void Refresh()
    {
        RefreshCategories();
        RefreshMemberSlots();
    }

    void RefreshCategories()
    {
        foreach (Transform child in categoryParent)
            Destroy(child.gameObject);

        // Group gear by slot
        var slots = new[]
        {
            GearSlot.Weapon, GearSlot.Helmet, GearSlot.Torso,
            GearSlot.Legs, GearSlot.Feet, GearSlot.Ring
        };

        foreach (var slot in slots)
        {
            var gearInSlot = allGear.Where(g => g.slot == slot).ToList();

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
            if (slotsContainer == null)
            {
                Debug.LogWarning("MemberGearBlock missing 'Slots' child!");
                continue;
            }

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