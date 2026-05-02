using UnityEngine;
using TMPro;

public class MerchantNPC : MonoBehaviour
{
    [Header("Merchant Info")]
    public string merchantName = "Boris";

    [Header("Interaction")]
    public TextMeshProUGUI interactPrompt;
    public ShopPanel shopPanel;
    public DialogueSO greetingDialogue; // optional short greeting

    private bool playerInRange = false;
    private bool shopOpen = false;

    void Start()
    {
        if (interactPrompt != null)
        {
            interactPrompt.text = $"E - Talk to {merchantName}";
            interactPrompt.gameObject.SetActive(false);
        }
        if (shopPanel != null)
            shopPanel.gameObject.SetActive(false);
    }

    void Update()
    {
        if (!playerInRange) return;
        if (shopOpen) return;

        if (Input.GetKeyDown(KeyCode.E))
        {
            if (greetingDialogue != null)
            {
                // Hide prompt during dialogue
                if (interactPrompt != null)
                    interactPrompt.gameObject.SetActive(false);

                DialogueUI.Instance.StartDialogue(greetingDialogue, () =>
                {
                    // Open shop after dialogue finishes
                    OpenShop();
                });
            }
            else
            {
                OpenShop();
            }
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
        shopPanel?.OpenShop(merchantName);
    }

    public void CloseShop()
    {
        shopOpen = false;
        shopPanel?.CloseShop();
        if (playerInRange && interactPrompt != null)
            interactPrompt.gameObject.SetActive(true);
    }
}