using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;

public class CombatAnimator : MonoBehaviour
{
    public static CombatAnimator Instance;

    [Header("Dash / Movement")]
    public float dashSpeed        = 850f;
    public float returnSpeed      = 650f;
    public float dashStopDistance = 75f;

    [Header("Timing (seconds)")]
    public float postHitPause    = 0.12f;
    public float postAttackPause = 0.08f;
    public float inPlaceHoldTime = 0.24f;

    [Header("Camera Zoom")]
    public float zoomAmount      = 1.10f;
    public float zoomInDuration  = 0.18f;
    public float zoomOutDuration = 0.26f;
    public AnimationCurve zoomCurve;

    private class CombatantData
    {
        public RectTransform rect;
        public Image         image;
        public AnimationClip idle;
        public AnimationClip dash;
        public AnimationClip attack;
        public AnimationClip mana;
        public AnimationClip guard;
        public AnimationClip item;
        public Coroutine     animCoroutine;
    }

    private Dictionary<string, CombatantData> dataMap       = new();
    private HashSet<string>                   guardingNames = new();
    private float  _baseCamSize = -1f;
    private Camera _cam;

    static string ClipName(AnimationClip c) => (c != null && c) ? c.name : "NULL";
    static AnimationClip ValidClip(AnimationClip c) => (c != null && c) ? c : null;

    void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    // ── Registration ──────────────────────────────────────────────────────────

    public void RegisterCombatant(string name, RectTransform rect, Image image,
        AnimationClip idle,  AnimationClip dash,  AnimationClip attack,
        AnimationClip mana,  AnimationClip guard, AnimationClip item)
    {
        idle   = ValidClip(idle);
        dash   = ValidClip(dash);
        attack = ValidClip(attack);
        mana   = ValidClip(mana);
        guard  = ValidClip(guard);
        item   = ValidClip(item);

        dataMap[name] = new CombatantData
        {
            rect = rect, image = image,
            idle = idle, dash = dash, attack = attack,
            mana = mana, guard = guard, item = item
        };

        // Log the actual GameObject path so we know WHICH Image we're writing to
        string imgPath = image != null ? GetPath(image.gameObject) : "NULL";
        Debug.Log($"[ANIM] Registered: {name} | image path: {imgPath} | " +
            $"idle={ClipName(idle)} dash={ClipName(dash)} attack={ClipName(attack)} " +
            $"mana={ClipName(mana)} guard={ClipName(guard)} item={ClipName(item)}");
    }

    static string GetPath(GameObject go)
    {
        string path = go.name;
        Transform t = go.transform.parent;
        while (t != null) { path = t.name + "/" + path; t = t.parent; }
        return path;
    }

    public void SetAllIdle(List<Combatant> party, List<Combatant> enemies)
    {
        foreach (var c in party)   SwitchAnim(c.Name, Get(c.Name)?.idle, loop: true);
        foreach (var c in enemies) SwitchAnim(c.Name, Get(c.Name)?.idle, loop: true);
    }

    public void Clear()
    {
        foreach (var kv in dataMap) StopAnimOnly(kv.Key);
        dataMap.Clear();
        guardingNames.Clear();
        _baseCamSize = -1f;
    }

    public void OnTurnStart(string name)
    {
        if (guardingNames.Remove(name))
            SwitchAnim(name, Get(name)?.idle, loop: true);
    }

    // ── Public entry points ───────────────────────────────────────────────────

    public void PlayAttack(string attackerName, string targetName,
        System.Action onDamagePoint, System.Action onComplete)
        => StartCoroutine(AttackSequence(attackerName, targetName, false, onDamagePoint, onComplete));

    public void PlayOffensiveMana(string attackerName, string targetName,
        System.Action onEffectPoint, System.Action onComplete)
        => StartCoroutine(AttackSequence(attackerName, targetName, true, onEffectPoint, onComplete));

    public void PlaySupportMana(string casterName,
        System.Action onEffectPoint, System.Action onComplete)
        => StartCoroutine(InPlaceSequence(casterName, "mana", onEffectPoint, onComplete));

    public void PlayGuard(string name, System.Action onComplete)
    {
        guardingNames.Add(name);
        SwitchAnim(name, Get(name)?.guard, loop: false, hold: true);
        onComplete?.Invoke();
    }

    public void PlayEvade(string name, System.Action onComplete)
    {
        guardingNames.Add(name);
        SwitchAnim(name, Get(name)?.guard, loop: false, hold: true);
        onComplete?.Invoke();
    }

    public void PlayItem(string name, System.Action onEffectPoint, System.Action onComplete)
    {
        Debug.Log($"[ANIM] PlayItem called for {name}");
        StartCoroutine(InPlaceSequence(name, "item", onEffectPoint, onComplete));
    }

    // ── Attack sequence ───────────────────────────────────────────────────────

    IEnumerator AttackSequence(string attackerName, string targetName,
        bool isMana, System.Action onDamagePoint, System.Action onComplete)
    {
        var a = Get(attackerName);
        var t = Get(targetName);

        if (a == null || t == null)
        {
            Debug.LogWarning($"[ANIM] AttackSequence missing data: " +
                $"attacker={attackerName}({a != null}) target={targetName}({t != null})");
            onDamagePoint?.Invoke();
            onComplete?.Invoke();
            yield break;
        }

        AnimationClip attackClip = isMana ? a.mana : a.attack;

        yield return null; // let layout settle

        Vector3 aw   = a.rect.position;
        Vector3 tw   = t.rect.position;
        Vector3 dir  = (tw - aw).normalized;
        float   dist = Vector3.Distance(aw, tw);

        Vector2 originAP = a.rect.anchoredPosition;
        Vector2 dashAP   = originAP + ScreenDeltaToAnchored(a.rect,
            (Vector2)(dir * Mathf.Max(0f, dist - dashStopDistance)));

        bool goingRight = tw.x > aw.x;

        Debug.Log($"[ANIM] AttackSequence {attackerName}->{targetName} | " +
            $"clip={ClipName(attackClip)} originAP={originAP} dashAP={dashAP}");

        // ── 1. Dash ───────────────────────────────────────────────────────────
        SwitchAnim(attackerName, a.dash, loop: true);
        SetFlipX(a.image, !goingRight);
        StartCoroutine(ZoomCamera(zoomAmount, zoomInDuration));
        yield return StartCoroutine(SlideRect(a.rect, originAP, dashAP, dashSpeed));

        Debug.Log($"[ANIM] {attackerName} reached target. anchoredPos={a.rect.anchoredPosition} " +
            $"worldPos={a.rect.position}  imageGO={a.image?.gameObject.name ?? "NULL"}");

        // ── 2. Attack anim ────────────────────────────────────────────────────
        float attackDur = (attackClip != null) ? attackClip.length : 0.2f;
        SwitchAnim(attackerName, attackClip, loop: false, hold: false);

        // Verify the first sprite sample immediately
        if (attackClip != null && a.image != null)
        {
            Sprite first = SampleSprite(a.image, attackClip, 0f);
            Debug.Log($"[ANIM] First frame sample: sprite={first?.name ?? "NULL"} " +
                $"image.sprite now={a.image.sprite?.name ?? "NULL"}");
        }

        yield return new WaitForSeconds(attackDur);

        // ── 3. Damage ─────────────────────────────────────────────────────────
        onDamagePoint?.Invoke();
        yield return new WaitForSeconds(postHitPause);

        // ── 4. Return ─────────────────────────────────────────────────────────
        SwitchAnim(attackerName, a.dash, loop: true);
        SetFlipX(a.image, goingRight);
        yield return new WaitForSeconds(postAttackPause);
        yield return StartCoroutine(SlideRect(a.rect, dashAP, originAP, returnSpeed));
        a.rect.anchoredPosition = originAP;
        SetFlipX(a.image, false);

        // ── 5. Zoom out + idle ────────────────────────────────────────────────
        StartCoroutine(ZoomCamera(1f, zoomOutDuration));
        if (!guardingNames.Contains(attackerName))
            SwitchAnim(attackerName, a.idle, loop: true);

        onComplete?.Invoke();
    }

    // ── In-place sequence ─────────────────────────────────────────────────────

    IEnumerator InPlaceSequence(string name, string clipKey,
        System.Action onEffectPoint, System.Action onComplete)
    {
        var d = Get(name);
        if (d == null)
        {
            Debug.LogWarning($"[ANIM] InPlaceSequence: no data for '{name}'");
            onEffectPoint?.Invoke();
            onComplete?.Invoke();
            yield break;
        }

        AnimationClip clip = clipKey == "item" ? d.item : d.mana;
        float dur = (clip != null) ? clip.length : inPlaceHoldTime;

        Debug.Log($"[ANIM] InPlaceSequence: {name} key={clipKey} " +
            $"clip={ClipName(clip)} dur={dur:F2}s  image={d.image?.gameObject.name ?? "NULL"}");

        if (clip != null)
        {
            SwitchAnim(name, clip, loop: false, hold: false);
            // Sample frame 0 immediately so we can confirm it's working
            Sprite s0 = SampleSprite(d.image, clip, 0f);
            Debug.Log($"[ANIM] InPlace frame0 sample: {s0?.name ?? "NULL"}  " +
                $"image.sprite={d.image?.sprite?.name ?? "NULL"}");
        }

        yield return new WaitForSeconds(dur);

        onEffectPoint?.Invoke();
        yield return new WaitForSeconds(0.08f);

        if (!guardingNames.Contains(name))
            SwitchAnim(name, d.idle, loop: true);

        onComplete?.Invoke();
    }

    // ── Animation switching ───────────────────────────────────────────────────

    void SwitchAnim(string name, AnimationClip clip, bool loop, bool hold = false)
    {
        var d = Get(name);
        if (d == null) return;
        StopAnimOnly(name);
        if (clip == null || d.image == null) return;

        if (loop)        d.animCoroutine = StartCoroutine(LoopClip(d, clip));
        else if (hold)   d.animCoroutine = StartCoroutine(PlayOnceHold(d, clip));
        else             d.animCoroutine = StartCoroutine(PlayOnceThenIdle(d, clip));
    }

    void StopAnimOnly(string name)
    {
        var d = Get(name);
        if (d?.animCoroutine != null)
        {
            StopCoroutine(d.animCoroutine);
            d.animCoroutine = null;
        }
    }

    // ── Coroutines ────────────────────────────────────────────────────────────

    IEnumerator LoopClip(CombatantData d, AnimationClip clip)
    {
        if (clip == null || clip.length <= 0f) yield break;
        while (true)
        {
            float e = 0f;
            while (e < clip.length)
            {
                SampleSprite(d.image, clip, e);
                e += Time.deltaTime;
                yield return null;
            }
        }
    }

    IEnumerator PlayOnceThenIdle(CombatantData d, AnimationClip clip)
    {
        if (clip != null)
        {
            float e = 0f;
            while (e < clip.length)
            {
                SampleSprite(d.image, clip, e);
                e += Time.deltaTime;
                yield return null;
            }
        }
        d.animCoroutine = StartCoroutine(LoopClip(d, d.idle));
    }

    IEnumerator PlayOnceHold(CombatantData d, AnimationClip clip)
    {
        Sprite last = null;
        float  e    = 0f;
        while (e < clip.length)
        {
            last = SampleSprite(d.image, clip, e);
            e += Time.deltaTime;
            yield return null;
        }
        if (last != null && d.image != null) d.image.sprite = last;
    }

    // ── Sprite sampler ────────────────────────────────────────────────────────

    Sprite SampleSprite(Image img, AnimationClip clip, float time)
    {
        if (img == null || clip == null) return null;

#if UNITY_EDITOR
        var bindings = UnityEditor.AnimationUtility.GetObjectReferenceCurveBindings(clip);
        foreach (var binding in bindings)
        {
            if (!binding.propertyName.Contains("m_Sprite")) continue;
            var keys = UnityEditor.AnimationUtility.GetObjectReferenceCurve(clip, binding);
            if (keys == null || keys.Length == 0) continue;

            Sprite chosen = keys[0].value as Sprite;
            for (int i = 1; i < keys.Length; i++)
            {
                if (keys[i].time <= time) chosen = keys[i].value as Sprite;
                else break;
            }
            if (chosen != null)
            {
                img.sprite = chosen;
                return chosen;
            }
        }

        // If no object reference curve found, the clip likely animates
        // SpriteRenderer not Image — log this so we know
        Debug.LogWarning($"[ANIM] SampleSprite: clip '{ClipName(clip)}' has no m_Sprite " +
            $"object reference curve. It probably targets SpriteRenderer, not Image. " +
            $"Re-create the clip targeting the Image component instead.");
#endif
        return null;
    }

    // ── Movement ──────────────────────────────────────────────────────────────

    IEnumerator SlideRect(RectTransform rect, Vector2 from, Vector2 to, float speed)
    {
        float dur = (speed > 0f) ? Vector2.Distance(from, to) / speed : 0f;
        if (dur <= 0f) { rect.anchoredPosition = to; yield break; }
        float elapsed = 0f;
        while (elapsed < dur)
        {
            elapsed += Time.deltaTime;
            rect.anchoredPosition = Vector2.Lerp(from, to, Mathf.Clamp01(elapsed / dur));
            yield return null;
        }
        rect.anchoredPosition = to;
    }

    // ── Camera zoom ───────────────────────────────────────────────────────────

    IEnumerator ZoomCamera(float mult, float dur)
    {
        if (_cam == null) _cam = Camera.main;
        if (_cam == null) yield break;
        if (_baseCamSize < 0f) _baseCamSize = _cam.orthographicSize;
        float start = _cam.orthographicSize;
        float target = _baseCamSize / mult;
        float e = 0f;
        while (e < dur)
        {
            e += Time.deltaTime;
            float t = Mathf.Clamp01(e / dur);
            float et = (zoomCurve != null && zoomCurve.length > 1)
                ? zoomCurve.Evaluate(t) : Mathf.SmoothStep(0f, 1f, t);
            _cam.orthographicSize = Mathf.Lerp(start, target, et);
            yield return null;
        }
        _cam.orthographicSize = target;
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    CombatantData Get(string name) =>
        dataMap.TryGetValue(name, out var d) ? d : null;

    Vector2 ScreenDeltaToAnchored(RectTransform rect, Vector2 screenDelta)
    {
        Canvas canvas = rect.GetComponentInParent<Canvas>();
        float sf = (canvas != null && canvas.scaleFactor > 0f) ? canvas.scaleFactor : 1f;
        return screenDelta / sf;
    }

    void SetFlipX(Image img, bool flip)
    {
        if (img == null) return;
        Vector3 s = img.rectTransform.localScale;
        s.x = flip ? -Mathf.Abs(s.x) : Mathf.Abs(s.x);
        img.rectTransform.localScale = s;
    }
}