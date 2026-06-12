using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class ResonanceCutsceneManager : MonoBehaviour
{
    [Header("Dialogues")]
    public DialogueSO resonanceDialogue;
    public DialogueSO postBattleDialogue;
    public DialogueSO knockoutDialogue;
    public DialogueSO afterDuelDialogue;

    [Header("Resonance Battle Enemy")]
    public EnemyStatsSO resonanceEnemy;

    [Header("Duel")]
    public CharacterStatsSO hiroseStats;
    public EnemyStatsSO hiroseAsDuelEnemy;

    [Header("Wake Up Location")]
    public Transform wakeUpLocation;

    [Header("Trigger")]
    public GameObject triggerObject;

    private bool started = false;
    private string saveKey;

    // Static flags that persist across scenes
    public static bool WaitingForResonanceBattleReturn { get; set; } = false;
    public static bool WaitingForDuelReturn { get; set; } = false;

    void Start()
    {
        saveKey = $"resonance_cs_{gameObject.name}";

        Debug.Log($"[RESONANCE CS START] WaitingForResonanceBattleReturn={WaitingForResonanceBattleReturn}" +
            $" WaitingForDuelReturn={WaitingForDuelReturn}");

        // Force hide overlay on scene load
        ResonanceManager.Instance?.ForceHideOverlay();

        if (PlayerPrefs.GetInt(saveKey, 0) == 1)
        {
            if (triggerObject != null)
                triggerObject.SetActive(false);
        }

        // Check if returning from resonance battle
        if (WaitingForResonanceBattleReturn)
        {
            WaitingForResonanceBattleReturn = false;
            Debug.Log("[RESONANCE CS] Returned from resonance battle – starting post sequence");
            StartCoroutine(PostResonanceBattleSequence());
            return;
        }

        // Check if returning from duel
        if (WaitingForDuelReturn)
        {
            WaitingForDuelReturn = false;
            Debug.Log("[RESONANCE CS] Returned from duel – starting blackout sequence");
            StartCoroutine(PostDuelSequence());
            return;
        }
    }

    public void StartResonanceCutscene()
    {
        if (started) return;
        if (PlayerPrefs.GetInt(saveKey, 0) == 1) return;
        started = true;

        Debug.Log("[RESONANCE CS] Starting initial sequence");
        PlayerPrefs.SetInt(saveKey, 1);
        PlayerPrefs.Save();

        if (triggerObject != null)
            triggerObject.SetActive(false);

        StartCoroutine(InitialResonanceSequence());
    }

    IEnumerator InitialResonanceSequence()
    {
        SetPlayerFrozen(true);
        yield return new WaitForSeconds(0.3f);

        Debug.Log("[RESONANCE CS] Starting violent flash");
        bool flashDone = false;
        StartCoroutine(ResonanceManager.Instance.TriggerResonanceFlash(() => flashDone = true));
        yield return new WaitUntil(() => flashDone);

        yield return new WaitForSeconds(0.3f);

        // Resonance dialogue
        if (resonanceDialogue != null)
        {
            Debug.Log("[RESONANCE CS] Playing resonance dialogue");
            bool done = false;
            DialogueUI.Instance.StartDialogue(resonanceDialogue, () => done = true);
            yield return new WaitUntil(() => done);
        }

        // Activate resonance
        ResonanceManager.Instance.ActivateResonance(scripted: true);
        yield return new WaitForSeconds(0.3f);

        // Set flag BEFORE starting encounter so it persists
        WaitingForResonanceBattleReturn = true;
        Debug.Log($"[RESONANCE CS] WaitingForResonanceBattleReturn={WaitingForResonanceBattleReturn}");

        EncounterManager.IsResonanceBattle = true;
        EncounterManager.Instance.StartEncounter(
            new List<EnemyStatsSO> { resonanceEnemy });
    }

    IEnumerator PostResonanceBattleSequence()
    {
        Debug.Log("[RESONANCE CS] PostResonanceBattle started");
        yield return new WaitForSeconds(0.8f);
        yield return new WaitUntil(() => DialogueUI.Instance != null);

        SetPlayerFrozen(true);

        // Post battle dialogue
        if (postBattleDialogue != null)
        {
            Debug.Log("[RESONANCE CS] Playing post battle dialogue");
            bool done = false;
            DialogueUI.Instance.StartDialogue(postBattleDialogue, () => done = true);
            yield return new WaitUntil(() => done);
        }

        // Knockout dialogue
        if (knockoutDialogue != null)
        {
            Debug.Log("[RESONANCE CS] Playing knockout dialogue");
            bool done = false;
            DialogueUI.Instance.StartDialogue(knockoutDialogue, () => done = true);
            yield return new WaitUntil(() => done);
        }

        yield return new WaitForSeconds(0.3f);

        // Remove Hirose from active party for duel
        CharacterInstance hiroseInstance = null;
        if (hiroseStats != null)
        {
            hiroseInstance = PartyManager.Instance.activeParty
                .Find(m => m.baseData == hiroseStats);
            if (hiroseInstance != null)
            {
                PartyManager.Instance.activeParty.Remove(hiroseInstance);
                Debug.Log("[RESONANCE CS] Hirose removed for duel");
            }
            else
                Debug.LogWarning("[RESONANCE CS] Could not find Hirose in active party!");
        }

        // Store Hirose for after duel
        HiroseTemporarilyRemoved = hiroseInstance;

        // Set flag before starting duel
        WaitingForDuelReturn = true;
        Debug.Log($"[RESONANCE CS] WaitingForDuelReturn={WaitingForDuelReturn}");

        EncounterManager.IsForcedLossBattle = true;
        EncounterManager.Instance.StartEncounter(
            new List<EnemyStatsSO> { hiroseAsDuelEnemy });
    }

    // Store hirose across scene loads
    private static CharacterInstance HiroseTemporarilyRemoved = null;

    IEnumerator PostDuelSequence()
    {
        Debug.Log("[RESONANCE CS] PostDuel sequence started");
        yield return new WaitForSeconds(0.5f);
        yield return new WaitUntil(() => ResonanceManager.Instance != null);

        SetPlayerFrozen(true);

        // Black out
        Debug.Log("[RESONANCE CS] Starting blackout");
        bool blackOutDone = false;
        StartCoroutine(ResonanceManager.Instance.BlackOut(() => blackOutDone = true));
        yield return new WaitUntil(() => blackOutDone);

        // Deactivate resonance
        ResonanceManager.Instance.DeactivateResonance();

        // Re-add Hirose
        if (HiroseTemporarilyRemoved != null)
        {
            if (!PartyManager.Instance.activeParty.Contains(HiroseTemporarilyRemoved))
                PartyManager.Instance.activeParty.Add(HiroseTemporarilyRemoved);
            Debug.Log("[RESONANCE CS] Hirose re-added to party");
            HiroseTemporarilyRemoved = null;
        }

        // Teleport player to wake up location
        var player = GameObject.FindGameObjectWithTag("Player");
        if (player != null && wakeUpLocation != null)
        {
            player.transform.position = wakeUpLocation.position;
            EncounterManager.PlayerReturnPosition = Vector3.zero;
            Debug.Log($"[RESONANCE CS] Player teleported to {wakeUpLocation.position}");
        }

        // Unlock resonance meter
        ResonanceManager.MeterUnlocked = true;
        Debug.Log("[RESONANCE CS] Resonance meter unlocked");

        // Fade in
        Debug.Log("[RESONANCE CS] Starting fade in");
        bool fadeInDone = false;
        StartCoroutine(ResonanceManager.Instance.FadeIn(() => fadeInDone = true));
        yield return new WaitUntil(() => fadeInDone);

        yield return new WaitForSeconds(0.3f);
        yield return new WaitUntil(() => DialogueUI.Instance != null);

        // Wake up dialogue
        if (afterDuelDialogue != null)
        {
            Debug.Log("[RESONANCE CS] Playing after duel dialogue");
            bool done = false;
            DialogueUI.Instance.StartDialogue(afterDuelDialogue, () => done = true);
            yield return new WaitUntil(() => done);
        }

        SetPlayerFrozen(false);
        Debug.Log("[RESONANCE CS] Full sequence complete!");
    }

    void SetPlayerFrozen(bool frozen)
    {
        var player = GameObject.FindGameObjectWithTag("Player");
        if (player == null) return;
        var movement = player.GetComponent<PlayerMovement2D>();
        if (movement != null) movement.enabled = !frozen;
        var rb = player.GetComponent<Rigidbody2D>();
        if (rb != null && frozen) rb.linearVelocity = Vector2.zero;
    }
}