using UnityEngine;
using UnityEngine.UI;
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

    void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    public void SetupSprites(List<Combatant> party, List<Combatant> enemies)
    {
        spriteMap.Clear();
        rectMap.Clear();

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

            var rt = obj.GetComponent<RectTransform>();
            spriteMap[member.Name] = img;
            rectMap[member.Name] = rt;
        }

        // Enemy sprites – right side
        foreach (var enemy in enemies)
        {
            var so = EncounterManager.CurrentEnemies
                .Find(e => e.enemyName == enemy.Name);
            if (so == null) continue;

            GameObject obj = Instantiate(combatSpritePrefab, enemySpritesParent);
            obj.name = enemy.Name;

            var img = obj.GetComponent<Image>();
            if (so.sprite != null)
                img.sprite = so.sprite;

            var rt = obj.GetComponent<RectTransform>();
            spriteMap[enemy.Name] = img;
            rectMap[enemy.Name] = rt;
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
    }

    IEnumerator ShakeAndFlash(string name)
    {
        if (!rectMap.ContainsKey(name) || !spriteMap.ContainsKey(name)) yield break;

        var rt = rectMap[name];
        var img = spriteMap[name];
        Vector2 originalPos = rt.anchoredPosition;

        // Flash red
        img.color = Color.red;

        // Shake
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