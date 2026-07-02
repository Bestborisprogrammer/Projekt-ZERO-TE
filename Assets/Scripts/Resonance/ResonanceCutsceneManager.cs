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

    public static bool WaitingForResonanceBattleReturn { get; set; } = false;
    public static bool WaitingForDuelReturn { get; set; } = false;
    private static CharacterInstance hiroseStoredInstance = null;

    void Awake()
    {
        saveKey = $"resonance_cs_{gameObject.name}";
        TrackedPlayerPrefsKeys.Register(saveKey);
        Debug.Log($"[RESONANCE CS] Awake - saveKey: {saveKey}");
    }

    void Start()
    {
        int flagValue = PlayerPrefs.GetInt(saveKey, 0);
        Debug.Log($"[RESONANCE CS] Start - {saveKey} = {flagValue}. " +
            $"WaitingForResonance={WaitingForResonanceBattleReturn} " +
            $"WaitingForDuel={WaitingForDuelReturn}");

        ResonanceManager.Instance?.ForceHideOverlay();

        if (flagValue == 1 && triggerObject != null)
            triggerObject.SetActive(false);

        if (WaitingForResonanceBattleReturn)
        {
            WaitingForResonanceBattleReturn = false;
            PlayerMovement2D.ForceFrozen = true;
            Debug.Log("[RESONANCE CS] Returned from resonance battle");
            StartCoroutine(PostResonanceBattleSequence());
            return;
        }

        if (WaitingForDuelReturn)
        {
            WaitingForDuelReturn = false;
            PlayerMovement2D.ForceFrozen = true;
            Debug.Log("[RESONANCE CS] Returned from duel");
            StartCoroutine(PostDuelSequence());
        }
    }

    public void StartResonanceCutscene()
    {
        if (started) return;
        if (PlayerPrefs.GetInt(saveKey, 0) == 1)
        {
            Debug.Log("[RESONANCE CS] Already completed - not starting again");
            return;
        }

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

        ResonanceManager.Instance.ShowResonanceTint();

        WaitingForResonanceBattleReturn = true;
        EncounterManager.IsResonanceBattle = true;
        Debug.Log("[RESONANCE CS] Starting resonance battle");
        EncounterManager.Instance.StartEncounter(new List<EnemyStatsSO> { resonanceEnemy });
    }

    IEnumerator PostResonanceBattleSequence()
    {
        Debug.Log("[RESONANCE CS] PostResonanceBattle");
        yield return new WaitForSeconds(0.8f);
        yield return new WaitUntil(() => DialogueUI.Instance != null);

        SetPlayerFrozen(true);

        if (postBattleDialogue != null)
        {
            bool done = false;
            DialogueUI.Instance.StartDialogue(postBattleDialogue, () => done = true);
            yield return new WaitUntil(() => done);
        }

        yield return new WaitForSeconds(0.5f);

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

        ResonanceManager.Instance.ShowResonanceTint();

        WaitingForDuelReturn = true;
        EncounterManager.IsForcedLossBattle = true;
        Debug.Log("[RESONANCE CS] Starting duel");
        EncounterManager.Instance.StartEncounter(new List<EnemyStatsSO> { hiroseAsDuelEnemy });
    }

    IEnumerator PostDuelSequence()
    {
        Debug.Log("[RESONANCE CS] PostDuel");
        ResonanceManager.Instance?.ForceHideOverlay();

        yield return new WaitForSeconds(0.5f);
        yield return new WaitUntil(() => ResonanceManager.Instance != null);

        SetPlayerFrozen(true);

        if (knockoutDialogue != null)
        {
            bool done = false;
            yield return new WaitUntil(() => DialogueUI.Instance != null);
            DialogueUI.Instance.StartDialogue(knockoutDialogue, () => done = true);
            yield return new WaitUntil(() => done);
        }

        bool blackOutDone = false;
        StartCoroutine(ResonanceManager.Instance.BlackOut(() => blackOutDone = true));
        yield return new WaitUntil(() => blackOutDone);

        ResonanceManager.Instance.DeactivateResonance();

        if (hiroseStoredInstance != null)
        {
            if (!PartyManager.Instance.activeParty.Contains(hiroseStoredInstance))
                PartyManager.Instance.activeParty.Add(hiroseStoredInstance);
            hiroseStoredInstance = null;
            Debug.Log("[RESONANCE CS] Hirose re-added");
        }

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

        if (afterDuelDialogue != null)
        {
            bool done = false;
            DialogueUI.Instance.StartDialogue(afterDuelDialogue, () => done = true);
            yield return new WaitUntil(() => done);
        }

        SetPlayerFrozen(false);
        PlayerMovement2D.ForceFrozen = false;
        Debug.Log("[RESONANCE CS] Complete!");
    }

    void SetPlayerFrozen(bool frozen)
    {
        PlayerMovement2D.ForceFrozen = frozen;
        var player = GameObject.FindGameObjectWithTag("Player");
        if (player == null) return;
        var movement = player.GetComponent<PlayerMovement2D>();
        if (movement != null) movement.enabled = !frozen;
        var rb = player.GetComponent<Rigidbody2D>();
        if (rb != null && frozen) rb.linearVelocity = Vector2.zero;
    }
}