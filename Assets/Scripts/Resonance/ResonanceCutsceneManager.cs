using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class ResonanceCutsceneManager : MonoBehaviour
{
    [Header("Dialogues")]
    public DialogueSO resonanceDialogue;
    public DialogueSO postBattleDialogue;    // Hirose worried, plays after resonance battle
    public DialogueSO knockoutDialogue;      // Hirose knocks Edward out, plays before duel
    public DialogueSO afterDuelDialogue;     // Edward wakes up in hut

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

    public static bool WaitingForResonanceBattleReturn { get; set; } = false;
    public static bool WaitingForDuelReturn { get; set; } = false;
    private static CharacterInstance hiroseStoredInstance = null;

    void Start()
    {
        saveKey = $"resonance_cs_{gameObject.name}";

        Debug.Log($"[RESONANCE CS] Start. WaitingForResonance={WaitingForResonanceBattleReturn} WaitingForDuel={WaitingForDuelReturn}");

        ResonanceManager.Instance?.ForceHideOverlay();

        if (PlayerPrefs.GetInt(saveKey, 0) == 1)
            if (triggerObject != null)
                triggerObject.SetActive(false);

        if (WaitingForResonanceBattleReturn)
        {
            WaitingForResonanceBattleReturn = false;
            // Keep player frozen immediately on scene load
            SetPlayerFrozen(true);
            Debug.Log("[RESONANCE CS] Returned from resonance battle – freezing player");
            StartCoroutine(PostResonanceBattleSequence());
            return;
        }

        if (WaitingForDuelReturn)
        {
            WaitingForDuelReturn = false;
            // Keep player frozen immediately on scene load
            SetPlayerFrozen(true);
            Debug.Log("[RESONANCE CS] Returned from duel – freezing player");
            StartCoroutine(PostDuelSequence());
        }
    }

    public void StartResonanceCutscene()
    {
        if (started) return;
        if (PlayerPrefs.GetInt(saveKey, 0) == 1) return;
        started = true;

        Debug.Log("[RESONANCE CS] Starting");
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

        bool flashDone = false;
        StartCoroutine(ResonanceManager.Instance.TriggerResonanceFlash(() => flashDone = true));
        yield return new WaitUntil(() => flashDone);

        yield return new WaitForSeconds(0.3f);

        if (resonanceDialogue != null)
        {
            bool done = false;
            DialogueUI.Instance.StartDialogue(resonanceDialogue, () => done = true);
            yield return new WaitUntil(() => done);
        }

        ResonanceManager.Instance.ActivateResonance(scripted: true);
        yield return new WaitForSeconds(0.3f);

        // Show purple tint for resonance battle
        ResonanceManager.Instance.ShowResonanceTint();

        WaitingForResonanceBattleReturn = true;
        EncounterManager.IsResonanceBattle = true;
        Debug.Log("[RESONANCE CS] Starting resonance battle");
        EncounterManager.Instance.StartEncounter(
            new List<EnemyStatsSO> { resonanceEnemy });
    }

    IEnumerator PostResonanceBattleSequence()
    {
        Debug.Log("[RESONANCE CS] PostResonanceBattle");
        yield return new WaitForSeconds(0.8f);
        yield return new WaitUntil(() => DialogueUI.Instance != null);

        SetPlayerFrozen(true);

        // Hirose worried
        if (postBattleDialogue != null)
        {
            bool done = false;
            DialogueUI.Instance.StartDialogue(postBattleDialogue, () => done = true);
            yield return new WaitUntil(() => done);
        }

        yield return new WaitForSeconds(0.5f);

        // Remove Hirose for duel
        if (hiroseStats != null)
        {
            hiroseStoredInstance = PartyManager.Instance.activeParty
                .Find(m => m.baseData == hiroseStats);
            if (hiroseStoredInstance != null)
            {
                PartyManager.Instance.activeParty.Remove(hiroseStoredInstance);
                Debug.Log("[RESONANCE CS] Hirose removed for duel");
            }
        }

        // Keep resonating
        ResonanceManager.Instance.ShowResonanceTint();

        WaitingForDuelReturn = true;
        EncounterManager.IsForcedLossBattle = true;
        Debug.Log("[RESONANCE CS] Starting duel");
        EncounterManager.Instance.StartEncounter(
            new List<EnemyStatsSO> { hiroseAsDuelEnemy });
    }

    IEnumerator PostDuelSequence()
    {
        Debug.Log("[RESONANCE CS] PostDuel");
        ResonanceManager.Instance?.ForceHideOverlay();

        yield return new WaitForSeconds(0.5f);
        yield return new WaitUntil(() => ResonanceManager.Instance != null);

        SetPlayerFrozen(true);

        // Knockout dialogue plays HERE – after Edward loses
        if (knockoutDialogue != null)
        {
            Debug.Log("[RESONANCE CS] Playing knockout dialogue");
            bool done = false;
            yield return new WaitUntil(() => DialogueUI.Instance != null);
            DialogueUI.Instance.StartDialogue(knockoutDialogue, () => done = true);
            yield return new WaitUntil(() => done);
        }

        // Black out
        bool blackOutDone = false;
        StartCoroutine(ResonanceManager.Instance.BlackOut(() => blackOutDone = true));
        yield return new WaitUntil(() => blackOutDone);

        ResonanceManager.Instance.DeactivateResonance();

        // Re-add Hirose
        if (hiroseStoredInstance != null)
        {
            if (!PartyManager.Instance.activeParty.Contains(hiroseStoredInstance))
                PartyManager.Instance.activeParty.Add(hiroseStoredInstance);
            hiroseStoredInstance = null;
            Debug.Log("[RESONANCE CS] Hirose re-added");
        }

        // Teleport
        var player = GameObject.FindGameObjectWithTag("Player");
        if (player != null && wakeUpLocation != null)
        {
            player.transform.position = wakeUpLocation.position;
            EncounterManager.PlayerReturnPosition = Vector3.zero;
        }

        ResonanceManager.MeterUnlocked = true;

        bool fadeInDone = false;
        StartCoroutine(ResonanceManager.Instance.FadeIn(() => fadeInDone = true));
        yield return new WaitUntil(() => fadeInDone);

        yield return new WaitForSeconds(0.3f);
        yield return new WaitUntil(() => DialogueUI.Instance != null);

        // Wake up dialogue
        if (afterDuelDialogue != null)
        {
            bool done = false;
            DialogueUI.Instance.StartDialogue(afterDuelDialogue, () => done = true);
            yield return new WaitUntil(() => done);
        }

        SetPlayerFrozen(false);
        // Also clear the static flag
        PlayerMovement2D.ForceFrozen = false;
        Debug.Log("[RESONANCE CS] Player unfrozen after full sequence");
    }

    void SetPlayerFrozen(bool frozen)
    {
        // Use static flag so it persists across scene reloads
        PlayerMovement2D.ForceFrozen = frozen;
        Debug.Log($"[RESONANCE CS] ForceFrozen={frozen}");

        var player = GameObject.FindGameObjectWithTag("Player");
        if (player == null) return;

        var movement = player.GetComponent<PlayerMovement2D>();
        if (movement != null)
            movement.enabled = !frozen;

        var rb = player.GetComponent<Rigidbody2D>();
        if (rb != null && frozen)
            rb.linearVelocity = Vector2.zero;
    }
}