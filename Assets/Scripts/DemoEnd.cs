using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;

public class DemoEnd : MonoBehaviour
{
    private bool triggered = false;

    void OnTriggerEnter2D(Collider2D other)
    {
        if (!other.CompareTag("Player")) return;
        if (triggered) return;
        triggered = true;

        // Freeze player
        var movement = other.GetComponent<PlayerMovement2D>();
        if (movement != null) movement.enabled = false;
        var rb = other.GetComponent<Rigidbody2D>();
        if (rb != null) rb.linearVelocity = Vector2.zero;

        StartCoroutine(PlayDemoEnd());
    }

    IEnumerator PlayDemoEnd()
    {
        // Create canvas
        GameObject canvasObj = new GameObject("DemoEndCanvas");
        var canvas = canvasObj.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 999;
        canvasObj.AddComponent<CanvasScaler>();
        canvasObj.AddComponent<GraphicRaycaster>();
        DontDestroyOnLoad(canvasObj);

        // Black background
        GameObject bgObj = new GameObject("BG");
        bgObj.transform.SetParent(canvasObj.transform, false);
        var bg = bgObj.AddComponent<Image>();
        bg.color = new Color(0, 0, 0, 0);
        var bgRT = bgObj.GetComponent<RectTransform>();
        bgRT.anchorMin = Vector2.zero;
        bgRT.anchorMax = Vector2.one;
        bgRT.offsetMin = Vector2.zero;
        bgRT.offsetMax = Vector2.zero;

        // Demo Over text
        GameObject demoObj = new GameObject("DemoText");
        demoObj.transform.SetParent(canvasObj.transform, false);
        var demoTMP = demoObj.AddComponent<TextMeshProUGUI>();
        demoTMP.text = "DEMO OVER";
        demoTMP.fontSize = 72;
        demoTMP.alignment = TextAlignmentOptions.Center;
        demoTMP.color = new Color(1, 1, 1, 0);
        demoTMP.fontStyle = FontStyles.Bold;
        var demoRT = demoObj.GetComponent<RectTransform>();
        demoRT.anchorMin = new Vector2(0.1f, 0.55f);
        demoRT.anchorMax = new Vector2(0.9f, 0.75f);
        demoRT.offsetMin = Vector2.zero;
        demoRT.offsetMax = Vector2.zero;

        // To Be Continued text
        GameObject contObj = new GameObject("ContText");
        contObj.transform.SetParent(canvasObj.transform, false);
        var contTMP = contObj.AddComponent<TextMeshProUGUI>();
        contTMP.text = "...to be continued";
        contTMP.fontSize = 36;
        contTMP.alignment = TextAlignmentOptions.Center;
        contTMP.color = new Color(1, 1, 1, 0);
        contTMP.fontStyle = FontStyles.Italic;
        var contRT = contObj.GetComponent<RectTransform>();
        contRT.anchorMin = new Vector2(0.1f, 0.38f);
        contRT.anchorMax = new Vector2(0.9f, 0.52f);
        contRT.offsetMin = Vector2.zero;
        contRT.offsetMax = Vector2.zero;

        // Phase 1 – fade to black
        float t = 0f;
        while (t < 1.5f)
        {
            t += Time.deltaTime;
            bg.color = new Color(0, 0, 0, Mathf.Clamp01(t / 1.5f));
            yield return null;
        }
        bg.color = new Color(0, 0, 0, 1);

        yield return new WaitForSeconds(0.5f);

        // Phase 2 – fade in DEMO OVER
        t = 0f;
        while (t < 1f)
        {
            t += Time.deltaTime;
            demoTMP.color = new Color(1, 1, 1, t);
            yield return null;
        }

        yield return new WaitForSeconds(0.8f);

        // Phase 3 – fade in ...to be continued
        t = 0f;
        while (t < 1f)
        {
            t += Time.deltaTime;
            contTMP.color = new Color(1, 1, 1, t);
            yield return null;
        }

        yield return new WaitForSeconds(3f);

        // Quit
        Debug.Log("DEMO END");
        Application.Quit();

#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#endif
    }
}