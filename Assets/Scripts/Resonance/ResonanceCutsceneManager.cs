using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class ResonanceCutsceneManager : MonoBehaviour
{
    [Header("Dialogues")]
    public DialogueSO resonanceDialogue;       // Edward enters resonance
    public DialogueSO postBattleDialogue;      // Hirose worried
    public DialogueSO afterDuelDialogue;       // Edward wakes up
    public DialogueSO knockoutDialogue;        // Hirose knocking Edward out

    [Header("Enemy for Resonance Battle")]
    public EnemyStatsSO resonanceEnemy;

    [Header("Hirose for Duel")]
    public CharacterStatsSO hiroseStats;
    public EnemyStatsSO hiroseAsDuelEnemy; // Hirose as enemy for the forced loss

    [Header("After Defeat")]
    public Transform wakeUpLocation;

    [Header("Trigger")]
    public GameObject triggerObject;

    private bool started = false;
    private string saveKey;

    void Start()
    {
        saveKey = $"resonance_cutscene_{gameObject.name}";
        if (PlayerPrefs.GetInt(saveKey, 0) == 1)
        {
            if (triggerObject != null)
                triggerObject.SetActive(false);
        }
    }

    public void StartResonanceCutscene()
    {
        if (started) return;
        started = true;

        if (triggerObject != null)
            triggerObject.SetActive(false);

        PlayerPrefs.SetInt(saveKey, 1);
        PlayerPrefs.Save();

        StartCoroutine(ResonanceSequence());
    }

    IEnumerator ResonanceSequence()
    {
        SetPlayerFrozen(true);

        // Purple flash sequence
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
        EncounterManager.IsResonanceBattle = true;
        EncounterManager.Instance.StartEncounter(
            new List<EnemyStatsSO> { resonanceEnemy });

        // Wait for battle to complete
        yield return new WaitUntil(() => EncounterManager.ResonanceBattleDone);
        EncounterManager.ResonanceBattleDone = false;

        yield return new WaitForSeconds(0.5f);

        // Post battle dialogue – Hirose worried
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

        // Remove Hirose from active party temporarily for duel
        var hiroseInstance = PartyManager.Instance.activeParty
            .Find(m => m.baseData == hiroseStats);
        if (hiroseInstance != null)
            PartyManager.Instance.activeParty.Remove(hiroseInstance);

        yield return new WaitForSeconds(0.3f);

        // Start forced loss duel
        EncounterManager.IsForcedLossBattle = true;
        EncounterManager.Instance.StartEncounter(
            new List<EnemyStatsSO> { hiroseAsDuelEnemy });

        // Wait for duel to complete (forced loss)
        yield return new WaitUntil(() => EncounterManager.ForcedLossBattleDone);
        EncounterManager.ForcedLossBattleDone = false;

        // Edward blacks out sequence
        yield return StartCoroutine(
            ResonanceManager.Instance.FadeToBlackOut());

        // Deactivate resonance
        ResonanceManager.Instance.DeactivateResonance();

        // Re-add Hirose
        if (hiroseInstance != null &&
            !PartyManager.Instance.activeParty.Contains(hiroseInstance))
            PartyManager.Instance.activeParty.Add(hiroseInstance);

        // Teleport to wake up location
        var player = GameObject.FindGameObjectWithTag("Player");
        if (player != null && wakeUpLocation != null)
            player.transform.position = wakeUpLocation.position;

        // Fade back in
        bool fadeInDone = false;
        ResonanceManager.Instance.FadeIn(() => fadeInDone = true);
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