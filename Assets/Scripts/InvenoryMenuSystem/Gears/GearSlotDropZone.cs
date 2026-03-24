using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;

public class GearSlotDropZone : MonoBehaviour, IDropHandler, IPointerEnterHandler, IPointerExitHandler, IPointerClickHandler
{
    public GearSlot slot;
    public bool isRing2;
    public CharacterInstance member;

    private TextMeshProUGUI slotText;

    void Awake()
    {
        slotText = GetComponentInChildren<TextMeshProUGUI>();
    }

    public void Setup(GearSlot s, bool ring2, CharacterInstance m)
    {
        slot = s;
        isRing2 = ring2;
        member = m;
        Refresh();
    }

    public void Refresh()
    {
        if (slotText == null)
            slotText = GetComponentInChildren<TextMeshProUGUI>();
        if (slotText == null) return;

        var equipped = GearManager.Instance.GetGearFor(member.Name).GetSlot(slot, isRing2);
        string label = isRing2 ? "Ring 2" : slot.ToString();
        slotText.text = equipped != null ? $"{label}\n{equipped.gearName}" : $"{label}\nEmpty";
    }

    public void OnDrop(PointerEventData eventData)
    {
        var card = eventData.pointerDrag?.GetComponent<GearCard>();
        if (card == null) return;

        if (card.gear.slot != slot)
        {
            Debug.Log($"Wrong slot! {card.gear.gearName} goes in {card.gear.slot} not {slot}");
            return;
        }

        GearManager.Instance.EquipGear(member.Name, card.gear, isRing2);
        Debug.Log($"Equipped {card.gear.gearName} on {member.Name}!");
        Refresh();
        Object.FindFirstObjectByType<GearMenuPanel>()?.Refresh();
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        // Click to unequip
        var equipped = GearManager.Instance.GetGearFor(member.Name).GetSlot(slot, isRing2);
        if (equipped == null) return;

        GearManager.Instance.UnequipGear(member.Name, slot, isRing2);
        Debug.Log($"Unequipped {equipped.gearName} from {member.Name}!");
        Refresh();
        Object.FindFirstObjectByType<GearMenuPanel>()?.Refresh();
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        var equipped = GearManager.Instance.GetGearFor(member.Name).GetSlot(slot, isRing2);
        if (equipped != null)
            GearTooltip.Instance?.Show(equipped, member);
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        GearTooltip.Instance?.Hide();
    }
}