using UnityEngine;
using TMPro;

public class MerchantNPC : MonoBehaviour
{
    [Header("Merchant Info")]
    public string merchantName = "Boris";

    [Header("Interaction")]
    public TextMeshProUGUI interactPrompt;
    public ShopPanel shopPanel;
    public DialogueSO greetingDialogue;

    private bool playerInRange = false;
    private bool shopOpen = false;
    private bool greetingPlayed = false;
    private string greetingSaveKey;

    void Start()
    {
        greetingSaveKey = $"merchant_greeting_{merchantName}";
        greetingPlayed = PlayerPrefs.GetInt(greetingSaveKey, 0) == 1;

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
        // Check if shop was closed externally
        if (shopOpen && shopPanel != null && !shopPanel.gameObject.activeSelf)
        {
            shopOpen = false;
            if (playerInRange && interactPrompt != null)
                interactPrompt.gameObject.SetActive(true);
        }

        if (!playerInRange || shopOpen) return;

        if (Input.GetKeyDown(KeyCode.E))
        {
            if (greetingDialogue != null && !greetingPlayed)
            {
                if (interactPrompt != null)
                    interactPrompt.gameObject.SetActive(false);

                greetingPlayed = true;
                PlayerPrefs.SetInt(greetingSaveKey, 1);
                PlayerPrefs.Save();

                DialogueUI.Instance.StartDialogue(greetingDialogue, () => OpenShop());
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
        if (!shopOpen && interactPrompt != null)
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
        if (interactPrompt != null)
            interactPrompt.gameObject.SetActive(false);
        shopPanel?.OpenShop(merchantName);
    }

    public void CloseShop()
    {
        if (!shopOpen) return;
        shopOpen = false;
        shopPanel?.CloseShop();
        if (playerInRange && interactPrompt != null)
            interactPrompt.gameObject.SetActive(true);
    }
}