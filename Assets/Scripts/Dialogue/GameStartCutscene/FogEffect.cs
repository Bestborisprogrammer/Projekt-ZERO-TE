using UnityEngine;
using UnityEngine.UI;

public class FogEffect : MonoBehaviour
{
    [Header("Settings")]
    public float radius = 150f;
    public Color darknessColor = new Color(0, 0, 0, 0.95f);
    public Transform player;
    public Canvas canvas;

    [Header("Fog Zones")]
    public Collider2D enterZone;   // walking into this activates fog
    public Collider2D exitZone;    // walking into this deactivates fog

    private Material fogMaterial;
    private Image overlay;
    private RectTransform overlayRT;
    private Camera mainCam;
    private bool isActive = false;

    void Start()
    {
        mainCam = Camera.main;

        if (player == null)
            player = GameObject.FindGameObjectWithTag("Player")?.transform;

        // Create its OWN canvas so sort order can be controlled independently
        GameObject canvasObj = new GameObject("FogCanvas");
        Canvas fogCanvas = canvasObj.AddComponent<Canvas>();
        fogCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
        fogCanvas.sortingOrder = 1; // BELOW dialogue which should be 50+
        canvasObj.AddComponent<UnityEngine.UI.CanvasScaler>();
        canvasObj.AddComponent<UnityEngine.UI.GraphicRaycaster>();

        GameObject obj = new GameObject("FogOverlay");
        obj.transform.SetParent(canvasObj.transform, false);

        overlay = obj.AddComponent<UnityEngine.UI.Image>();
        overlayRT = obj.GetComponent<RectTransform>();
        overlayRT.anchorMin = Vector2.zero;
        overlayRT.anchorMax = Vector2.one;
        overlayRT.offsetMin = Vector2.zero;
        overlayRT.offsetMax = Vector2.zero;

        Shader shader = Shader.Find("UI/TorchEffect");
        if (shader != null)
        {
            fogMaterial = new Material(shader);
            overlay.material = fogMaterial;
            overlay.color = Color.white;
        }
        else
        {
            Debug.LogWarning("UI/TorchEffect shader not found!");
            overlay.color = darknessColor;
        }

        overlay.gameObject.SetActive(false);
        canvasObj.SetActive(true);
    }

    void Update()
    {
        if (!isActive || player == null || mainCam == null) return;

        if (fogMaterial != null)
        {
            // Get player screen position - use center of sprite not feet
            Vector3 worldPos = player.position + new Vector3(0, 1.7f, 0);
            Vector2 screenPos = mainCam.WorldToScreenPoint(worldPos);

            // Convert screen pos to viewport pos (0-1 range)
            Vector2 viewportPos = new Vector2(
                screenPos.x / Screen.width,
                screenPos.y / Screen.height
            );

            fogMaterial.SetVector("_PlayerPos", new Vector4(viewportPos.x, viewportPos.y, 0, 0));
            fogMaterial.SetFloat("_Radius", radius / Screen.width);
            fogMaterial.SetColor("_DarknessColor", darknessColor);
        }
    }

    public void SetActive(bool active)
    {
        isActive = active;
        if (overlay != null)
            overlay.gameObject.SetActive(active);
        Debug.Log($"[FOG] {(active ? "activated" : "deactivated")}");
    }
}