using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using static Unity.Burst.Intrinsics.X86.Avx;
using static UnityEngine.Rendering.DebugUI;

public class GearMenuPanel : MonoBehaviour
{
    [Header("Gear List (left side)")]
    public Transform weaponParent;
    public Transform helmetParent;
    public Transform torsoParent;
    public Transform legsParent;
    public Transform feetParent;
    public Transform ringParent;
    public GameObject gearCardPrefab;

    [Header("Member Gear Slots (right side)")]
    public Transform memberSlotsParent;
    public GameObject memberGearBlockPrefab;

    [Header("All Gear")]
    public List<GearSO> allGear = new();

    public CharacterInstance selectedMember;

    void OnEnable() => Refresh();

    public void Refresh()
    {
        RefreshGearList();
        RefreshMemberSlots();
    }

    void RefreshGearList()
    {
        // Clear all slot parents
        Transform[] parents = { weaponParent, helmetParent, torsoParent, legsParent, feetParent, ringParent };
        foreach (var p in parents)
            foreach (Transform child in p)
                Destroy(child.gameObject);

        foreach (var gear in allGear)
        {
            Transform parent = gear.slot switch
            {
                GearSlot.Weapon => weaponParent,
                GearSlot.Helmet => helmetParent,
                GearSlot.Torso => torsoParent,
                GearSlot.Legs => legsParent,
                GearSlot.Feet => feetParent,
                GearSlot.Ring => ringParent,
                _ => null
            };

            if (parent == null) continue;

            GameObject card = Instantiate(gearCardPrefab, parent);
            card.GetComponent<DraggableGearCard>()?.Setup(gear);
        }
    }

    void RefreshMemberSlots()
    {
        foreach (Transform child in memberSlotsParent)
            Destroy(child.gameObject);

        foreach (var member in PartyManager.Instance.activeParty)
        {
            GameObject block = Instantiate(memberGearBlockPrefab, memberSlotsParent);
            var nameText = block.transform.Find("MemberName")?.GetComponent<TextMeshProUGUI>();
            if (nameText != null)
                nameText.text = $"{member.Name} Lv.{member.level}";

            // Create a drop zone for each slot
            SpawnSlot(block, GearSlot.Weapon, false, member);
            SpawnSlot(block, GearSlot.Helmet, false, member);
            SpawnSlot(block, GearSlot.Torso, false, member);
            SpawnSlot(block, GearSlot.Legs, false, member);
            SpawnSlot(block, GearSlot.Feet, false, member);
            SpawnSlot(block, GearSlot.Ring, false, member);
            SpawnSlot(block, GearSlot.Ring, true, member);
        }
    }

    void SpawnSlot(GameObject block, GearSlot slot, bool isRing2, CharacterInstance member)
    {
        var slotsParent = block.transform.Find("Slots");
        if (slotsParent == null) return;

        GameObject slotObj = new GameObject($"{slot}{(isRing2 ? "2" : "")}");
        slotObj.transform.SetParent(slotsParent);

        var rt = slotObj.AddComponent<RectTransform>();
        rt.sizeDelta = new Vector2(120, 40);

        var img = slotObj.AddComponent<Image>();
        img.color = new Color(0.2f, 0.2f, 0.2f, 0.8f);

        var tmp = new GameObject("Text").AddComponent<TextMeshProUGUI>();
        tmp.transform.SetParent(slotObj.transform);
        tmp.fontSize = 12;
        tmp.alignment = TextAlignmentOptions.Center;
        var tmpRT = tmp.GetComponent<RectTransform>();
        tmpRT.anchorMin = Vector2.zero;
        tmpRT.anchorMax = Vector2.one;
        tmpRT.offsetMin = Vector2.zero;
        tmpRT.offsetMax = Vector2.zero;

        var dropZone = slotObj.AddComponent<GearSlotDropZone>();
        dropZone.slotText = tmp;
        dropZone.Setup(slot, isRing2, member);
    }
}