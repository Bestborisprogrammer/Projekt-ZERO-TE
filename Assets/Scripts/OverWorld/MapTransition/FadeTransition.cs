using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class FadeTransition : MonoBehaviour
{
    public static FadeTransition Instance;

    public Image fadeImage;
    public float fadeDuration = 0.5f;

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else Destroy(gameObject);
    }

    void Start()
    {
        // Start fully transparent
        SetAlpha(0f);
    }

    public void FadeToPosition(Vector3 targetPosition, System.Action onMidFade = null)
    {
        StartCoroutine(FadeRoutine(targetPosition, onMidFade));
    }

    IEnumerator FadeRoutine(Vector3 targetPosition, System.Action onMidFade)
    {
        // Fade to black
        yield return StartCoroutine(Fade(0f, 1f));

        // Teleport player
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player != null)
            player.transform.position = targetPosition;

        onMidFade?.Invoke();

        // Small pause at black
        yield return new WaitForSeconds(0.1f);

        // Fade back in
        yield return StartCoroutine(Fade(1f, 0f));
    }

    IEnumerator Fade(float from, float to)
    {
        float elapsed = 0f;
        SetAlpha(from);

        while (elapsed < fadeDuration)
        {
            elapsed += Time.deltaTime;
            SetAlpha(Mathf.Lerp(from, to, elapsed / fadeDuration));
            yield return null;
        }

        SetAlpha(to);
    }

    void SetAlpha(float alpha)
    {
        if (fadeImage == null) return;
        var color = fadeImage.color;
        color.a = alpha;
        fadeImage.color = color;
    }
}