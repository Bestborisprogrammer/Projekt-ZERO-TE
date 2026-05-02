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

        // Create overlay
        GameObject obj = new GameObject("FogOverlay");
        obj.transform.SetParent(canvas.transform, false);
        obj.transform.SetAsLastSibling();

        overlay = obj.AddComponent<Image>();
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
    }

    void Update()
    {
        if (!isActive || player == null || mainCam == null) return;

        // Check zone transitions
        if (exitZone != null && exitZone.bounds.Contains(player.position))
        {
            SetActive(false);
            return;
        }
        if (enterZone != null && enterZone.bounds.Contains(player.position))
        {
            if (!isActive) SetActive(true);
        }

        // Update shader
        if (fogMaterial != null)
        {
            Vector2 screenPos = mainCam.WorldToScreenPoint(player.position);
            RectTransformUtility.ScreenPointToLocalPointInRectangle(
                overlayRT, screenPos, null, out Vector2 localPos);

            Vector2 size = overlayRT.rect.size;
            Vector2 normalized = new Vector2(
                (localPos.x + size.x * 0.5f) / size.x,
                (localPos.y + size.y * 0.5f) / size.y);

            fogMaterial.SetVector("_PlayerPos", normalized);
            fogMaterial.SetFloat("_Radius", radius / size.x);
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