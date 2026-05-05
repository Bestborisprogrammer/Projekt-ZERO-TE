using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class CombatFadeIn : MonoBehaviour
{
    void Start()
    {
        StartCoroutine(CinematicOpen());
    }

    IEnumerator CinematicOpen()
    {
        GameObject canvasObj = new GameObject("CombatFadeCanvas");
        var canvas = canvasObj.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 999;
        canvasObj.AddComponent<CanvasScaler>();

        // Top bar
        var topBar = CreateBar(canvasObj.transform, true);
        // Bottom bar
        var bottomBar = CreateBar(canvasObj.transform, false);

        // Start with bars covering full screen
        SetBarHeight(topBar, 0.5f);
        SetBarHeight(bottomBar, 0.5f);

        // Phase 1 – hold for dramatic effect
        yield return new WaitForSeconds(0.2f);

        // Phase 2 – bars slide outward revealing scene (cinematic letterbox open)
        float elapsed = 0f;
        float duration = 0.8f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.SmoothStep(0f, 1f, elapsed / duration);

            // Top bar slides up
            var topRT = topBar.GetComponent<RectTransform>();
            topRT.anchorMin = new Vector2(0, 1f - (1f - t) * 0.5f);
            topRT.anchorMax = new Vector2(1, 1);

            // Bottom bar slides down
            var bottomRT = bottomBar.GetComponent<RectTransform>();
            bottomRT.anchorMin = new Vector2(0, 0);
            bottomRT.anchorMax = new Vector2(1, (1f - t) * 0.5f);

            yield return null;
        }

        // Phase 3 – vignette fade (dark edges fade away)
        GameObject vignetteObj = new GameObject("Vignette");
        vignetteObj.transform.SetParent(canvasObj.transform, false);
        var vigImg = vignetteObj.AddComponent<Image>();

        // Dark overlay fades out
        vigImg.color = new Color(0, 0, 0, 0.6f);
        var vigRT = vignetteObj.GetComponent<RectTransform>();
        vigRT.anchorMin = Vector2.zero;
        vigRT.anchorMax = Vector2.one;
        vigRT.offsetMin = Vector2.zero;
        vigRT.offsetMax = Vector2.zero;

        elapsed = 0f;
        float fadeDuration = 0.5f;
        while (elapsed < fadeDuration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / fadeDuration;
            vigImg.color = new Color(0, 0, 0, Mathf.Lerp(0.6f, 0f, t));
            yield return null;
        }

        Destroy(canvasObj);
    }

    Image CreateBar(Transform parent, bool isTop)
    {
        GameObject obj = new GameObject(isTop ? "TopBar" : "BottomBar");
        obj.transform.SetParent(parent, false);
        var img = obj.AddComponent<Image>();
        img.color = Color.black;
        return img;
    }

    void SetBarHeight(Image bar, float height)
    {
        var rt = bar.GetComponent<RectTransform>();
        if (bar.gameObject.name == "TopBar")
        {
            rt.anchorMin = new Vector2(0, 1f - height);
            rt.anchorMax = new Vector2(1, 1);
        }
        else
        {
            rt.anchorMin = new Vector2(0, 0);
            rt.anchorMax = new Vector2(1, height);
        }
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;
    }
}