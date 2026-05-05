using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;
using System.Collections.Generic;

public class EncounterManager : MonoBehaviour
{
    public static EncounterManager Instance;
    public string combatSceneName = "CombatScene";

    public static List<EnemyStatsSO> CurrentEnemies { get; private set; } = new();
    public static Vector3 PlayerReturnPosition { get; set; }
    public static CutsceneManager ActiveCutscene { get; set; }

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else Destroy(gameObject);
    }

    public void StartEncounter(List<EnemyStatsSO> enemies)
    {
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player != null)
            PlayerReturnPosition = player.transform.position;

        CurrentEnemies = enemies;
        StartCoroutine(BattleTransition());
    }

    IEnumerator BattleTransition()
    {
        var overlay = GetOrCreateOverlay();

        // Flash white like Pokemon
        yield return StartCoroutine(FlashScreen(overlay, 6, 0.07f));

        // Fade to black
        yield return StartCoroutine(FadeToBlack(overlay));

        // Load scene then destroy overlay
        SceneManager.LoadScene(combatSceneName);

        // Destroy after tiny delay so scene loads first
        Destroy(overlay.transform.parent.gameObject, 0.1f);
    }

    IEnumerator FlashScreen(UnityEngine.UI.Image overlay, int flashes, float interval)
    {
        for (int i = 0; i < flashes; i++)
        {
            overlay.color = new Color(1, 1, 1, 0.9f);
            yield return new WaitForSeconds(interval);
            overlay.color = new Color(1, 1, 1, 0f);
            yield return new WaitForSeconds(interval);
        }
    }

    IEnumerator FadeToBlack(UnityEngine.UI.Image overlay)
    {
        float elapsed = 0f;
        float duration = 0.4f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            overlay.color = new Color(0, 0, 0, Mathf.Lerp(0f, 1f, elapsed / duration));
            yield return null;
        }
        overlay.color = Color.black;
    }

    UnityEngine.UI.Image overlayImage;

    UnityEngine.UI.Image GetOrCreateOverlay()
    {
        if (overlayImage != null) return overlayImage;

        GameObject canvasObj = new GameObject("TransitionCanvas");
        var canvas = canvasObj.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 999;
        canvasObj.AddComponent<UnityEngine.UI.CanvasScaler>();

        GameObject imgObj = new GameObject("Overlay");
        imgObj.transform.SetParent(canvasObj.transform, false);
        overlayImage = imgObj.AddComponent<UnityEngine.UI.Image>();
        overlayImage.color = new Color(1, 1, 1, 0);

        var rt = imgObj.GetComponent<RectTransform>();
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;

        return overlayImage;
    }
}