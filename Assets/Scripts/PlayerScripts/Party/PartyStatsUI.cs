using UnityEngine;

public class PartyStatsUI : MonoBehaviour
{
    public GameObject statsPanel;
    public Transform memberListParent;
    public GameObject memberCardPrefab;

    public void TogglePanel()
    {
        statsPanel.SetActive(!statsPanel.activeSelf);
        if (statsPanel.activeSelf) RefreshUI();
    }

    void RefreshUI()
    {
        foreach (Transform child in memberListParent)
            Destroy(child.gameObject);

        foreach (var member in PartyManager.Instance.activeParty)
        {
            GameObject card = Instantiate(memberCardPrefab, memberListParent);
            card.GetComponent<MemberCard>().Setup(member);
        }
    }
}