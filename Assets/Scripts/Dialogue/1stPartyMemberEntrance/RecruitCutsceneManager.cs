using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using TMPro;

public class RecruitCutsceneManager : MonoBehaviour
{
    [Header("Dialogues")]
    public DialogueSO preDialogue;
    public DialogueSO postDialogue;
    public DialogueSO npcTalkDialogue;

    [Header("New Member")]
    public CharacterStatsSO newMember;

    [Header("Enemy")]
    public EnemyStatsSO enemyToFight;
    public GameObject enemyGameObject;

    [Header("NPC")]
    public GameObject npcGameObject;

    [Header("Trigger")]
    public GameObject triggerObject;

    [Header("Join Message")]
    public TextMeshProUGUI joinMessageText;
    public float joinMessageDuration = 3f;

    private bool cutsceneStarted = false;
    private CharacterInstance recruitedInstance;
    private string saveKey;

    void Awake()
    {
        saveKey = $"recruit_cs_{gameObject.name}";
        TrackedPlayerPrefsKeys.Register(saveKey);
        Debug.Log($"[RECRUIT CS] Awake - saveKey: {saveKey}");
    }

    void Start()
    {
        if (joinMessageText != null)
            joinMessageText.gameObject.SetActive(false);

        int flagValue = PlayerPrefs.GetInt(saveKey, 0);
        Debug.Log($"[RECRUIT CS] Start - {saveKey} = {flagValue}");

        if (flagValue == 1)
        {
            Debug.Log("[RECRUIT CS] Already completed - disabling trigger");
            if (triggerObject != null)
                triggerObject.SetActive(false);
        }

        Debug.Log($"[RECRUIT CS] PendingCompletion:{EncounterManager.PendingRecruitCompletion} " +
            $"PendingName:{EncounterManager.PendingRecruitMemberName} " +
            $"MyMember:{newMember?.characterName}");

        if (EncounterManager.PendingRecruitCompletion &&
            EncounterManager.PendingRecruitMemberName == newMember.characterName)
        {
            Debug.Log("[RECRUIT CS] Match! Running PostBattle");
            EncounterManager.PendingRecruitCompletion = false;
            EncounterManager.PendingRecruitMemberName = "";

            if (enemyGameObject != null)
                enemyGameObject.SetActive(false);

            StartCoroutine(PostBattle());
        }
    }

    public void StartRecruitCutscene()
    {
        if (cutsceneStarted) return;
        if (PlayerPrefs.GetInt(saveKey, 0) == 1)
        {
            Debug.Log("[RECRUIT CS] Already done - not starting again");
            return;
        }

        cutsceneStarted = true;
        Debug.Log("[RECRUIT CS] Starting cutscene");

        PlayerPrefs.SetInt(saveKey, 1);
        PlayerPrefs.Save();

        if (triggerObject != null)
            triggerObject.SetActive(false);

        StartCoroutine(PlayCutscene());
    }

    IEnumerator PlayCutscene()
    {
        SetPlayerFrozen(true);
        yield return new WaitForSeconds(0.3f);

        if (preDialogue != null)
        {
            bool done = false;
            DialogueUI.Instance.StartDialogue(preDialogue, () => done = true);
            yield return new WaitUntil(() => done);
        }

        yield return new WaitForSeconds(0.3f);

        recruitedInstance = new CharacterInstance { baseData = newMember };
        recruitedInstance.Initialize();

        if (!PartyManager.Instance.allMembers.Exists(m => m.Name == newMember.characterName))
            PartyManager.Instance.allMembers.Add(recruitedInstance);

        if (!PartyManager.Instance.activeParty.Exists(m => m.Name == newMember.characterName)
            && PartyManager.Instance.activeParty.Count < 4)
            PartyManager.Instance.activeParty.Add(recruitedInstance);

        EncounterManager.IsRecruitBattle = true;
        EncounterManager.PendingRecruitCompletion = true;
        EncounterManager.PendingRecruitMemberName = newMember.characterName;

        Debug.Log($"[RECRUIT CS] Starting encounter");
        EncounterManager.Instance.StartEncounter(new List<EnemyStatsSO> { enemyToFight });
    }

    IEnumerator PostBattle()
    {
        yield return new WaitForSeconds(0.8f);
        yield return new WaitUntil(() => DialogueUI.Instance != null);

        SetPlayerFrozen(true);

        if (postDialogue != null)
        {
            bool done = false;
            DialogueUI.Instance.StartDialogue(postDialogue, () => done = true);
            yield return new WaitUntil(() => done);
        }

        yield return StartCoroutine(ShowJoinMessage());

        if (!PartyManager.Instance.allMembers.Exists(m => m.Name == newMember.characterName))
        {
            var newInst = new CharacterInstance { baseData = newMember };
            newInst.Initialize();
            PartyManager.Instance.allMembers.Add(newInst);
        }

        if (!PartyManager.Instance.activeParty.Exists(m => m.Name == newMember.characterName))
        {
            var inst = PartyManager.Instance.allMembers
                .Find(m => m.Name == newMember.characterName);
            if (inst != null && PartyManager.Instance.activeParty.Count < 4)
                PartyManager.Instance.activeParty.Add(inst);
        }

        Debug.Log($"[RECRUIT CS] {newMember.characterName} permanently joined!");

        if (npcGameObject != null && npcTalkDialogue != null)
            SetupNPCTalk();

        SetPlayerFrozen(false);
    }

    IEnumerator ShowJoinMessage()
    {
        if (joinMessageText == null) yield break;

        joinMessageText.text = $"{newMember.characterName} joined the party!";
        joinMessageText.gameObject.SetActive(true);

        Color c = joinMessageText.color;
        c.a = 0f;
        joinMessageText.color = c;

        float t = 0f;
        while (t < 0.5f)
        {
            t += Time.deltaTime;
            c.a = t / 0.5f;
            joinMessageText.color = c;
            yield return null;
        }
        c.a = 1f;
        joinMessageText.color = c;

        yield return new WaitForSeconds(joinMessageDuration);

        t = 0f;
        while (t < 0.5f)
        {
            t += Time.deltaTime;
            c.a = 1f - t / 0.5f;
            joinMessageText.color = c;
            yield return null;
        }

        joinMessageText.gameObject.SetActive(false);
    }

    void SetupNPCTalk()
    {
        var trigger = npcGameObject.GetComponent<DialogueTrigger>();
        if (trigger == null)
            trigger = npcGameObject.AddComponent<DialogueTrigger>();

        trigger.dialogue = npcTalkDialogue;
        trigger.triggerType = TriggerType.Interact;
        trigger.oneTimeOnly = false;

        var col = npcGameObject.GetComponent<CircleCollider2D>();
        if (col == null)
        {
            col = npcGameObject.AddComponent<CircleCollider2D>();
            col.isTrigger = true;
            col.radius = 1.5f;
        }
    }

    public void DespawnNPC()
    {
        if (npcGameObject != null)
            npcGameObject.SetActive(false);
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