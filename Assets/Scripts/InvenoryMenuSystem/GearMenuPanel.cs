using UnityEngine;
using TMPro;
using UnityEngine.UI;
using System.Collections.Generic;

public class GearMenuPanel : MonoBehaviour
{
    [Header("Member Selection")]
    public Transform memberButtonParent;
    public GameObject memberButtonPrefab;

    [Header("Gear Slots")]
    public Transform gearSlotParent;
    public GameObject gearSlotPrefab;

    [Header("Available Gear List")]
    public GameObject gearListPanel;
    public Transform gearListParent;
    public GameObject gearEntryPrefab;

    // All gear in the game – drag your GearSOs here
    public List<GearSO> allGear = new();

    private CharacterInstance selectedMember;
    private GearSlot selectedSlot;
    private bool selectedIsRing2;

    void Start()
    {
        gearListPanel.SetActive(false);
    }

    public void Refresh()
    {
        RefreshMemberButtons();
        if (selectedMember != null)
            RefreshGearSlots();
    }

    void RefreshMemberButtons()
    {
        foreach (Transform child in memberButtonParent)
            Destroy(child.gameObject);

        foreach (var member in PartyManager.Instance.allMembers)
        {
            GameObject btn = Instantiate(memberButtonPrefab, memberButtonParent);
            var tmp = btn.GetComponentInChildren<TextMeshProUGUI>();
            tmp.text = $"{member.Name}\nLv.{member.level}";

            var capturedMember = member;
            btn.GetComponent<Button>()?.onClick.AddListener(() =>
            {
                selectedMember = capturedMember;
                RefreshGearSlots();
            });
        }
    }

    void RefreshGearSlots()
    {
        foreach (Transform child in gearSlotParent)
            Destroy(child.gameObject);

        if (selectedMember == null) return;

        var gear = GearManager.Instance.GetGearFor(selectedMember.Name);

        AddGearSlot("Weapon", gear.weapon, GearSlot.Weapon, false);
        AddGearSlot("Helmet", gear.helmet, GearSlot.Helmet, false);
        AddGearSlot("Torso", gear.torso, GearSlot.Torso, false);
        AddGearSlot("Legs", gear.legs, GearSlot.Legs, false);
        AddGearSlot("Feet", gear.feet, GearSlot.Feet, false);
        AddGearSlot("Ring 1", gear.ring1, GearSlot.Ring, false);
        AddGearSlot("Ring 2", gear.ring2, GearSlot.Ring, true);
    }

    void AddGearSlot(string label, GearSO equipped, GearSlot slot, bool isRing2)
    {
        GameObject slotObj = Instantiate(gearSlotPrefab, gearSlotParent);
        var tmp = slotObj.GetComponentInChildren<TextMeshProUGUI>();
        tmp.text = $"{label}: {equipped?.gearName ?? "Empty"}";

        var btn = slotObj.GetComponent<Button>();
        btn?.onClick.AddListener(() =>
        {
            selectedSlot = slot;
            selectedIsRing2 = isRing2;
            ShowGearList(slot);
        });
    }

    void ShowGearList(GearSlot slot)
    {
        gearListPanel.SetActive(true);

        foreach (Transform child in gearListParent)
            Destroy(child.gameObject);

        // Unequip option
        GameObject unequipBtn = Instantiate(gearEntryPrefab, gearListParent);
        unequipBtn.GetComponentInChildren<TextMeshProUGUI>().text = "Unequip";
        unequipBtn.GetComponent<Button>()?.onClick.AddListener(() =>
        {
            GearManager.Instance.UnequipGear(selectedMember.Name, selectedSlot, selectedIsRing2);
            gearListPanel.SetActive(false);
            RefreshGearSlots();
        });

        // Show matching gear
        foreach (var g in allGear)
        {
            if (g.slot != slot) continue;

            GameObject entry = Instantiate(gearEntryPrefab, gearListParent);
            var tmp = entry.GetComponentInChildren<TextMeshProUGUI>();
            tmp.text =
                $"{g.gearName}\n" +
                $"{(g.bonusHP != 0 ? $"HP+{g.bonusHP} " : "")}" +
                $"{(g.bonusATK != 0 ? $"ATK+{g.bonusATK} " : "")}" +
                $"{(g.bonusDEF != 0 ? $"DEF+{g.bonusDEF} " : "")}" +
                $"{(g.bonusSPD != 0 ? $"SPD+{g.bonusSPD} " : "")}" +
                $"{(g.bonusMP != 0 ? $"MP+{g.bonusMP}" : "")}";

            var capturedGear = g;
            entry.GetComponent<Button>()?.onClick.AddListener(() =>
            {
                GearManager.Instance.EquipGear(selectedMember.Name, capturedGear, selectedIsRing2);
                gearListPanel.SetActive(false);
                RefreshGearSlots();
            });
        }
    }
}