using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;

public class SaveManager : MonoBehaviour
{
    public static SaveManager Instance { get; private set; }

    public const int MaxSlots = 10;
    public const int AutoSaveSlot = -1;
    public const string OverworldSceneName = "overworldScene";

    [Header("Autosave")]
    public float autoSaveIntervalSeconds = 600f;
    private float autoSaveTimer = 0f;

    private List<CharacterStatsSO> allCharacterSOs = new();
    private List<ItemSO> allItemSOs = new();
    private List<GearSO> allGearSOs = new();
    private bool databaseLoaded = false;

    public float sessionPlaytime = 0f;
    public int currentSlot = -2;

    private static SaveData pendingLoadData = null;
    public static bool IsLoadingSave { get; private set; } = false;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    static void Bootstrap()
    {
        if (Instance != null) return;

        GameObject obj = new GameObject("SaveManager");
        Instance = obj.AddComponent<SaveManager>();
        DontDestroyOnLoad(obj);
        Debug.Log("[SAVE MANAGER] Bootstrapped via RuntimeInitializeOnLoadMethod - single instance guaranteed");

        Instance.LoadDatabase();
    }

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            LoadDatabase();
        }
        else if (Instance != this)
        {
            Destroy(gameObject);
        }
    }

    void LoadDatabase()
    {
        if (databaseLoaded) return;

        allCharacterSOs = Resources.LoadAll<CharacterStatsSO>("").ToList();
        allItemSOs = Resources.LoadAll<ItemSO>("").ToList();
        allGearSOs = Resources.LoadAll<GearSO>("").ToList();

        Debug.Log($"[SAVE MANAGER] Database loaded: {allCharacterSOs.Count} characters, " +
            $"{allItemSOs.Count} items, {allGearSOs.Count} gear");

        databaseLoaded = true;
    }

    void Update()
    {
        sessionPlaytime += Time.deltaTime;

        if (UnityEngine.SceneManagement.SceneManager.GetActiveScene().name == OverworldSceneName)
        {
            if (pendingLoadData != null)
            {
                Debug.Log("[SAVE] *** pendingLoadData detected in correct scene - starting apply coroutine ***");
                var dataToApply = pendingLoadData;
                pendingLoadData = null;
                StartCoroutine(ApplyLoadedDataDelayed(dataToApply));
                return;
            }

            if (!IsLoadingSave)
            {
                autoSaveTimer += Time.deltaTime;
                if (autoSaveTimer >= autoSaveIntervalSeconds)
                {
                    autoSaveTimer = 0f;
                    Debug.Log("[SAVE] Autosaving...");
                    SaveToSlot(AutoSaveSlot, "Autosave");
                }
            }
        }
    }

    string GetPath(int slot)
    {
        string fileName = slot == AutoSaveSlot ? "autosave.json" : $"save_slot_{slot}.json";
        return Path.Combine(Application.persistentDataPath, fileName);
    }

    public bool SlotExists(int slot) => File.Exists(GetPath(slot));

    public SaveData LoadSlotPreview(int slot)
    {
        string path = GetPath(slot);
        if (!File.Exists(path)) return null;
        try
        {
            string json = File.ReadAllText(path);
            return JsonUtility.FromJson<SaveData>(json);
        }
        catch (System.Exception e)
        {
            Debug.LogError($"[SAVE] Failed to read preview for slot {slot}: {e.Message}");
            return null;
        }
    }

    public CharacterStatsSO FindCharacterSO(string name) => allCharacterSOs.Find(c => c.characterName == name);
    public ItemSO FindItemSO(string name) => allItemSOs.Find(i => i.itemName == name);
    public GearSO FindGearSO(string name) => allGearSOs.Find(g => g.gearName == name);

    public void SaveToSlot(int slot, string customName = null)
    {
        Debug.Log($"[SAVE] Saving to slot {slot}");

        if (PartyManager.Instance == null)
        {
            Debug.LogError("[SAVE] PartyManager.Instance is null – cannot save outside overworldScene!");
            return;
        }

        var data = new SaveData();
        data.isEmpty = false;
        data.saveName = customName ?? $"Edward's Save Slot {slot + 1}";
        data.playtimeSeconds = sessionPlaytime;
        data.dateTime = System.DateTime.Now.ToString("yyyy-MM-dd HH:mm");

        var player = GameObject.FindGameObjectWithTag("Player");
        data.sceneName = OverworldSceneName;

        if (player != null)
        {
            data.playerX = player.transform.position.x;
            data.playerY = player.transform.position.y;
            data.playerZ = player.transform.position.z;
        }
        else if (PlayerPrefs.HasKey("PlayerReturnX"))
        {
            data.playerX = PlayerPrefs.GetFloat("PlayerReturnX");
            data.playerY = PlayerPrefs.GetFloat("PlayerReturnY");
            data.playerZ = PlayerPrefs.GetFloat("PlayerReturnZ");
            Debug.Log($"[SAVE] Used cached menu-return position: {data.playerX},{data.playerY},{data.playerZ}");
        }
        else
        {
            Debug.LogWarning("[SAVE] Could not determine player position - saving as 0,0,0");
        }

        foreach (var member in PartyManager.Instance.allMembers)
        {
            data.allMembers.Add(new CharacterSaveEntry
            {
                characterSOName = member.baseData.characterName,
                level = member.level,
                currentXP = member.currentXP,
                xpToNextLevel = member.xpToNextLevel,
                currentHP = member.currentHP,
                currentMana = member.currentMana
            });
        }
        foreach (var active in PartyManager.Instance.activeParty)
            data.activePartyNames.Add(active.baseData.characterName);

        foreach (var item in InventoryManager.Instance.items)
        {
            data.inventoryItems.Add(new InventoryItemSave
            {
                itemSOName = item.itemData.itemName,
                quantity = item.quantity
            });
        }

        foreach (var stack in GearManager.Instance.gearInventory)
        {
            data.gearStacks.Add(new GearStackSave
            {
                gearSOName = stack.gear.gearName,
                quantity = stack.quantity
            });
        }

        foreach (var member in PartyManager.Instance.allMembers)
        {
            var gear = GearManager.Instance.GetGearFor(member.Name);
            data.equippedGear.Add(new EquippedGearSave
            {
                characterName = member.Name,
                weaponName = gear.weapon?.gearName,
                helmetName = gear.helmet?.gearName,
                torsoName = gear.torso?.gearName,
                legsName = gear.legs?.gearName,
                feetName = gear.feet?.gearName,
                ring1Name = gear.ring1?.gearName,
                ring2Name = gear.ring2?.gearName
            });
        }

        data.gold = GoldManager.Instance.gold;
        data.resonanceMeterUnlocked = ResonanceManager.MeterUnlocked;
        data.resonanceMeterValue = ResonanceManager.Instance != null ? ResonanceManager.Instance.currentMeter : 0f;

        // Capture every registered one-time-trigger PlayerPrefs flag into THIS save slot
        data.playerPrefsKeys = new List<string>();
        data.playerPrefsValues = new List<int>();

        foreach (var key in TrackedPlayerPrefsKeys.AllKnownKeys)
        {
            if (PlayerPrefs.HasKey(key))
            {
                data.playerPrefsKeys.Add(key);
                data.playerPrefsValues.Add(PlayerPrefs.GetInt(key));
            }
        }

        Debug.Log($"[SAVE] Captured {data.playerPrefsKeys.Count} PlayerPrefs flags into save");

        string json = JsonUtility.ToJson(data, true);
        File.WriteAllText(GetPath(slot), json);
        currentSlot = slot;
        Debug.Log($"[SAVE] Saved to {GetPath(slot)}");
    }

    public void LoadFromSlot(int slot)
    {
        string path = GetPath(slot);
        if (!File.Exists(path))
        {
            Debug.LogError($"[SAVE] No save found at slot {slot}");
            return;
        }

        string json = File.ReadAllText(path);
        var data = JsonUtility.FromJson<SaveData>(json);

        if (data == null)
        {
            Debug.LogError($"[SAVE] Failed to parse save data for slot {slot}");
            return;
        }

        data.sceneName = OverworldSceneName;

        Debug.Log($"[SAVE] Queued load for slot {slot}: {data.saveName}. Target scene: {data.sceneName}");
        currentSlot = slot;

        IsLoadingSave = true;
        pendingLoadData = data;
        UnityEngine.SceneManagement.SceneManager.LoadScene(data.sceneName);
    }

    IEnumerator ApplyLoadedDataDelayed(SaveData data)
    {
        Debug.Log("[SAVE] ApplyLoadedDataDelayed coroutine STARTED");

        yield return new WaitForEndOfFrame();
        yield return new WaitForEndOfFrame();

        yield return new WaitUntil(() => PartyManager.Instance != null);
        yield return new WaitUntil(() => GoldManager.Instance != null);
        yield return new WaitUntil(() => GearManager.Instance != null);
        yield return new WaitUntil(() => InventoryManager.Instance != null);

        Debug.Log("[SAVE] All managers confirmed ready, applying save data now");

        try
        {
            ApplyLoadedData(data);
        }
        catch (System.Exception e)
        {
            Debug.LogError($"[SAVE] EXCEPTION during ApplyLoadedData: {e}");
        }

        IsLoadingSave = false;
    }

    void ApplyLoadedData(SaveData data)
    {
        Debug.Log($"[SAVE] ApplyLoadedData ENTERED for: {data.saveName}");

        if (PartyManager.Instance == null)
        {
            Debug.LogError("[SAVE] ApplyLoadedData called but PartyManager.Instance is still null!");
            return;
        }

        sessionPlaytime = data.playtimeSeconds;

        PartyManager.Instance.allMembers.Clear();
        PartyManager.Instance.activeParty.Clear();

        foreach (var entry in data.allMembers)
        {
            var so = FindCharacterSO(entry.characterSOName);
            if (so == null)
            {
                Debug.LogWarning($"[SAVE] Could not find CharacterStatsSO: {entry.characterSOName}");
                continue;
            }

            var inst = new CharacterInstance { baseData = so };
            inst.Initialize();
            inst.level = entry.level;
            inst.currentXP = entry.currentXP;
            inst.xpToNextLevel = entry.xpToNextLevel;
            inst.currentHP = entry.currentHP;
            inst.currentMana = entry.currentMana;
            PartyManager.Instance.allMembers.Add(inst);
            Debug.Log($"[SAVE] Restored member: {inst.Name} Lv{inst.level} HP:{inst.currentHP}");
        }

        foreach (var name in data.activePartyNames)
        {
            var inst = PartyManager.Instance.allMembers.Find(m => m.Name == name);
            if (inst != null)
                PartyManager.Instance.activeParty.Add(inst);
        }

        Debug.Log($"[SAVE] Active party restored: {string.Join(",", PartyManager.Instance.activeParty.ConvertAll(m => m.Name))}");

        if (InventoryManager.Instance != null)
        {
            InventoryManager.Instance.items.Clear();
            foreach (var itemSave in data.inventoryItems)
            {
                var so = FindItemSO(itemSave.itemSOName);
                if (so == null)
                {
                    Debug.LogWarning($"[SAVE] Could not find ItemSO: {itemSave.itemSOName}");
                    continue;
                }
                InventoryManager.Instance.items.Add(new InventoryItem(so, itemSave.quantity));
            }
            Debug.Log($"[SAVE] Inventory restored: {InventoryManager.Instance.items.Count} item stacks");
        }

        if (GearManager.Instance != null)
        {
            GearManager.Instance.gearInventory.Clear();
            foreach (var gearSave in data.gearStacks)
            {
                var so = FindGearSO(gearSave.gearSOName);
                if (so == null) continue;
                GearManager.Instance.gearInventory.Add(new GearStack(so, gearSave.quantity));
            }

            GearManager.Instance.ClearAllEquipped();
            foreach (var eq in data.equippedGear)
            {
                var charGear = GearManager.Instance.GetGearFor(eq.characterName);
                charGear.weapon = FindGearSO(eq.weaponName);
                charGear.helmet = FindGearSO(eq.helmetName);
                charGear.torso = FindGearSO(eq.torsoName);
                charGear.legs = FindGearSO(eq.legsName);
                charGear.feet = FindGearSO(eq.feetName);
                charGear.ring1 = FindGearSO(eq.ring1Name);
                charGear.ring2 = FindGearSO(eq.ring2Name);
            }
            Debug.Log($"[SAVE] Gear restored: {GearManager.Instance.gearInventory.Count} stacks in inventory");
        }

        if (GoldManager.Instance != null)
        {
            GoldManager.Instance.SetGold(data.gold);
            Debug.Log($"[SAVE] Gold restored: {GoldManager.Instance.gold}");
        }

        ResonanceManager.MeterUnlocked = data.resonanceMeterUnlocked;
        if (ResonanceManager.Instance != null)
            ResonanceManager.Instance.currentMeter = data.resonanceMeterValue;

        // Restore PlayerPrefs flags to EXACTLY match this save slot.
        // Wipe every tracked key first, then re-apply only what this save had.
        foreach (var key in TrackedPlayerPrefsKeys.AllKnownKeys)
            PlayerPrefs.DeleteKey(key);

        if (data.playerPrefsKeys != null)
        {
            for (int i = 0; i < data.playerPrefsKeys.Count; i++)
                PlayerPrefs.SetInt(data.playerPrefsKeys[i], data.playerPrefsValues[i]);
        }
        PlayerPrefs.Save();

        Debug.Log($"[SAVE] Restored {data.playerPrefsKeys?.Count ?? 0} PlayerPrefs flags from save slot");

        var player = GameObject.FindGameObjectWithTag("Player");
        if (player != null)
        {
            player.transform.position = new Vector3(data.playerX, data.playerY, data.playerZ);
            Debug.Log($"[SAVE] Player position set to: {player.transform.position}");
        }
        else
        {
            Debug.LogWarning("[SAVE] Could not find Player GameObject to set position!");
        }

        EncounterManager.PlayerReturnPosition = Vector3.zero;

        Debug.Log($"[SAVE] *** LOAD COMPLETE *** Gold:{GoldManager.Instance?.gold} " +
            $"PartyCount:{PartyManager.Instance.activeParty.Count} " +
            $"Members:{string.Join(",", PartyManager.Instance.activeParty.ConvertAll(m => m.Name))}");
    }

    public void DeleteSlot(int slot)
    {
        string path = GetPath(slot);
        if (File.Exists(path))
        {
            File.Delete(path);
            Debug.Log($"[SAVE] Deleted slot {slot}");
        }
    }

    public string FormatPlaytime(float seconds)
    {
        int h = Mathf.FloorToInt(seconds / 3600f);
        int m = Mathf.FloorToInt((seconds % 3600f) / 60f);
        int s = Mathf.FloorToInt(seconds % 60f);

        if (h > 0)
            return $"{h:00}h {m:00}m";
        else
            return $"{m:00}m {s:00}s";
    }
}