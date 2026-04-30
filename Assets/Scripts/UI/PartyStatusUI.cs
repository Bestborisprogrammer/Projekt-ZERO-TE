using UnityEngine;
using TMPro;
using UnityEngine.UI;

public class PartyStatusUI : MonoBehaviour
{
    public GameObject statusPanel;
    public TextMeshProUGUI memberInfoText;
    public TextMeshProUGUI pageText;
    public Button prevButton;
    public Button nextButton;

    private bool isOpen = false;
    private int currentIndex = 0;

    void Start()
    {
        statusPanel.SetActive(false);
        memberInfoText.gameObject.SetActive(false);
        pageText.gameObject.SetActive(false);
        prevButton.gameObject.SetActive(false);
        nextButton.gameObject.SetActive(false);

        prevButton.onClick.AddListener(PrevMember);
        nextButton.onClick.AddListener(NextMember);
    }

    public void TogglePanel()
    {
        isOpen = !isOpen;

        statusPanel.SetActive(isOpen);
        memberInfoText.gameObject.SetActive(isOpen);
        pageText.gameObject.SetActive(isOpen);

        if (isOpen)
        {
            currentIndex = 0;
            ShowMember(currentIndex);
        }
        else
        {
            prevButton.gameObject.SetActive(false);
            nextButton.gameObject.SetActive(false);
        }
    }

    void PrevMember()
    {
        currentIndex--;
        if (currentIndex < 0)
            currentIndex = PartyManager.Instance.activeParty.Count - 1;
        ShowMember(currentIndex);
    }

    void NextMember()
    {
        currentIndex++;
        if (currentIndex >= PartyManager.Instance.activeParty.Count)
            currentIndex = 0;
        ShowMember(currentIndex);
    }

    void ShowMember(int index)
    {
        if (PartyManager.Instance == null) return;

        var member = PartyManager.Instance.activeParty[index];
        float xpPercent = (float)member.currentXP / member.xpToNextLevel * 100f;

        // Build affinity string
        string affinities = member.baseData.affinities.Count > 0
            ? string.Join(", ", member.baseData.affinities)
            : "None";

        memberInfoText.text =
            $"════════════════\n" +
            $"{member.Name}  |  Lv. {member.level}\n" +
            $"════════════════\n" +
            $"HP:  {member.currentHP} / {member.MaxHP}\n" +
            $"MP:  {member.currentMana} / {member.MaxMana}\n" +
            $"ATK: {member.Attack}\n" +
            $"DEF: {member.Defense}\n" +
            $"SPD: {member.Speed}\n" +
            $"Affinity: {affinities}\n" +
            $"XP:  {member.currentXP} / {member.xpToNextLevel}  ({xpPercent:F1}%)";

        pageText.text = $"{index + 1} / {PartyManager.Instance.activeParty.Count}";

        bool multipleMembers = PartyManager.Instance.activeParty.Count > 1;
        prevButton.gameObject.SetActive(multipleMembers);
        nextButton.gameObject.SetActive(multipleMembers);
    }
}