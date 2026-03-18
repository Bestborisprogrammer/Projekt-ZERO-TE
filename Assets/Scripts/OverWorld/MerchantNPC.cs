using UnityEngine;
using TMPro;

public class MerchantNPC : MonoBehaviour
{
    [Header("Merchant Info")]
    public string merchantName = "Boris";

    [Header("Interaction")]
    public float interactRange = 2f;
    public TextMeshProUGUI interactPrompt;
    public ShopPanel shopPanel;

    private bool playerInRange = false;
    private bool shopOpen = false;

    void Start()
    {
        if (interactPrompt != null)
        {
            interactPrompt.text = $"E - Talk to {merchantName}";
            interactPrompt.gameObject.SetActive(false);
        }
        shopPanel.gameObject.SetActive(false);
    }

    void Update()
    {
        if (playerInRange && Input.GetKeyDown(KeyCode.E))
        {
            if (!shopOpen)
                OpenShop();
            else
                CloseShop();
        }
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (!other.CompareTag("Player")) return;
        playerInRange = true;
        if (interactPrompt != null)
            interactPrompt.gameObject.SetActive(true);
    }

    void OnTriggerExit2D(Collider2D other)
    {
        if (!other.CompareTag("Player")) return;
        playerInRange = false;
        if (interactPrompt != null)
            interactPrompt.gameObject.SetActive(false);
        CloseShop();
    }

    void OpenShop()
    {
        shopOpen = true;
        shopPanel.OpenShop(merchantName);
        if (interactPrompt != null)
            interactPrompt.gameObject.SetActive(false);
    }

    void CloseShop()
    {
        shopOpen = false;
        shopPanel.CloseShop();
        if (interactPrompt != null && playerInRange)
            interactPrompt.gameObject.SetActive(true);
    }
}