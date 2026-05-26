using UnityEngine;
using TMPro;
using UnityEngine.UI;
using System.Collections;

public class DemoEndTrigger : MonoBehaviour
{
    [Header("Text Settings")]
    public string message = "DEMO FINISHED\n\nTO BE CONTINUED...";
    public float textDelay = 1f;
    public float endDuration = 4f;

    private bool triggered = false;

    private Canvas canvas;
    private Image blackImage;
    private TextMeshProUGUI endText;

    void Start()
    {
        CreateUI();
    }

    void CreateUI()
    {
        // Create Canvas
        GameObject canvasObj = new GameObject("EndCanvas");
        canvas = canvasObj.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 9999;

        canvasObj.AddComponent<CanvasScaler>();
        canvasObj.AddComponent<GraphicRaycaster>();

        DontDestroyOnLoad(canvasObj);

        // Black screen
        GameObject blackObj = new GameObject("BlackScreen");
        blackObj.transform.SetParent(canvas.transform, false);

        blackImage = blackObj.AddComponent<Image>();
        blackImage.color = new Color(0, 0, 0, 0);

        RectTransform brt = blackObj.GetComponent<RectTransform>();
        brt.anchorMin = Vector2.zero;
        brt.anchorMax = Vector2.one;
        brt.offsetMin = Vector2.zero;
        brt.offsetMax = Vector2.zero;

        // Text
        GameObject textObj = new GameObject("EndText");
        textObj.transform.SetParent(canvas.transform, false);

        endText = textObj.AddComponent<TextMeshProUGUI>();
        endText.text = message;
        endText.fontSize = 48;
        endText.alignment = TextAlignmentOptions.Center;
        endText.color = Color.white;

        RectTransform tr = textObj.GetComponent<RectTransform>();
        tr.anchorMin = new Vector2(0f, 0f);
        tr.anchorMax = new Vector2(1f, 1f);
        tr.offsetMin = Vector2.zero;
        tr.offsetMax = Vector2.zero;

        endText.gameObject.SetActive(false);

        // FORCE TEXT ABOVE BLACK SCREEN
        textObj.transform.SetAsLastSibling();
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (triggered) return;

        if (other.CompareTag("Player"))
        {
            triggered = true;
            StartCoroutine(EndSequence());
        }
    }

    IEnumerator EndSequence()
    {
        // Fade to black manually
        float t = 0f;
        float duration = 1f;

        while (t < duration)
        {
            t += Time.deltaTime;
            float a = Mathf.Lerp(0f, 1f, t / duration);
            blackImage.color = new Color(0, 0, 0, a);
            yield return null;
        }

        blackImage.color = Color.black;

        yield return new WaitForSeconds(textDelay);

        // Show text ON TOP of black
        endText.gameObject.SetActive(true);
        endText.transform.SetAsLastSibling();

        yield return new WaitForSeconds(endDuration);

        Application.Quit();

#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#endif
    }
}