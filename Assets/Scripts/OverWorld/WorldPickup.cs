using UnityEngine;
using TMPro;

public enum PickupType { Gold, Item, Gear }

public class WorldPickup : MonoBehaviour
{
    [Header("Pickup Type")]
    public PickupType pickupType = PickupType.Gold;
    public int goldAmount = 20;
    public ItemSO item;
    public GearSO gear;

    [Header("Interaction")]
    public TextMeshProUGUI pickupPrompt;
    public float interactRange = 1.5f;

    private bool playerInRange = false;
    private string pickupID;

    void Awake()
    {
        if (pickupPrompt != null)
            pickupPrompt.gameObject.SetActive(false);
    }

    void Start()
    {
        pickupID = $"pickup_{transform.position.x}_{transform.position.y}";

        if (PlayerPrefs.GetInt(pickupID, 0) == 1)
        {
            Destroy(gameObject);
            return;
        }

        if (pickupPrompt != null)
            pickupPrompt.text = GetPromptText();
    }

    void Update()
    {
        if (playerInRange && Input.GetKeyDown(KeyCode.E))
            Pickup();
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (!other.CompareTag("Player")) return;
        playerInRange = true;
        if (pickupPrompt != null)
        {
            pickupPrompt.text = GetPromptText();
            pickupPrompt.gameObject.SetActive(true);
        }
    }

    void OnTriggerExit2D(Collider2D other)
    {
        if (!other.CompareTag("Player")) return;
        playerInRange = false;
        if (pickupPrompt != null)
            pickupPrompt.gameObject.SetActive(false);
    }

    string GetPromptText()
    {
        return pickupType switch
        {
            PickupType.Gold => $"E - Pick up {goldAmount} Gold",
            PickupType.Item => $"E - Pick up {item?.itemName}",
            PickupType.Gear => $"E - Pick up {gear?.gearName}",
            _ => "E - Pick up"
        };
    }

    void Pickup()
    {
        switch (pickupType)
        {
            case PickupType.Gold:
                GoldManager.Instance.AddGold(goldAmount);
                Debug.Log($"Picked up {goldAmount} Gold!");
                break;
            case PickupType.Item:
                if (item != null)
                {
                    InventoryManager.Instance.AddItem(item);
                    Debug.Log($"Picked up {item.itemName}!");
                }
                break;
            case PickupType.Gear:
                if (gear != null)
                {
                    GearManager.Instance.AddGearToInventory(gear);
                    Debug.Log($"Picked up {gear.gearName}!");
                }
                break;
        }

        PlayerPrefs.SetInt(pickupID, 1);
        PlayerPrefs.Save();

        if (pickupPrompt != null)
            pickupPrompt.gameObject.SetActive(false);

        Destroy(gameObject);
    }
}