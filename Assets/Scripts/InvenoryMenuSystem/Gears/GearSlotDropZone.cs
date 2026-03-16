using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;

public class GearSlotDropZone : MonoBehaviour, IDropHandler, IPointerEnterHandler, IPointerExitHandler
{
    public GearSlot slot;
    public bool isRing2;
    public CharacterInstance member;
    public TextMeshProUGUI slotText;

    public void Setup(GearSlot s, bool ring2, CharacterInstance m)
    {
        slot = s;
        isRing2 = ring2;
        member = m;
        Refresh();
    }

    public void Refresh()
    {
        if (slotText == null) return;
        var gear = GearManager.Instance.GetGearFor(member.Name).GetSlot(slot, isRing2);
        string label = isRing2 ? "Ring 2" : slot.ToString();
        slotText.text = $"{label}\n{(gear != null ? gear.gearName : "Empty")}";
    }

    public void OnDrop(PointerEventData eventData)
    {
        var card = eventData.pointerDrag?.GetComponent<DraggableGearCard>();
        if (card == null) return;

        if (card.gear.slot != slot)
        {
            Debug.Log($"Wrong slot! {card.gear.gearName} goes in {card.gear.slot} not {slot}");
            return;
        }

        GearManager.Instance.EquipGear(member.Name, card.gear, isRing2);
        Refresh();
        Object.FindFirstObjectByType<GearMenuPanel>()?.Refresh();
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        var gear = GearManager.Instance.GetGearFor(member.Name).GetSlot(slot, isRing2);
        if (gear != null)
            GearTooltip.Instance?.Show(gear, member);
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        GearTooltip.Instance?.Hide();
    }
}