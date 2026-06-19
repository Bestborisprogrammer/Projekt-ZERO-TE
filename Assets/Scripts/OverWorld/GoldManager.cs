using UnityEngine;
using TMPro;

public class GoldManager : MonoBehaviour
{
    public static GoldManager Instance;
    public int gold = 0;
    public string goldTextName = "GoldText";
    private TextMeshProUGUI goldText;

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else Destroy(gameObject);
    }

    void Start()
    {
        FindAndRefreshUI();
    }

    void OnEnable()
    {
        UnityEngine.SceneManagement.SceneManager.sceneLoaded += OnSceneLoaded;
    }

    void OnDisable()
    {
        UnityEngine.SceneManagement.SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    void OnSceneLoaded(UnityEngine.SceneManagement.Scene scene, UnityEngine.SceneManagement.LoadSceneMode mode)
    {
        FindAndRefreshUI();
    }

    void FindAndRefreshUI()
    {
        var obj = GameObject.Find(goldTextName);
        if (obj != null)
            goldText = obj.GetComponent<TextMeshProUGUI>();
        RefreshUI();
    }

    public void AddGold(int amount)
    {
        gold += amount;
        Debug.Log($"Gold: +{amount} | Total: {gold}");
        RefreshUI();
    }

    public bool SpendGold(int amount)
    {
        if (gold < amount)
        {
            Debug.Log("Not enough gold!");
            return false;
        }
        gold -= amount;
        Debug.Log($"Gold: -{amount} | Total: {gold}");
        RefreshUI();
        return true;
    }

    public void RefreshUI()
    {
        if (goldText != null)
            goldText.text = $"Gold: {gold}";
    }

    // FIXED: was referencing non-existent "currentGold" field, now uses "gold"
    public void SetGold(int amount)
    {
        gold = amount;
        RefreshUI();
    }
}