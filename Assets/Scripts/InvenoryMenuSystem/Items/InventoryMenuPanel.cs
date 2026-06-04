using UnityEngine;
using TMPro;
using UnityEngine.UI;

public class InventoryMenuPanel : MonoBehaviour
{
    [Header("Item List")]
    public Transform itemListParent;
    public GameObject itemEntryPrefab;
    public TextMeshProUGUI inventoryCountText;

    [Header("Target Panel")]
    public GameObject targetPanel;
    public Transform targetParent;
    public GameObject targetMemberPrefab;
    public TextMeshProUGUI targetTitleText;

    private ItemSO pendingItem;

    void OnEnable() => Refresh();

    public void Refresh()
    {
        foreach (Transform child in itemListParent)
            Destroy(child.gameObject);

        foreach (var item in InventoryManager.Instance.items)
        {
            if (item.quantity <= 0) continue;
            GameObject entry = Instantiate(itemEntryPrefab, itemListParent);
            var entryUI = entry.GetComponent<ItemEntryUI>();
            if (entryUI != null)
                entryUI.Setup(item, this);
        }

        int count = InventoryManager.Instance.items.Count;
        if (inventoryCountText != null)
            inventoryCountText.text = $"Items: {count}/{InventoryManager.MaxSlots}";

        if (targetPanel != null)
            targetPanel.SetActive(false);
    }

    public void OpenTargetPanel(ItemSO item)
    {
        if (item.itemTarget == ItemTarget.Enemy)
        {
            Debug.Log("[MENU] Enemy items can only be used in combat!");
            return;
        }

        pendingItem = item;
        Debug.Log($"[MENU] Opening target panel for: {item.itemName} type:{item.itemType}");

        if (targetTitleText != null)
            targetTitleText.text = $"Use {item.itemName} on who?";

        targetPanel.SetActive(true);

        foreach (Transform child in targetParent)
            Destroy(child.gameObject);

        foreach (var member in PartyManager.Instance.activeParty)
        {
            GameObject btn = Instantiate(targetMemberPrefab, targetParent);

            // Get button from root OR any child
            Button button = btn.GetComponent<Button>();
            if (button == null)
                button = btn.GetComponentInChildren<Button>();

            var tmps = btn.GetComponentsInChildren<TextMeshProUGUI>();

            string preview = "";
            if (pendingItem.itemType == ItemType.Heal)
            {
                int totalHeal = pendingItem.flatHeal +
                    Mathf.RoundToInt(member.MaxHP * pendingItem.percentHeal);
                int actual = Mathf.Min(totalHeal, member.MaxHP - member.currentHP);
                preview = $"Heals +{actual} HP";
            }
            else if (pendingItem.itemType == ItemType.Buff)
                preview = $"+{pendingItem.statModifier} {pendingItem.statType} ({pendingItem.modifierDuration} turns)";

            if (tmps.Length > 0)
                tmps[0].text = $"{member.Name}  Lv.{member.level}\n" +
                    $"HP: {member.currentHP}/{member.MaxHP}\n{preview}";

            Debug.Log($"[MENU] Spawned button for {member.Name}, button null: {button == null}");

            if (button != null)
            {
                button.onClick.RemoveAllListeners();
                var capturedMember = member;
                button.onClick.AddListener(() =>
                {
                    Debug.Log($"[MENU] Button clicked for {capturedMember.Name}");
                    UseItemOnMember(capturedMember);
                });
            }
        }
    }

    public void CloseTargetPanel()
    {
        if (targetPanel != null)
            targetPanel.SetActive(false);
        pendingItem = null;
    }

    void UseItemOnMember(CharacterInstance member)
    {
        if (pendingItem == null)
        {
            Debug.LogError("[MENU] pendingItem is NULL!");
            return;
        }

        Debug.Log($"[MENU] Using {pendingItem.itemName} (type:{pendingItem.itemType}) on {member.Name}");
        Debug.Log($"[MENU] Before - HP: {member.currentHP}/{member.MaxHP}  statMods: {member.statModifiers.Count}");

        if (pendingItem.itemType == ItemType.Heal)
        {
            int totalHeal = pendingItem.flatHeal +
                Mathf.RoundToInt(member.MaxHP * pendingItem.percentHeal);
            totalHeal = Mathf.Max(0, totalHeal);
            int before = member.currentHP;
            member.currentHP = Mathf.Min(member.MaxHP, member.currentHP + totalHeal);
            int actual = member.currentHP - before;
            Debug.Log($"[MENU] Heal: flatHeal={pendingItem.flatHeal} " +
                $"percentHeal={pendingItem.percentHeal} total={totalHeal} actual={actual}");
            Debug.Log($"[MENU] After HP: {member.currentHP}/{member.MaxHP}");
        }
        else if (pendingItem.itemType == ItemType.Buff)
        {
            member.statModifiers.Add(new StatModifier(
                pendingItem.statType,
                pendingItem.statModifier,
                pendingItem.modifierDuration));
            Debug.Log($"[MENU] Buff applied: {pendingItem.statType} +{pendingItem.statModifier} " +
                $"for {pendingItem.modifierDuration} turns");
            Debug.Log($"[MENU] statMods after: {member.statModifiers.Count}");
        }

        InventoryManager.Instance.RemoveItem(pendingItem);
        pendingItem = null;
        targetPanel.SetActive(false);
        Refresh();

        Debug.Log($"[MENU] Done. Item removed from inventory.");
    }
}