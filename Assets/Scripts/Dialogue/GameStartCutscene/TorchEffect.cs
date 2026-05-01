using UnityEngine;
using UnityEngine.UI;

public class TorchEffect : MonoBehaviour
{
    [Header("Settings")]
    public float radius = 150f;
    public Color darknessColor = new Color(0, 0, 0, 0.95f);

    [Header("References")]
    public Transform player;
    public Canvas canvas;

    private Material torchMaterial;
    private Image darkOverlay;
    private RectTransform overlayRT;
    private Camera mainCam;

    void Start()
    {
        mainCam = Camera.main;

        // Create dark overlay with torch shader
        GameObject overlayObj = new GameObject("TorchOverlay");
        overlayObj.transform.SetParent(canvas.transform, false);
        overlayObj.transform.SetAsLastSibling();

        darkOverlay = overlayObj.AddComponent<Image>();
        overlayRT = overlayObj.GetComponent<RectTransform>();
        overlayRT.anchorMin = Vector2.zero;
        overlayRT.anchorMax = Vector2.one;
        overlayRT.offsetMin = Vector2.zero;
        overlayRT.offsetMax = Vector2.zero;

        // Use shader material
        torchMaterial = new Material(Shader.Find("UI/TorchEffect"));
        if (torchMaterial.shader.name == "Hidden/InternalErrorShader")
        {
            // Fallback if shader not found - use simple dark overlay
            Debug.LogWarning("TorchEffect shader not found - using fallback");
            darkOverlay.color = darknessColor;
            torchMaterial = null;
        }
        else
        {
            darkOverlay.material = torchMaterial;
            darkOverlay.color = Color.white;
        }

        gameObject.SetActive(true);
    }

    void Update()
    {
        if (player == null || mainCam == null) return;

        if (torchMaterial != null)
        {
            // Convert player world position to screen position
            Vector2 screenPos = mainCam.WorldToScreenPoint(player.position);

            // Convert to canvas space
            RectTransformUtility.ScreenPointToLocalPointInRectangle(
                overlayRT, screenPos, null, out Vector2 localPos);

            // Normalize to 0-1 range
            Vector2 canvasSize = overlayRT.rect.size;
            Vector2 normalizedPos = new Vector2(
                (localPos.x + canvasSize.x * 0.5f) / canvasSize.x,
                (localPos.y + canvasSize.y * 0.5f) / canvasSize.y
            );

            torchMaterial.SetVector("_PlayerPos", normalizedPos);
            torchMaterial.SetFloat("_Radius", radius / canvasSize.x);
            torchMaterial.SetColor("_DarknessColor", darknessColor);
        }
    }

    public void SetActive(bool active)
    {
        darkOverlay?.gameObject.SetActive(active);
    }
}