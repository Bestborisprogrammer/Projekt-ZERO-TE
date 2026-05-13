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

        var topBar = CreateBar(canvasObj.transform, true);
        var bottomBar = CreateBar(canvasObj.transform, false);

        SetBarAnchors(topBar, true, 0.5f);
        SetBarAnchors(bottomBar, false, 0.5f);

        yield return new WaitForSeconds(0.2f);

        float elapsed = 0f;
        float duration = 0.8f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.SmoothStep(0f, 1f, elapsed / duration);

            var topRT = topBar.GetComponent<RectTransform>();
            topRT.anchorMin = new Vector2(0, 1f - (1f - t) * 0.5f);
            topRT.anchorMax = new Vector2(1, 1);

            var bottomRT = bottomBar.GetComponent<RectTransform>();
            bottomRT.anchorMin = new Vector2(0, 0);
            bottomRT.anchorMax = new Vector2(1, (1f - t) * 0.5f);

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

    void SetBarAnchors(Image bar, bool isTop, float height)
    {
        var rt = bar.GetComponent<RectTransform>();
        if (isTop)
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