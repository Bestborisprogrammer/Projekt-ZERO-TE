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

    [Header("Wake Up")]
    public Transform wakeUpLocation;

    [Header("Trigger")]
    public GameObject triggerObject;

    private bool started = false;
    private string saveKey;

    void Start()
    {
        saveKey = $"resonance_cs_{gameObject.name}";
        if (PlayerPrefs.GetInt(saveKey, 0) == 1)
            if (triggerObject != null)
                triggerObject.SetActive(false);
    }

    public void StartResonanceCutscene()
    {
        if (started) return;
        if (PlayerPrefs.GetInt(saveKey, 0) == 1) return;
        started = true;

        Debug.Log("[RESONANCE CS] Starting sequence");
        PlayerPrefs.SetInt(saveKey, 1);
        PlayerPrefs.Save();

        if (triggerObject != null)
            triggerObject.SetActive(false);

        StartCoroutine(ResonanceSequence());
    }

    IEnumerator ResonanceSequence()
    {
        SetPlayerFrozen(true);
        yield return new WaitForSeconds(0.3f);

        // Violent flash
        yield return StartCoroutine(
            ResonanceManager.Instance.TriggerResonanceFlash());

        yield return new WaitForSeconds(0.3f);

        // Resonance dialogue
        if (resonanceDialogue != null)
        {
            bool done = false;
            DialogueUI.Instance.StartDialogue(resonanceDialogue, () => done = true);
            yield return new WaitUntil(() => done);
        }

        // Activate resonance
        ResonanceManager.Instance.ActivateResonance(scripted: true);
        yield return new WaitForSeconds(0.3f);

        // Start resonance battle
        Debug.Log("[RESONANCE CS] Starting resonance battle");
        EncounterManager.IsResonanceBattle = true;
        EncounterManager.ResonanceBattleDone = false;
        EncounterManager.Instance.StartEncounter(
            new List<EnemyStatsSO> { resonanceEnemy });

        // Wait for resonance battle to finish
        yield return new WaitUntil(() => EncounterManager.ResonanceBattleDone);
        Debug.Log("[RESONANCE CS] Resonance battle done");
        EncounterManager.ResonanceBattleDone = false;

        yield return new WaitForSeconds(0.8f);
        yield return new WaitUntil(() => DialogueUI.Instance != null);

        // Post battle dialogue
        if (postBattleDialogue != null)
        {
            bool done = false;
            DialogueUI.Instance.StartDialogue(postBattleDialogue, () => done = true);
            yield return new WaitUntil(() => done);
        }

        // Knockout dialogue
        if (knockoutDialogue != null)
        {
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
                Debug.Log("[RESONANCE CS] Hirose removed from party for duel");
            }
        }

        // Start forced loss duel
        Debug.Log("[RESONANCE CS] Starting forced duel");
        EncounterManager.IsForcedLossBattle = true;
        EncounterManager.ForcedLossBattleDone = false;
        EncounterManager.Instance.StartEncounter(
            new List<EnemyStatsSO> { hiroseAsDuelEnemy });

        // Wait for duel to finish
        yield return new WaitUntil(() => EncounterManager.ForcedLossBattleDone);
        Debug.Log("[RESONANCE CS] Duel done");
        EncounterManager.ForcedLossBattleDone = false;

        yield return new WaitForSeconds(0.5f);
        yield return new WaitUntil(() => DialogueUI.Instance != null);

        // Black out
        bool blackOutDone = false;
        StartCoroutine(ResonanceManager.Instance.BlackOut(() => blackOutDone = true));
        yield return new WaitUntil(() => blackOutDone);

        // Deactivate resonance
        ResonanceManager.Instance.DeactivateResonance();

        // Re-add Hirose
        if (hiroseInstance != null &&
            !PartyManager.Instance.activeParty.Contains(hiroseInstance))
            PartyManager.Instance.activeParty.Add(hiroseInstance);

        // Teleport player
        var player = GameObject.FindGameObjectWithTag("Player");
        if (player != null && wakeUpLocation != null)
        {
            player.transform.position = wakeUpLocation.position;
            EncounterManager.PlayerReturnPosition = Vector3.zero;
        }

        // Unlock resonance meter for future use
        ResonanceManager.MeterUnlocked = true;

        // Fade in
        bool fadeInDone = false;
        StartCoroutine(ResonanceManager.Instance.FadeIn(() => fadeInDone = true));
        yield return new WaitUntil(() => fadeInDone);

        yield return new WaitForSeconds(0.3f);

        // Wake up dialogue
        if (afterDuelDialogue != null)
        {
            bool done = false;
            DialogueUI.Instance.StartDialogue(afterDuelDialogue, () => done = true);
            yield return new WaitUntil(() => done);
        }

        SetPlayerFrozen(false);
        Debug.Log("[RESONANCE CS] Full sequence complete");
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