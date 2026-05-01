using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.EventSystems;
using System.Collections;
using System.Collections.Generic;

public class CombatSpriteManager : MonoBehaviour
{
    public static CombatSpriteManager Instance;

    [Header("Parents")]
    public Transform partySpritesParent;
    public Transform enemySpritesParent;
    public GameObject combatSpritePrefab;

    private Dictionary<string, Image> spriteMap = new();
    private Dictionary<string, RectTransform> rectMap = new();
    private Dictionary<string, TextMeshProUGUI> enemyLabelMap = new();
    private Dictionary<string, Image> enemyContainerBGMap = new();
    private List<string> enemyNameOrder = new();

    // Colors
    private Color normalColor = new Color(0f, 0f, 0f, 0.5f);
    private Color selectedColor = new Color(0.2f, 0.8f, 0.2f, 0.7f);
    private Color defeatedColor = new Color(0.1f, 0.1f, 0.1f, 0.8f);

    void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    public void SetupSprites(List<Combatant> party, List<Combatant> enemies)
    {
        spriteMap.Clear();
        rectMap.Clear();
        enemyLabelMap.Clear();
        enemyContainerBGMap.Clear();
        enemyNameOrder.Clear();

        foreach (Transform child in partySpritesParent) Destroy(child.gameObject);
        foreach (Transform child in enemySpritesParent) Destroy(child.gameObject);

        // Party sprites – left side
        foreach (var member in party)
        {
            var so = PartyManager.Instance.allMembers
                .Find(m => m.Name == member.Name)?.baseData;
            if (so == null) continue;

            GameObject obj = Instantiate(combatSpritePrefab, partySpritesParent);
            obj.name = member.Name;

            var img = obj.GetComponent<Image>();
            if (so.portrait != null)
                img.sprite = so.portrait;

            spriteMap[member.Name] = img;
            rectMap[member.Name] = obj.GetComponent<RectTransform>();
        }

        // Enemy sprites – right side
        for (int i = 0; i < enemies.Count; i++)
        {
            var enemy = enemies[i];
            enemyNameOrder.Add(enemy.Name);

            var so = EncounterManager.CurrentEnemies
                .Find(e => e.enemyName == enemy.Name);
            if (so == null) continue;

            int capturedIndex = i;

            // Container
            GameObject container = new GameObject($"EnemyContainer_{enemy.Name}");
            container.transform.SetParent(enemySpritesParent, false);

            var containerRT = container.AddComponent<RectTransform>();
            containerRT.sizeDelta = new Vector2(240, 280);

            // Background highlight – green when selected
            var containerBG = container.AddComponent<Image>();
            containerBG.color = normalColor;
            enemyContainerBGMap[enemy.Name] = containerBG;

            // Make whole container clickable
            var btn = container.AddComponent<Button>();
            btn.onClick.AddListener(() =>
            {
                TurnCombatManager.Instance.SelectEnemy(capturedIndex);
                HighlightSelectedEnemy(capturedIndex);
            });

            // Remove default button transition so our color logic works
            var colors = btn.colors;
            colors.normalColor = Color.white;
            colors.highlightedColor = Color.white;
            colors.pressedColor = Color.white;
            colors.selectedColor = Color.white;
            btn.colors = colors;
            btn.transition = Selectable.Transition.None;

            var layout = container.AddComponent<VerticalLayoutGroup>();
            layout.childAlignment = TextAnchor.UpperCenter;
            layout.spacing = 4;
            layout.childControlHeight = false;
            layout.childControlWidth = true;
            layout.childForceExpandHeight = false;
            layout.childForceExpandWidth = true;
            layout.padding = new RectOffset(4, 4, 4, 4);

            // HP Label
            GameObject labelObj = new GameObject("HPLabel");
            labelObj.transform.SetParent(container.transform, false);

            var labelRT = labelObj.AddComponent<RectTransform>();
            labelRT.sizeDelta = new Vector2(122, 35);

            var label = labelObj.AddComponent<TextMeshProUGUI>();
            label.fontSize = 30;
            label.alignment = TextAlignmentOptions.Center;
            label.text = $"{enemy.Name}\nHP: {enemy.CurrentHP}/{enemy.MaxHP}";
            label.color = Color.white;
            label.raycastTarget = false;
            enemyLabelMap[enemy.Name] = label;

            // Sprite
            GameObject spriteObj = Instantiate(combatSpritePrefab, container.transform);
            spriteObj.name = enemy.Name;

            // Disable raycast on sprite so container gets the click
            var spriteImg = spriteObj.GetComponent<Image>();
            spriteImg.raycastTarget = false;

            var spriteRT = spriteObj.GetComponent<RectTransform>();
            spriteRT.sizeDelta = new Vector2(120, 120);

            if (so.sprite != null)
                spriteImg.sprite = so.sprite;

            spriteMap[enemy.Name] = spriteImg;
            rectMap[enemy.Name] = spriteRT;
        }

        // Auto highlight first enemy
        if (enemies.Count > 0)
            HighlightSelectedEnemy(0);
    }

    public void HighlightSelectedEnemy(int index)
    {
        for (int i = 0; i < enemyNameOrder.Count; i++)
        {
            string name = enemyNameOrder[i];
            if (!enemyContainerBGMap.ContainsKey(name)) continue;

            var bg = enemyContainerBGMap[name];

            // Check if defeated
            bool isDefeated = !spriteMap.ContainsKey(name) ||
                spriteMap[name].color == new Color(0.3f, 0.3f, 0.3f, 0.5f);

            if (isDefeated)
                bg.color = defeatedColor;
            else if (i == index)
                bg.color = selectedColor;
            else
                bg.color = normalColor;
        }
    }

    public void UpdateEnemyLabels(List<Combatant> enemies)
    {
        foreach (var enemy in enemies)
        {
            if (!enemyLabelMap.ContainsKey(enemy.Name)) continue;
            var label = enemyLabelMap[enemy.Name];
            if (label == null) continue;

            label.text = !enemy.IsAlive
                ? $"{enemy.Name}\nDefeated"
                : $"{enemy.Name}\nHP: {enemy.CurrentHP}/{enemy.MaxHP}";
        }
    }

    public void PlayHitEffect(string name)
    {
        if (!spriteMap.ContainsKey(name)) return;
        StartCoroutine(ShakeAndFlash(name));
    }

    public void PlayDefeatedEffect(string name)
    {
        if (!spriteMap.ContainsKey(name)) return;
        StartCoroutine(GreyOut(name));

        // Grey out container too
        if (enemyContainerBGMap.ContainsKey(name))
            enemyContainerBGMap[name].color = defeatedColor;
    }

    IEnumerator ShakeAndFlash(string name)
    {
        if (!rectMap.ContainsKey(name) || !spriteMap.ContainsKey(name)) yield break;

        var rt = rectMap[name];
        var img = spriteMap[name];
        Vector2 originalPos = rt.anchoredPosition;

        img.color = Color.red;

        float elapsed = 0f;
        float duration = 0.3f;
        float magnitude = 8f;

        while (elapsed < duration)
        {
            float x = originalPos.x + Random.Range(-magnitude, magnitude);
            float y = originalPos.y + Random.Range(-magnitude, magnitude);
            rt.anchoredPosition = new Vector2(x, y);
            elapsed += Time.deltaTime;
            yield return null;
        }

        rt.anchoredPosition = originalPos;
        img.color = Color.white;
    }

    IEnumerator GreyOut(string name)
    {
        if (!spriteMap.ContainsKey(name)) yield break;

        var img = spriteMap[name];
        float elapsed = 0f;
        float duration = 0.5f;
        Color start = img.color;
        Color grey = new Color(0.3f, 0.3f, 0.3f, 0.5f);

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            img.color = Color.Lerp(start, grey, elapsed / duration);
            yield return null;
        }

        img.color = grey;
    }
}