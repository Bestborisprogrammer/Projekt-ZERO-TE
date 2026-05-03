using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class MainMenuController : MonoBehaviour
{
    [Header("Background")]
    public RectTransform backgroundImage;
    public float parallaxStrength = 30f;
    public float smoothSpeed = 5f;

    [Header("Scene")]
    public string gameScene = "OverworldScene";

    private Vector2 originalPos;
    private Vector2 targetPos;

    void Start()
    {
        originalPos = backgroundImage.anchoredPosition;
    }

    void Update()
    {
        // Get mouse position normalized -1 to 1
        float mouseX = (Input.mousePosition.x / Screen.width) * 2f - 1f;
        float mouseY = (Input.mousePosition.y / Screen.height) * 2f - 1f;

        // Calculate target offset
        targetPos = new Vector2(
            originalPos.x + mouseX * parallaxStrength,
            originalPos.y + mouseY * parallaxStrength
        );

        // Smooth movement
        backgroundImage.anchoredPosition = Vector2.Lerp(
            backgroundImage.anchoredPosition,
            targetPos,
            Time.deltaTime * smoothSpeed
        );
    }

    public void StartGame()
    {
        SceneManager.LoadScene(gameScene);
    }
}