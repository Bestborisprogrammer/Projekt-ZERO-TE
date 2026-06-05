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

    [Header("Font")]
    public TMP_FontAsset combatFont;

    [Header("Canvas")]
    public Canvas combatCanvas;

    private Dictionary<string, Image> spriteMap = new();
    private Dictionary<string, RectTransform> rectMap = new();
    private Dictionary<string, TextMeshProUGUI> enemyLabelMap = new();
    private Dictionary<string, Image> enemyContainerBGMap = new();
    private Dictionary<string, TextMeshProUGUI> statusTextMap = new();
    private List<string> enemyNameOrder = new();

    private Color normalColor = new Color(0f, 0f, 0f, 0.5f);
    private Color selectedColor = new Color(0.2f, 0.8f, 0.2f, 0.7f);
    private Color defeatedColor = new Color(0.1f, 0.1f, 0.1f, 0.8f);

    void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    void SetFont(TextMeshProUGUI tmp)
    {
        if (combatFont != null && tmp != null)
            tmp.font = combatFont;
    }

    public void SetupSprites(List<Combatant> party, List<Combatant> enemies)
    {
        if (combatSpritePrefab == null) { Debug.LogError("combatSpritePrefab NULL!"); return; }
        if (partySpritesParent == null) { Debug.LogError("partySpritesParent NULL!"); return; }
        if (enemySpritesParent == null) { Debug.LogError("enemySpritesParent NULL!"); return; }

        spriteMap.Clear();
        rectMap.Clear();
        enemyLabelMap.Clear();
        enemyContainerBGMap.Clear();
        statusTextMap.Clear();
        enemyNameOrder.Clear();

        foreach (Transform child in partySpritesParent) Destroy(child.gameObject);
        foreach (Transform child in enemySpritesParent) Destroy(child.gameObject);

        // ── Party ──────────────────────────────────
        foreach (var member in party)
        {
            var so = PartyManager.Instance.allMembers
                .Find(m => m.Name == member.Name)?.baseData;

            GameObject container = new GameObject($"Party_{member.Name}");
            container.transform.SetParent(partySpritesParent, false);
            var cRT = container.AddComponent<RectTransform>();
            cRT.sizeDelta = new Vector2(160, 200);

            var cLayout = container.AddComponent<VerticalLayoutGroup>();
            cLayout.childAlignment = TextAnchor.LowerCenter;
            cLayout.spacing = 2;
            cLayout.childControlHeight = false;
            cLayout.childControlWidth = true;
            cLayout.childForceExpandHeight = false;
            cLayout.childForceExpandWidth = true;

            GameObject statusObj = new GameObject("Status");
            statusObj.transform.SetParent(container.transform, false);
            var statusRT = statusObj.AddComponent<RectTransform>();
            statusRT.sizeDelta = new Vector2(155, 24);
            var statusTMP = statusObj.AddComponent<TextMeshProUGUI>();
            statusTMP.fontSize = 11;
            statusTMP.alignment = TextAlignmentOptions.Center;
            statusTMP.color = Color.yellow;
            statusTMP.text = "";
            statusTMP.raycastTarget = false;
            SetFont(statusTMP);
            statusTextMap[member.Name] = statusTMP;

            GameObject spriteObj = Instantiate(combatSpritePrefab, container.transform);
            spriteObj.name = member.Name;
            var img = spriteObj.GetComponent<Image>();
            if (so != null && so.portrait != null) img.sprite = so.portrait;
            var sRT = spriteObj.GetComponent<RectTransform>();
            sRT.sizeDelta = new Vector2(155, 155);

            spriteMap[member.Name] = img;
            rectMap[member.Name] = sRT;
        }

        // ── Enemies ────────────────────────────────
        for (int i = 0; i < enemies.Count; i++)
        {
            var enemy = enemies[i];
            enemyNameOrder.Add(enemy.Name);

            EnemyStatsSO so = (i < EncounterManager.CurrentEnemies.Count)
                ? EncounterManager.CurrentEnemies[i] : null;

            int capturedIndex = i;

            GameObject container = new GameObject($"Enemy_{enemy.Name}");
            container.transform.SetParent(enemySpritesParent, false);
            var cRT = container.AddComponent<RectTransform>();
            cRT.sizeDelta = new Vector2(320, 420);

            var bg = container.AddComponent<Image>();
            bg.color = normalColor;
            enemyContainerBGMap[enemy.Name] = bg;

            var btn = container.AddComponent<Button>();
            btn.transition = Selectable.Transition.None;
            btn.onClick.AddListener(() =>
            {
                TurnCombatManager.Instance.SelectEnemy(capturedIndex);
                HighlightSelectedEnemy(capturedIndex);
            });

            var cLayout = container.AddComponent<VerticalLayoutGroup>();
            cLayout.childAlignment = TextAnchor.UpperCenter;
            cLayout.spacing = 8;
            cLayout.childControlHeight = false;
            cLayout.childControlWidth = true;
            cLayout.childForceExpandHeight = false;
            cLayout.childForceExpandWidth = true;
            cLayout.padding = new RectOffset(6, 6, 6, 6);

            // HP label
            GameObject labelObj = new GameObject("Label");
            labelObj.transform.SetParent(container.transform, false);
            var labelRT = labelObj.AddComponent<RectTransform>();
            labelRT.sizeDelta = new Vector2(300, 50);
            var labelBG = labelObj.AddComponent<Image>();
            labelBG.color = new Color(0, 0, 0, 0.65f);

            GameObject labelTextObj = new GameObject("Text");
            labelTextObj.transform.SetParent(labelObj.transform, false);
            var textRT = labelTextObj.AddComponent<RectTransform>();
            textRT.anchorMin = Vector2.zero;
            textRT.anchorMax = Vector2.one;
            textRT.offsetMin = Vector2.zero;
            textRT.offsetMax = Vector2.zero;
            var label = labelTextObj.AddComponent<TextMeshProUGUI>();
            label.fontSize = 22;
            label.alignment = TextAlignmentOptions.Center;
            label.color = Color.white;
            label.raycastTarget = false;
            label.text = $"{enemy.Name}  HP:{enemy.CurrentHP}/{enemy.MaxHP}";
            SetFont(label);
            enemyLabelMap[enemy.Name] = label;

            // Status text
            GameObject statusObj = new GameObject("Status");
            statusObj.transform.SetParent(container.transform, false);
            var statusRT = statusObj.AddComponent<RectTransform>();
            statusRT.sizeDelta = new Vector2(300, 35);
            var statusTMP = statusObj.AddComponent<TextMeshProUGUI>();
            statusTMP.fontSize = 16;
            statusTMP.alignment = TextAlignmentOptions.Center;
            statusTMP.color = Color.yellow;
            statusTMP.raycastTarget = false;
            statusTMP.text = "";
            SetFont(statusTMP);
            statusTextMap[enemy.Name] = statusTMP;

            // Sprite
            GameObject spriteObj = Instantiate(combatSpritePrefab, container.transform);
            spriteObj.name = enemy.Name;
            var spriteImg = spriteObj.GetComponent<Image>();
            spriteImg.raycastTarget = false;
            var spriteRT = spriteObj.GetComponent<RectTransform>();
            spriteRT.sizeDelta = new Vector2(300, 300);
            if (so != null && so.sprite != null) spriteImg.sprite = so.sprite;

            spriteMap[enemy.Name] = spriteImg;
            rectMap[enemy.Name] = spriteRT;
        }

        if (enemies.Count > 0)
            HighlightSelectedEnemy(0);
    }

    public void UpdateStatusIndicators(List<Combatant> allCombatants)
    {
        foreach (var combatant in allCombatants)
        {
            if (!statusTextMap.ContainsKey(combatant.Name)) continue;

            List<string> parts = new();

            if (combatant.IsBlocking) parts.Add("B!");
            else if (combatant.CombatStyle == CombatStyle.Evade && combatant.IsEvading) parts.Add("E!");

            List<StatModifier> mods = GetModifiers(combatant);
            foreach (var mod in mods)
            {
                if (mod.turnsRemaining <= 0) continue;
                string label = mod.statType switch
                {
                    StatType.ATK => mod.modifier > 0 ? "ATK+" : "ATK-",
                    StatType.DEF => mod.modifier > 0 ? "DEF+" : "DEF-",
                    StatType.SPD => mod.modifier > 0 ? "SPD+" : "SPD-",
                    StatType.HP => mod.modifier > 0 ? "HP+" : "HP-",
                    StatType.MP => mod.modifier > 0 ? "MP+" : "MP-",
                    _ => mod.modifier > 0 ? "UP" : "DWN"
                };
                parts.Add($"{label}({mod.turnsRemaining})");
            }

            List<ActiveStatusEffect> effects = GetEffects(combatant);
            foreach (var effect in effects)
            {
                if (effect.turnsRemaining <= 0) continue;
                string label = effect.type switch
                {
                    StatusEffectType.Burn => "BRN",
                    StatusEffectType.Poison => "PSN",
                    StatusEffectType.Freeze => "FRZ",
                    StatusEffectType.Paralyze => "PAR",
                    StatusEffectType.Wet => "WET",
                    StatusEffectType.Dark => "DRK",
                    _ => effect.type.ToString()
                };
                parts.Add($"{label}({effect.turnsRemaining})");
            }

            statusTextMap[combatant.Name].text = string.Join(" ", parts);
        }
    }

    List<StatModifier> GetModifiers(Combatant combatant)
    {
        if (combatant.IsEnemy)
        {
            var inst = TurnCombatManager.Instance?.GetEnemyInstance(combatant.Name);
            return inst?.statModifiers ?? new List<StatModifier>();
        }
        var member = PartyManager.Instance.activeParty.Find(m => m.Name == combatant.Name);
        return member?.statModifiers ?? new List<StatModifier>();
    }

    List<ActiveStatusEffect> GetEffects(Combatant combatant)
    {
        if (combatant.IsEnemy)
        {
            var inst = TurnCombatManager.Instance?.GetEnemyInstance(combatant.Name);
            return inst?.activeEffects ?? new List<ActiveStatusEffect>();
        }
        var member = PartyManager.Instance.activeParty.Find(m => m.Name == combatant.Name);
        return member?.activeEffects ?? new List<ActiveStatusEffect>();
    }

    public void UpdateEnemyLabels(List<Combatant> enemies)
    {
        foreach (var enemy in enemies)
        {
            if (!enemyLabelMap.ContainsKey(enemy.Name)) continue;
            enemyLabelMap[enemy.Name].text = enemy.IsAlive
                ? $"{enemy.Name}  HP:{enemy.CurrentHP}/{enemy.MaxHP}"
                : $"{enemy.Name}  DEFEATED";
        }
    }

    public void HighlightSelectedEnemy(int index)
    {
        for (int i = 0; i < enemyNameOrder.Count; i++)
        {
            string name = enemyNameOrder[i];
            if (!enemyContainerBGMap.ContainsKey(name)) continue;
            bool isDefeated = spriteMap.ContainsKey(name) &&
                spriteMap[name].color.r < 0.5f &&
                spriteMap[name].color.g < 0.5f;
            enemyContainerBGMap[name].color = isDefeated ? defeatedColor
                : i == index ? selectedColor : normalColor;
        }
    }

    public void PlayHitEffect(string name, int damage = 0, bool isCrit = false)
    {
        if (!spriteMap.ContainsKey(name)) return;
        StartCoroutine(ShakeAndFlash(name));
        if (damage > 0 && combatCanvas != null)
            ShowDamageNumber(name, damage, false, isCrit);
    }

    public void ShowDamageNumber(string targetName, int damage,
        bool isHeal = false, bool isCrit = false)
    {
        if (combatCanvas == null) return;
        if (!rectMap.ContainsKey(targetName)) return;

        var rt = rectMap[targetName];

        GameObject dmgObj = new GameObject("DamageNumber");
        dmgObj.transform.SetParent(combatCanvas.transform, false);

        var tmp = dmgObj.AddComponent<TextMeshProUGUI>();
        tmp.text = isHeal ? $"+{damage}" : $"-{damage}";
        tmp.fontSize = isCrit ? 48 : 38;
        tmp.fontStyle = FontStyles.Bold;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.raycastTarget = false;
        SetFont(tmp);

        if (isHeal) tmp.color = Color.green;
        else if (isCrit) tmp.color = new Color(1f, 0.85f, 0f);
        else tmp.color = Color.red;

        var dmgRT = dmgObj.GetComponent<RectTransform>();
        dmgRT.sizeDelta = new Vector2(isCrit ? 200 : 150, 60);

        Vector3[] corners = new Vector3[4];
        rt.GetWorldCorners(corners);
        Vector3 topCenter = (corners[1] + corners[2]) / 2f;
        topCenter.y += 40f;
        topCenter.x += Random.Range(-30f, 30f);
        dmgRT.position = topCenter;

        Vector2 floatDir = new Vector2(
            Random.Range(-40f, 40f),
            Random.Range(isCrit ? 120f : 80f, isCrit ? 180f : 140f));

        StartCoroutine(AnimateDamageNumber(dmgObj, tmp, dmgRT, floatDir, isHeal, isCrit));
    }

    IEnumerator AnimateDamageNumber(GameObject obj, TextMeshProUGUI tmp,
        RectTransform rt, Vector2 floatDir, bool isHeal = false, bool isCrit = false)
    {
        float duration = isCrit ? 1.6f : 1.2f;
        float elapsed = 0f;
        Vector3 startPos = rt.position;
        bool flashA = true;
        float flashInterval = isCrit ? 0.06f : 0.08f;
        float nextFlash = 0f;

        Color colorA = isHeal ? Color.green
            : isCrit ? new Color(1f, 0.85f, 0f) : Color.red;
        Color colorB = Color.white;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / duration;

            rt.position = startPos + new Vector3(floatDir.x * t, floatDir.y * t, 0);

            nextFlash -= Time.deltaTime;
            if (nextFlash <= 0f)
            {
                flashA = !flashA;
                Color c = flashA ? colorA : colorB;
                c.a = 1f - (t * t);
                tmp.color = c;
                nextFlash = flashInterval;
            }

            float scale = t < 0.2f
                ? Mathf.Lerp(isCrit ? 0.3f : 0.5f, isCrit ? 1.5f : 1.3f, t / 0.2f)
                : Mathf.Lerp(isCrit ? 1.5f : 1.3f, 0.8f, (t - 0.2f) / 0.8f);
            rt.localScale = Vector3.one * scale;

            yield return null;
        }

        Destroy(obj);
    }

    public void ShowStatusTextAboveSprite(string name, string text)
    {
        if (!rectMap.ContainsKey(name) || combatCanvas == null) return;

        var rt = rectMap[name];

        GameObject obj = new GameObject("StatusPopup");
        obj.transform.SetParent(combatCanvas.transform, false);

        var tmp = obj.AddComponent<TextMeshProUGUI>();
        tmp.text = text;
        tmp.fontSize = 30;
        tmp.fontStyle = FontStyles.Bold;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.color = text == "B!" ? Color.cyan : Color.yellow;
        tmp.raycastTarget = false;
        SetFont(tmp);

        var popRT = obj.GetComponent<RectTransform>();
        popRT.sizeDelta = new Vector2(100, 50);

        Vector3[] corners = new Vector3[4];
        rt.GetWorldCorners(corners);
        Vector3 topCenter = (corners[1] + corners[2]) / 2f;
        topCenter.y += 40f;
        popRT.position = topCenter;

        StartCoroutine(AnimateStatusPopup(obj, tmp, popRT));
    }

    IEnumerator AnimateStatusPopup(GameObject obj, TextMeshProUGUI tmp, RectTransform rt)
    {
        Vector3 startPos = rt.position;
        float duration = 1.0f;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / duration;
            rt.position = startPos + new Vector3(0, 50f * t, 0);
            tmp.color = new Color(tmp.color.r, tmp.color.g, tmp.color.b, 1f - t);
            yield return null;
        }

        Destroy(obj);
    }

    public void PlayDefeatedEffect(string name)
    {
        if (!spriteMap.ContainsKey(name)) return;
        StartCoroutine(GreyOut(name));
        if (enemyContainerBGMap.ContainsKey(name))
            enemyContainerBGMap[name].color = defeatedColor;
    }

    public void PlayPartyDefeatedEffect(string name)
    {
        if (!spriteMap.ContainsKey(name)) return;
        StartCoroutine(GreyOut(name));
    }

    IEnumerator ShakeAndFlash(string name)
    {
        if (!rectMap.ContainsKey(name) || !spriteMap.ContainsKey(name)) yield break;
        var rt = rectMap[name];
        var img = spriteMap[name];
        Vector2 orig = rt.anchoredPosition;
        img.color = Color.red;
        float t = 0f;
        while (t < 0.3f)
        {
            rt.anchoredPosition = orig + new Vector2(
                Random.Range(-8f, 8f), Random.Range(-8f, 8f));
            t += Time.deltaTime;
            yield return null;
        }
        rt.anchoredPosition = orig;
        img.color = Color.white;
    }

    IEnumerator GreyOut(string name)
    {
        if (!spriteMap.ContainsKey(name)) yield break;
        var img = spriteMap[name];
        float t = 0f;
        Color start = img.color;
        Color grey = new Color(0.3f, 0.3f, 0.3f, 0.5f);
        while (t < 0.5f)
        {
            t += Time.deltaTime;
            img.color = Color.Lerp(start, grey, t / 0.5f);
            yield return null;
        }
        img.color = grey;
    }
}