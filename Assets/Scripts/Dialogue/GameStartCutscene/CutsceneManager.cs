using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class CutsceneManager : MonoBehaviour
{
    [Header("Dialogues")]
    public DialogueSO fullDialogue; // all dialogue in one SO
    public int monsterDialogueStartIndex = 2; // which line monster starts speaking
    public int postBattleDialogueStartIndex = 4; // which line post battle starts

    [Header("Monster")]
    public GameObject monsterGameObject;
    public float monsterMoveSpeed = 2f;
    public EnemyStatsSO monsterEnemySO;

    [Header("Torch")]
    public TorchEffect torchEffect;

    [Header("Trigger")]
    public GameObject triggerObject; // the scripted event trigger GO

    private Transform player;
    private bool cutsceneStarted = false;
    private bool battleDone = false;
    private string cutsceneSaveKey;

    void Start()
    {
        player = GameObject.FindGameObjectWithTag("Player")?.transform;

        cutsceneSaveKey = $"cutscene_{transform.position.x}_{transform.position.y}";

        // Hide monster at start
        if (monsterGameObject != null)
            monsterGameObject.SetActive(false);

        // Torch off by default
        if (torchEffect != null)
            torchEffect.SetActive(false);
    }

    // Called by ScriptedEventTrigger when player steps on it
    public void StartCutscene()
    {
        if (cutsceneStarted) return;
        if (PlayerPrefs.GetInt(cutsceneSaveKey, 0) == 1) return;
        cutsceneStarted = true;

        // Disable trigger so it cant fire again
        if (triggerObject != null)
            triggerObject.SetActive(false);

        // Activate torch effect
        if (torchEffect != null)
            torchEffect.SetActive(true);

        StartCoroutine(PlayCutscene());
    }

    IEnumerator PlayCutscene()
    {
        yield return new WaitForSeconds(0.3f);

        // Play lines up to monster dialogue start
        bool part1Done = false;
        DialogueUI.Instance.StartDialogueRange(
            fullDialogue, 0, monsterDialogueStartIndex - 1,
            () => part1Done = true);
        yield return new WaitUntil(() => part1Done);

        // Player can walk briefly
        yield return new WaitForSeconds(2f);

        // Monster appears
        if (monsterGameObject != null)
            monsterGameObject.SetActive(true);

        yield return new WaitForSeconds(0.5f);

        // Monster dialogue
        bool part2Done = false;
        DialogueUI.Instance.StartDialogueRange(
            fullDialogue, monsterDialogueStartIndex, postBattleDialogueStartIndex - 1,
            () => part2Done = true);
        yield return new WaitUntil(() => part2Done);

        // Monster charges
        StartCoroutine(MonsterCharge());
    }

    IEnumerator MonsterCharge()
    {
        if (monsterGameObject == null || player == null) yield break;

        var rb = monsterGameObject.GetComponent<Rigidbody2D>();

        while (!battleDone)
        {
            if (rb != null)
            {
                Vector2 dir = (player.position - monsterGameObject.transform.position).normalized;
                rb.linearVelocity = dir * monsterMoveSpeed;
            }
            else
            {
                monsterGameObject.transform.position = Vector2.MoveTowards(
                    monsterGameObject.transform.position,
                    player.position,
                    monsterMoveSpeed * Time.deltaTime);
            }
            yield return null;
        }
    }

    public void TriggerScriptedBattle()
    {
        if (battleDone) return;
        StopCoroutine(nameof(MonsterCharge));

        var enemies = new List<EnemyStatsSO> { monsterEnemySO };
        EncounterManager.Instance.StartEncounter(enemies);

        EncounterManager.ActiveCutscene = this;
    }

    // Call this after returning from battle
    public void OnBattleComplete()
    {
        battleDone = true;

        // Disable torch
        if (torchEffect != null)
            torchEffect.SetActive(false);

        // Destroy monster
        if (monsterGameObject != null)
            Destroy(monsterGameObject);

        // Save that this cutscene is done
        PlayerPrefs.SetInt(cutsceneSaveKey, 1);
        PlayerPrefs.Save();

        // Post battle dialogue
        if (postBattleDialogueStartIndex < fullDialogue.lines.Count)
        {
            DialogueUI.Instance.StartDialogueRange(
                fullDialogue,
                postBattleDialogueStartIndex,
                fullDialogue.lines.Count - 1,
                null);
        }
    }
}