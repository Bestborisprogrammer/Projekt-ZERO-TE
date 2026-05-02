using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class CutsceneManager : MonoBehaviour
{
    [Header("Monster")]
    public GameObject monsterGameObject;
    public float monsterMoveSpeed = 3f;
    public EnemyStatsSO monsterEnemySO;

    [Header("Fog")]
    public FogEffect fogEffect;

    private Transform player;
    private bool battleDone = false;
    private Coroutine chargeCoroutine;

    void Start()
    {
        player = GameObject.FindGameObjectWithTag("Player")?.transform;

        if (monsterGameObject != null)
            monsterGameObject.SetActive(false);

        if (fogEffect != null)
            fogEffect.SetActive(false);
    }

    public void StartMonsterSequence()
    {
        Debug.Log("[CUTSCENE] StartMonsterSequence called");
        StartCoroutine(MonsterSequence());
    }

    IEnumerator MonsterSequence()
    {
        if (fogEffect != null)
            fogEffect.SetActive(true);

        yield return new WaitForSeconds(1f);

        if (monsterGameObject != null)
        {
            monsterGameObject.SetActive(true);
            Debug.Log("[CUTSCENE] Monster spawned");
        }

        // Second dialogue trigger handles monster dialogue
        // Just start charging after spawn delay
        yield return new WaitForSeconds(0f);
        chargeCoroutine = StartCoroutine(MonsterCharge());
    }

    IEnumerator MonsterCharge()
    {
        if (monsterGameObject == null) yield break;

        var rb = monsterGameObject.GetComponent<Rigidbody2D>();

        while (!battleDone)
        {
            if (monsterGameObject == null || player == null) yield break;

            Vector2 dir = ((Vector2)player.position - (Vector2)monsterGameObject.transform.position).normalized;

            if (rb != null)
                rb.linearVelocity = dir * monsterMoveSpeed;
            else
                monsterGameObject.transform.position = Vector2.MoveTowards(
                    monsterGameObject.transform.position,
                    player.position,
                    monsterMoveSpeed * Time.deltaTime);

            yield return null;
        }
    }

    public void TriggerScriptedBattle()
    {
        if (battleDone) return;
        Debug.Log("[CUTSCENE] Battle triggered!");
        battleDone = true;

        if (chargeCoroutine != null)
            StopCoroutine(chargeCoroutine);

        if (monsterGameObject != null)
        {
            var rb = monsterGameObject.GetComponent<Rigidbody2D>();
            if (rb != null) rb.linearVelocity = Vector2.zero;
        }

        EncounterManager.ActiveCutscene = this;
        EncounterManager.Instance.StartEncounter(new List<EnemyStatsSO> { monsterEnemySO });
    }

    public void OnBattleComplete()
    {
        Debug.Log("[CUTSCENE] Battle complete!");

        if (fogEffect != null)
            fogEffect.SetActive(false);

        if (monsterGameObject != null)
            Destroy(monsterGameObject);
    }
}