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
        Debug.Log("[SAVE MANAGER] Bootstrapped");
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
            Destroy(gameObject);
    }

    void LoadDatabase()
    {
        if (databaseLoaded) return;
        allCharacterSOs = Resources.LoadAll<CharacterStatsSO>("").ToList();
        allItemSOs = Resources.LoadAll<ItemSO>("").ToList();
        allGearSOs = Resources.LoadAll<GearSO>("").ToList();
        Debug.Log($"[SAVE MANAGER] Database: {allCharacterSOs.Count} chars, {allItemSOs.Count} items, {allGearSOs.Count} gear");
        databaseLoaded = true;
    }

    void Update()
    {
        sessionPlaytime += Time.deltaTime;

        if (UnityEngine.SceneManagement.SceneManager.GetActiveScene().name == OverworldSceneName)
        {
            if (pendingLoadData != null)
            {
                Debug.Log("[SAVE] pendingLoadData detected - starting apply coroutine");
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
            return JsonUtility.FromJson<SaveData>(File.ReadAllText(path));
        }
        catch (System.Exception e)
        {
            Debug.LogError($"[SAVE] Preview failed slot {slot}: {e.Message}");
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
            Debug.LogError("[SAVE] PartyManager null - cannot save!");
            return;
        }

        var data = new SaveData();
        data.isEmpty = false;
        data.saveName = customName ?? $"Edward's Save Slot {slot + 1}";
        data.playtimeSeconds = sessionPlaytime;
        data.dateTime = System.DateTime.Now.ToString("yyyy-MM-dd HH:mm");
        data.sceneName = OverworldSceneName;

        var player = GameObject.FindGameObjectWithTag("Player");
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
            Debug.Log($"[SAVE] Position from cache: {data.playerX},{data.playerY},{data.playerZ}");
        }

        foreach (var member in PartyManager.Instance.allMembers)
            data.allMembers.Add(new CharacterSaveEntry
            {
                characterSOName = member.baseData.characterName,
                level = member.level,
                currentXP = member.currentXP,
                xpToNextLevel = member.xpToNextLevel,
                currentHP = member.currentHP,
                currentMana = member.currentMana
            });

        foreach (var active in PartyManager.Instance.activeParty)
            data.activePartyNames.Add(active.baseData.characterName);

        foreach (var item in InventoryManager.Instance.items)
            data.inventoryItems.Add(new InventoryItemSave
            { itemSOName = item.itemData.itemName, quantity = item.quantity });

        foreach (var stack in GearManager.Instance.gearInventory)
            data.gearStacks.Add(new GearStackSave
            { gearSOName = stack.gear.gearName, quantity = stack.quantity });

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

        // Capture PlayerPrefs story flags into this save slot
        data.playerPrefsKeys = new List<string>();
        data.playerPrefsValues = new List<int>();

        var allTracked = TrackedPlayerPrefsKeys.GetAllTrackedKeys();
        Debug.Log($"[SAVE] Capturing {allTracked.Count} tracked keys");
        foreach (var key in allTracked)
        {
            if (PlayerPrefs.HasKey(key))
            {
                int val = PlayerPrefs.GetInt(key, 0);
                data.playerPrefsKeys.Add(key);
                data.playerPrefsValues.Add(val);
                Debug.Log($"[SAVE] Flag: {key} = {val}");
            }
        }
        Debug.Log($"[SAVE] Captured {data.playerPrefsKeys.Count} flags");

        File.WriteAllText(GetPath(slot), JsonUtility.ToJson(data, true));
        currentSlot = slot;
        Debug.Log($"[SAVE] Done: {GetPath(slot)}");
    }

    public void LoadFromSlot(int slot)
    {
        string path = GetPath(slot);
        if (!File.Exists(path))
        {
            Debug.LogError($"[SAVE] No save at slot {slot}");
            return;
        }

        var data = JsonUtility.FromJson<SaveData>(File.ReadAllText(path));
        if (data == null)
        {
            Debug.LogError($"[SAVE] Parse failed slot {slot}");
            return;
        }

        data.sceneName = OverworldSceneName;
        Debug.Log($"[SAVE] Queued load slot {slot}: {data.saveName}");
        currentSlot = slot;

        IsLoadingSave = true;
        pendingLoadData = data;
        UnityEngine.SceneManagement.SceneManager.LoadScene(data.sceneName);
    }

    IEnumerator ApplyLoadedDataDelayed(SaveData data)
    {
        Debug.Log("[SAVE] ApplyLoadedDataDelayed started");

        yield return new WaitForEndOfFrame();
        yield return new WaitForEndOfFrame();

        yield return new WaitUntil(() => PartyManager.Instance != null);
        yield return new WaitUntil(() => GoldManager.Instance != null);
        yield return new WaitUntil(() => GearManager.Instance != null);
        yield return new WaitUntil(() => InventoryManager.Instance != null);

        Debug.Log("[SAVE] All managers ready - applying");

        try { ApplyLoadedData(data); }
        catch (System.Exception e) { Debug.LogError($"[SAVE] Exception: {e}"); }

        IsLoadingSave = false;
    }

    void ApplyLoadedData(SaveData data)
    {
        Debug.Log($"[SAVE] ApplyLoadedData: {data.saveName}");

        if (PartyManager.Instance == null)
        {
            Debug.LogError("[SAVE] PartyManager null in ApplyLoadedData!");
            return;
        }

        sessionPlaytime = data.playtimeSeconds;

        PartyManager.Instance.allMembers.Clear();
        PartyManager.Instance.activeParty.Clear();

        foreach (var entry in data.allMembers)
        {
            var so = FindCharacterSO(entry.characterSOName);
            if (so == null) { Debug.LogWarning($"[SAVE] Missing SO: {entry.characterSOName}"); continue; }
            var inst = new CharacterInstance { baseData = so };
            inst.Initialize();
            inst.level = entry.level;
            inst.currentXP = entry.currentXP;
            inst.xpToNextLevel = entry.xpToNextLevel;
            inst.currentHP = entry.currentHP;
            inst.currentMana = entry.currentMana;
            PartyManager.Instance.allMembers.Add(inst);
            Debug.Log($"[SAVE] Restored: {inst.Name} Lv{inst.level} HP:{inst.currentHP}");
        }

        foreach (var name in data.activePartyNames)
        {
            var inst = PartyManager.Instance.allMembers.Find(m => m.Name == name);
            if (inst != null) PartyManager.Instance.activeParty.Add(inst);
        }

        Debug.Log($"[SAVE] Party: {string.Join(",", PartyManager.Instance.activeParty.ConvertAll(m => m.Name))}");

        if (InventoryManager.Instance != null)
        {
            InventoryManager.Instance.items.Clear();
            foreach (var s in data.inventoryItems)
            {
                var so = FindItemSO(s.itemSOName);
                if (so == null) continue;
                InventoryManager.Instance.items.Add(new InventoryItem(so, s.quantity));
            }
            Debug.Log($"[SAVE] Inventory: {InventoryManager.Instance.items.Count} stacks");
        }

        if (GearManager.Instance != null)
        {
            GearManager.Instance.gearInventory.Clear();
            foreach (var s in data.gearStacks)
            {
                var so = FindGearSO(s.gearSOName);
                if (so == null) continue;
                GearManager.Instance.gearInventory.Add(new GearStack(so, s.quantity));
            }

            GearManager.Instance.ClearAllEquipped();
            foreach (var eq in data.equippedGear)
            {
                var cg = GearManager.Instance.GetGearFor(eq.characterName);
                cg.weapon = FindGearSO(eq.weaponName);
                cg.helmet = FindGearSO(eq.helmetName);
                cg.torso = FindGearSO(eq.torsoName);
                cg.legs = FindGearSO(eq.legsName);
                cg.feet = FindGearSO(eq.feetName);
                cg.ring1 = FindGearSO(eq.ring1Name);
                cg.ring2 = FindGearSO(eq.ring2Name);
            }
            Debug.Log($"[SAVE] Gear: {GearManager.Instance.gearInventory.Count} stacks");
        }

        if (GoldManager.Instance != null)
        {
            GoldManager.Instance.SetGold(data.gold);
            Debug.Log($"[SAVE] Gold: {GoldManager.Instance.gold}");
        }

        ResonanceManager.MeterUnlocked = data.resonanceMeterUnlocked;
        if (ResonanceManager.Instance != null)
            ResonanceManager.Instance.currentMeter = data.resonanceMeterValue;

        // Restore PlayerPrefs story flags to EXACTLY what they were at save time
        var allTracked = TrackedPlayerPrefsKeys.GetAllTrackedKeys();
        Debug.Log($"[SAVE] Wiping {allTracked.Count} tracked keys before restore");
        foreach (var key in allTracked)
        {
            if (PlayerPrefs.HasKey(key))
            {
                Debug.Log($"[SAVE] Wiping: {key} (was {PlayerPrefs.GetInt(key, 0)})");
                PlayerPrefs.DeleteKey(key);
            }
        }

        int restored = 0;
        if (data.playerPrefsKeys != null)
        {
            for (int i = 0; i < data.playerPrefsKeys.Count; i++)
            {
                PlayerPrefs.SetInt(data.playerPrefsKeys[i], data.playerPrefsValues[i]);
                Debug.Log($"[SAVE] Restored flag: {data.playerPrefsKeys[i]} = {data.playerPrefsValues[i]}");
                restored++;
            }
        }
        PlayerPrefs.Save();
        Debug.Log($"[SAVE] PlayerPrefs: {restored} flags restored");

        var player = GameObject.FindGameObjectWithTag("Player");
        if (player != null)
        {
            player.transform.position = new Vector3(data.playerX, data.playerY, data.playerZ);
            Debug.Log($"[SAVE] Position: {player.transform.position}");
        }

        EncounterManager.PlayerReturnPosition = Vector3.zero;
        Debug.Log($"[SAVE] *** LOAD COMPLETE *** Gold:{GoldManager.Instance?.gold} Party:{PartyManager.Instance.activeParty.Count}");
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
        return h > 0 ? $"{h:00}h {m:00}m" : $"{m:00}m {s:00}s";
    }
}