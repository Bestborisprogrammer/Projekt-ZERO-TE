using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine.UI;

public class RecruitCutsceneManager : MonoBehaviour
{
    [Header("Dialogues")]
    public DialogueSO preDialogue;       // NPC fighting monster dialogue
    public DialogueSO midDialogue;       // before battle starts
    public DialogueSO postDialogue;      // after battle
    public DialogueSO joinDialogue;      // "[Name] joined the party!"

    [Header("New Member")]
    public CharacterStatsSO newMember;

    [Header("Enemy")]
    public EnemyStatsSO enemyToFight;

    [Header("NPC")]
    public GameObject npcGameObject;
    public SpriteRenderer npcSprite;

    [Header("Trigger")]
    public GameObject triggerObject;

    [Header("Join Message")]
    public GameObject joinMessagePanel;
    public TextMeshProUGUI joinMessageText;
    public Image joinMemberPortrait;

    private bool cutsceneStarted = false;
    private bool battleDone = false;

    void Start()
    {
        if (joinMessagePanel != null)
            joinMessagePanel.SetActive(false);
    }

    public void StartRecruitCutscene()
    {
        if (cutsceneStarted) return;
        cutsceneStarted = true;

        if (triggerObject != null)
            triggerObject.SetActive(false);

        StartCoroutine(PlayCutscene());
    }

    IEnumerator PlayCutscene()
    {
        // Freeze player
        SetPlayerFrozen(true);

        yield return new WaitForSeconds(0.3f);

        // Pre battle dialogue
        if (preDialogue != null)
        {
            bool done = false;
            DialogueUI.Instance.StartDialogue(preDialogue, () => done = true);
            yield return new WaitUntil(() => done);
        }

        yield return new WaitForSeconds(0.5f);

        // Mid battle dialogue
        if (midDialogue != null)
        {
            bool done = false;
            DialogueUI.Instance.StartDialogue(midDialogue, () => done = true);
            yield return new WaitUntil(() => done);
        }

        yield return new WaitForSeconds(0.3f);

        // Start battle with new member temporarily in party
        TriggerRecruitBattle();
    }

    void TriggerRecruitBattle()
    {
        // Temporarily add new member to party for this fight
        var tempMember = new CharacterInstance { baseData = newMember };
        tempMember.Initialize();
        PartyManager.Instance.allMembers.Add(tempMember);
        PartyManager.Instance.activeParty.Add(tempMember);

        EncounterManager.ActiveRecruitCutscene = this;
        EncounterManager.Instance.StartEncounter(new List<EnemyStatsSO> { enemyToFight });
    }

    public void OnBattleComplete()
    {
        battleDone = true;
        StartCoroutine(PostBattle());
    }

    IEnumerator PostBattle()
    {
        yield return new WaitForSeconds(0.5f);

        // Remove temp member from active party
        // We'll re-add properly after dialogue
        var tempMember = PartyManager.Instance.activeParty
            .Find(m => m.Name == newMember.characterName);
        if (tempMember != null)
            PartyManager.Instance.activeParty.Remove(tempMember);

        // Post battle dialogue
        if (postDialogue != null)
        {
            bool done = false;
            DialogueUI.Instance.StartDialogue(postDialogue, () => done = true);
            yield return new WaitUntil(() => done);
        }

        // Show join message
        yield return StartCoroutine(ShowJoinMessage());

        // Officially add member to party
        AddMemberToParty();

        // Join dialogue
        if (joinDialogue != null)
        {
            bool done = false;
            DialogueUI.Instance.StartDialogue(joinDialogue, () => done = true);
            yield return new WaitUntil(() => done);
        }

        // Unfreeze player
        SetPlayerFrozen(false);
    }

    IEnumerator ShowJoinMessage()
    {
        if (joinMessagePanel == null) yield break;

        string name = newMember.characterName;

        if (joinMessageText != null)
            joinMessageText.text = $"{name} joined the party!";

        if (joinMemberPortrait != null && newMember.portrait != null)
            joinMemberPortrait.sprite = newMember.portrait;

        joinMessagePanel.SetActive(true);

        // Fade in
        var canvasGroup = joinMessagePanel.GetComponent<CanvasGroup>();
        if (canvasGroup == null)
            canvasGroup = joinMessagePanel.AddComponent<CanvasGroup>();

        canvasGroup.alpha = 0f;
        float t = 0f;
        while (t < 0.5f)
        {
            t += Time.deltaTime;
            canvasGroup.alpha = t / 0.5f;
            yield return null;
        }
        canvasGroup.alpha = 1f;

        yield return new WaitForSeconds(2.5f);

        // Fade out
        t = 0f;
        while (t < 0.5f)
        {
            t += Time.deltaTime;
            canvasGroup.alpha = 1f - t / 0.5f;
            yield return null;
        }

        joinMessagePanel.SetActive(false);
    }

    void AddMemberToParty()
    {
        // Check not already in party
        if (PartyManager.Instance.allMembers
            .Exists(m => m.Name == newMember.characterName)) return;

        var newInstance = new CharacterInstance { baseData = newMember };
        newInstance.Initialize();
        PartyManager.Instance.allMembers.Add(newInstance);

        if (PartyManager.Instance.activeParty.Count < 4)
            PartyManager.Instance.activeParty.Add(newInstance);

        Debug.Log($"[RECRUIT] {newMember.characterName} permanently joined the party!");
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