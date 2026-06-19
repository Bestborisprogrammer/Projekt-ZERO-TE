using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;

public class SaveManager : MonoBehaviour
{
    private static SaveManager _instance;
    public static SaveManager Instance
    {
        get
        {
            if (_instance == null)
            {
                // Auto-create if it doesn't exist yet — no more manual scene placement needed
                var existing = FindFirstObjectByType<SaveManager>();
                if (existing != null)
                {
                    _instance = existing;
                }
                else
                {
                    GameObject obj = new GameObject("SaveManager (Auto-Created)");
                    _instance = obj.AddComponent<SaveManager>();
                    Debug.Log("[SAVE MANAGER] Auto-created since none existed in scene");
                }
            }
            return _instance;
        }
    }

    public const int MaxSlots = 10;
    public const int AutoSaveSlot = -1;

    [Header("Autosave")]
    public float autoSaveIntervalSeconds = 600f;
    private float autoSaveTimer = 0f;

    // No longer manually assigned — auto-populated from Resources folders
    private List<CharacterStatsSO> allCharacterSOs = new();
    private List<ItemSO> allItemSOs = new();
    private List<GearSO> allGearSOs = new();
    private bool databaseLoaded = false;

    public float sessionPlaytime = 0f;
    public int currentSlot = -2;

    private static SaveData pendingLoadData = null;
    public static bool IsLoadingSave { get; private set; } = false;

    void Awake()
    {
        if (_instance == null)
        {
            _instance = this;
            DontDestroyOnLoad(gameObject);
            Debug.Log("[SAVE MANAGER] Awake - Instance set");
        }
        else if (_instance != this)
        {
            Debug.Log("[SAVE MANAGER] Duplicate destroyed");
            Destroy(gameObject);
            return;
        }

        LoadDatabase();
    }

    // Auto-loads EVERY SO of each type from anywhere in a "Resources" folder.
    // No more manually dragging 1000 items into a list.
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

        if (UnityEngine.SceneManagement.SceneManager.GetActiveScene().name == "OverworldScene")
        {
            if (pendingLoadData != null)
            {
                Debug.Log("[SAVE] pendingLoadData found in OverworldScene, applying now");
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

    public bool SlotExists(int slot)
    {
        return File.Exists(GetPath(slot));
    }

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

    public CharacterStatsSO FindCharacterSO(string name) =>
        allCharacterSOs.Find(c => c.characterName == name);
    public ItemSO FindItemSO(string name) =>
        allItemSOs.Find(i => i.itemName == name);
    public GearSO FindGearSO(string name) =>
        allGearSOs.Find(g => g.gearName == name);

    public void SaveToSlot(int slot, string customName = null)
    {
        Debug.Log($"[SAVE] Saving to slot {slot}");

        if (PartyManager.Instance == null)
        {
            Debug.LogError("[SAVE] PartyManager.Instance is null – cannot save outside OverworldScene!");
            return;
        }

        var data = new SaveData();
        data.isEmpty = false;
        data.saveName = customName ?? $"Edward's Save Slot {slot + 1}";
        data.playtimeSeconds = sessionPlaytime;
        data.dateTime = System.DateTime.Now.ToString("yyyy-MM-dd HH:mm");

        var player = GameObject.FindGameObjectWithTag("Player");
        data.sceneName = "OverworldScene";
        if (player != null)
        {
            data.playerX = player.transform.position.x;
            data.playerY = player.transform.position.y;
            data.playerZ = player.transform.position.z;
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

        Debug.Log($"[SAVE] Queued load for slot {slot}: {data.saveName}");
        currentSlot = slot;

        IsLoadingSave = true;
        pendingLoadData = data;
        UnityEngine.SceneManagement.SceneManager.LoadScene(data.sceneName);
    }

    IEnumerator ApplyLoadedDataDelayed(SaveData data)
    {
        // Wait several frames to make ABSOLUTELY sure every Awake/Start
        // (including GameInitializer) has fully finished before we overwrite.
        yield return null;
        yield return null;
        yield return null;
        yield return new WaitUntil(() => PartyManager.Instance != null);

        ApplyLoadedData(data);
        IsLoadingSave = false;
    }

    void ApplyLoadedData(SaveData data)
    {
        if (PartyManager.Instance == null)
        {
            Debug.LogError("[SAVE] ApplyLoadedData called but PartyManager.Instance is still null!");
            return;
        }

        sessionPlaytime = data.playtimeSeconds;
        Debug.Log($"[SAVE] Applying loaded data: {data.saveName}");

        PartyManager.Instance.allMembers.Clear();
        PartyManager.Instance.activeParty.Clear();

        foreach (var entry in data.allMembers)
        {
            var so = FindCharacterSO(entry.characterSOName);
            if (so == null)
            {
                Debug.LogWarning($"[SAVE] Could not find CharacterStatsSO: {entry.characterSOName} " +
                    $"- make sure it's inside a 'Resources' folder!");
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
        }

        foreach (var name in data.activePartyNames)
        {
            var inst = PartyManager.Instance.allMembers.Find(m => m.Name == name);
            if (inst != null)
                PartyManager.Instance.activeParty.Add(inst);
        }

        Debug.Log($"[SAVE] Party restored: {string.Join(",", PartyManager.Instance.activeParty.ConvertAll(m => m.Name))}");

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
        }

        if (GoldManager.Instance != null)
            GoldManager.Instance.SetGold(data.gold);

        ResonanceManager.MeterUnlocked = data.resonanceMeterUnlocked;
        if (ResonanceManager.Instance != null)
            ResonanceManager.Instance.currentMeter = data.resonanceMeterValue;

        var player = GameObject.FindGameObjectWithTag("Player");
        if (player != null)
            player.transform.position = new Vector3(data.playerX, data.playerY, data.playerZ);

        EncounterManager.PlayerReturnPosition = Vector3.zero;

        Debug.Log($"[SAVE] Load complete! Gold:{GoldManager.Instance?.gold} " +
            $"PartyCount:{PartyManager.Instance.activeParty.Count}");
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
        return $"{h:00}h {m:00}m";
    }
}