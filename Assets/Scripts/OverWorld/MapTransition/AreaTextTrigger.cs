using UnityEngine;
using TMPro;
using System.Collections;

public class AreaTextTrigger : MonoBehaviour
{
    [Header("UI")]
    public TextMeshProUGUI areaText;

    [Header("Settings")]
    public string message = "LEVEL 2";
    public float displayTime = 2f;

    private bool triggered = false;

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (triggered) return;

        if (other.CompareTag("Player"))
        {
            triggered = true;
            StartCoroutine(ShowText());
        }
    }

    IEnumerator ShowText()
    {
        areaText.text = message;
        areaText.gameObject.SetActive(true);

        yield return new WaitForSeconds(displayTime);

        areaText.gameObject.SetActive(false);
    }
}