using UnityEngine;
using System.Collections;

public class CutsceneManager : MonoBehaviour
{
    [Header("Dialogues")]
    public DialogueSO wakeUpDialogue;
    public DialogueSO monsterDialogue;

    [Header("Monster")]
    public GameObject monsterGameObject;
    public float monsterMoveSpeed = 2f;
    public EnemyStatsSO monsterEnemySO;

    [Header("Torch")]
    public TorchEffect torchEffect;
    public bool roomHasTorch = true;

    [Header("Encounter")]
    public EncounterManager encounterManager;

    private Transform player;
    private bool cutsceneStarted = false;

    void Start()
    {
        player = GameObject.FindGameObjectWithTag("Player")?.transform;

        // Hide monster at start
        if (monsterGameObject != null)
            monsterGameObject.SetActive(false);

        // Activate torch if this room has it
        if (torchEffect != null)
            torchEffect.SetActive(roomHasTorch);

        StartCoroutine(PlayOpeningCutscene());
    }

    IEnumerator PlayOpeningCutscene()
    {
        // Small delay before dialogue starts
        yield return new WaitForSeconds(0.5f);

        // MC wakes up dialogue
        bool wakeUpDone = false;
        DialogueUI.Instance.StartDialogue(wakeUpDialogue, () => wakeUpDone = true);
        yield return new WaitUntil(() => wakeUpDone);

        // Player can walk around briefly
        yield return new WaitForSeconds(3f);

        // Monster appears
        if (monsterGameObject != null)
            monsterGameObject.SetActive(true);

        // Short pause before monster speaks
        yield return new WaitForSeconds(0.5f);

        // Monster dialogue
        bool monsterDone = false;
        DialogueUI.Instance.StartDialogue(monsterDialogue, () => monsterDone = true);
        yield return new WaitUntil(() => monsterDone);

        // Monster charges at player
        StartCoroutine(MonsterCharge());
    }

    IEnumerator MonsterCharge()
    {
        if (monsterGameObject == null || player == null) yield break;

        var rb = monsterGameObject.GetComponent<Rigidbody2D>();

        while (true)
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

    // Called when monster collider hits player
    public void TriggerScriptedBattle()
    {
        if (cutsceneStarted) return;
        cutsceneStarted = true;

        StopAllCoroutines();

        var enemies = new System.Collections.Generic.List<EnemyStatsSO> { monsterEnemySO };
        EncounterManager.Instance.StartEncounter(enemies);
    }
}