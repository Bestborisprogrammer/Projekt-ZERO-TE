using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;

public class CombatAnimator : MonoBehaviour
{
    public static CombatAnimator Instance;

    [Header("Dash / Movement")]
    public float dashSpeed = 850f;
    public float returnSpeed = 650f;
    public float dashStopDistance = 75f;

    [Header("Timing (seconds)")]
    public float postHitPause = 0.12f;
    public float postAttackPause = 0.08f;
    public float inPlaceHoldTime = 0.24f;

    [Header("Camera Zoom")]
    public float zoomAmount = 1.10f;
    public float zoomInDuration = 0.18f;
    public float zoomOutDuration = 0.26f;
    public AnimationCurve zoomCurve;

    private class CombatantData
    {
        public RectTransform rect;      // container RT — what we slide
        public Image image;     // the Image we write sprites to
        public AnimationClip idle;
        public AnimationClip dash;
        public AnimationClip attack;
        public AnimationClip mana;
        public AnimationClip guard;
        public AnimationClip item;
        public Coroutine animCoroutine;
    }

    private Dictionary<string, CombatantData> dataMap = new();
    private HashSet<string> guardingNames = new();
    private float _baseCamSize = -1f;
    private Camera _cam;

    // All LayoutGroups we disabled this sequence (restored after)
    private List<LayoutGroup> _disabledLayouts = new();

    static string ClipName(AnimationClip c) => (c != null && c) ? c.name : "NULL";
    static AnimationClip ValidClip(AnimationClip c) => (c != null && c) ? c : null;

    void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    // ── Registration ──────────────────────────────────────────────────────────

    public void RegisterCombatant(string name, RectTransform containerRect, Image image,
        AnimationClip idle, AnimationClip dash, AnimationClip attack,
        AnimationClip mana, AnimationClip guard, AnimationClip item)
    {
        dataMap[name] = new CombatantData
        {
            rect = containerRect,
            image = image,
            idle = ValidClip(idle),
            dash = ValidClip(dash),
            attack = ValidClip(attack),
            mana = ValidClip(mana),
            guard = ValidClip(guard),
            item = ValidClip(item),
        };

        string imgPath = image != null ? GetPath(image.gameObject) : "NULL";
        Debug.Log($"[ANIM] Registered: {name} | img={imgPath} | " +
            $"idle={ClipName(dataMap[name].idle)} dash={ClipName(dataMap[name].dash)} " +
            $"attack={ClipName(dataMap[name].attack)} mana={ClipName(dataMap[name].mana)} " +
            $"guard={ClipName(dataMap[name].guard)} item={ClipName(dataMap[name].item)}");
    }

    static string GetPath(GameObject go)
    {
        string p = go.name;
        Transform t = go.transform.parent;
        while (t != null) { p = t.name + "/" + p; t = t.parent; }
        return p;
    }

    public void SetAllIdle(List<Combatant> party, List<Combatant> enemies)
    {
        foreach (var c in party) SwitchAnim(c.Name, Get(c.Name)?.idle, loop: true);
        foreach (var c in enemies) SwitchAnim(c.Name, Get(c.Name)?.idle, loop: true);
    }

    public void Clear()
    {
        foreach (var kv in dataMap) StopAnimOnly(kv.Key);
        dataMap.Clear();
        guardingNames.Clear();
        _baseCamSize = -1f;
        RestoreAllLayouts();
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

    // ── Layout freeze helpers ─────────────────────────────────────────────────
    // Disables ALL LayoutGroups in the hierarchy of the given RectTransform
    // (walks up to Canvas root) so that writing Image.sprite doesn't trigger
    // a layout recalculation that resets our hand-driven position.

    void FreezeLayouts(RectTransform rt)
    {
        Transform t = rt.transform;
        while (t != null)
        {
            var lg = t.GetComponent<LayoutGroup>();
            if (lg != null && lg.enabled)
            {
                lg.enabled = false;
                _disabledLayouts.Add(lg);
            }
            // Stop at Canvas root
            if (t.GetComponent<Canvas>() != null) break;
            t = t.parent;
        }
        // Also check children of the container itself
        foreach (var lg in rt.GetComponentsInChildren<LayoutGroup>())
        {
            if (lg.enabled)
            {
                lg.enabled = false;
                if (!_disabledLayouts.Contains(lg))
                    _disabledLayouts.Add(lg);
            }
        }
    }

    void RestoreAllLayouts()
    {
        foreach (var lg in _disabledLayouts)
            if (lg != null) lg.enabled = true;
        _disabledLayouts.Clear();
    }

    // ── Attack sequence ───────────────────────────────────────────────────────
    // Strict order: freeze layouts → dash slide → attack clip at dash pos →
    //               damage → return slide → restore layouts → idle

    IEnumerator AttackSequence(string attackerName, string targetName,
        bool isMana, System.Action onDamagePoint, System.Action onComplete)
    {
        var a = Get(attackerName);
        var t = Get(targetName);

        if (a == null || t == null)
        {
            Debug.LogWarning($"[ANIM] AttackSequence: missing data for " +
                $"attacker='{attackerName}'({a != null}) target='{targetName}'({t != null})");
            onDamagePoint?.Invoke();
            onComplete?.Invoke();
            yield break;
        }

        AnimationClip attackClip = isMana ? a.mana : a.attack;

        // Let layout settle one frame before we freeze it
        yield return null;

        // ── Freeze ALL layout groups that could fight us ───────────────────
        FreezeLayouts(a.rect);

        // Sample positions NOW (layout is frozen, positions are stable)
        Vector3 originWorld = a.rect.position;
        Vector3 targetWorld = t.rect.position;
        Vector3 dir = (targetWorld - originWorld).normalized;
        float dist = Vector3.Distance(originWorld, targetWorld);
        Vector3 dashWorld = originWorld + dir * Mathf.Max(0f, dist - dashStopDistance);
        bool goingRight = targetWorld.x > originWorld.x;

        Debug.Log($"[ANIM] AttackSequence {attackerName}->{targetName} | " +
            $"clip={ClipName(attackClip)} | origin={originWorld:F1} dash={dashWorld:F1}");

        // ── 1. Dash slide (loop dash anim during movement) ────────────────
        StopAnimOnly(attackerName);
        SetFlipX(a.image, !goingRight);
        // Show first frame of dash clip immediately
        if (a.dash != null) SampleSprite(a.image, a.dash, 0f);
        // Start dash anim loop and slide simultaneously
        a.animCoroutine = StartCoroutine(LoopClip(a, a.dash));
        StartCoroutine(ZoomCamera(zoomAmount, zoomInDuration));
        yield return StartCoroutine(SlideWorld(a.rect, originWorld, dashWorld, dashSpeed));

        // Dash done — snap to exact dash position, stop dash anim
        a.rect.position = dashWorld;
        StopAnimOnly(attackerName);

        Debug.Log($"[ANIM] Dash done. rect.position={a.rect.position:F1}  now playing attack clip");

        // ── 2. Attack clip plays AT dash position ─────────────────────────
        // We manually sample every frame so we control both the sprite AND
        // the position each frame — layout can't interfere
        if (attackClip != null && attackClip.length > 0f)
        {
            float e = 0f;
            while (e < attackClip.length)
            {
                a.rect.position = dashWorld; // pin position every frame
                SampleSprite(a.image, attackClip, e);
                e += Time.deltaTime;
                yield return null;
            }
            // Hold last frame briefly
            a.rect.position = dashWorld;
        }
        else
        {
            yield return new WaitForSeconds(0.25f);
        }

        // ── 3. Damage point ───────────────────────────────────────────────
        onDamagePoint?.Invoke();
        yield return new WaitForSeconds(postHitPause);

        // ── 4. Return slide ───────────────────────────────────────────────
        SetFlipX(a.image, goingRight);
        if (a.dash != null) SampleSprite(a.image, a.dash, 0f);
        a.animCoroutine = StartCoroutine(LoopClip(a, a.dash));
        yield return new WaitForSeconds(postAttackPause);
        yield return StartCoroutine(SlideWorld(a.rect, dashWorld, originWorld, returnSpeed));

        // Snap back and clean up
        a.rect.position = originWorld;
        StopAnimOnly(attackerName);
        SetFlipX(a.image, false);

        // ── 5. Restore layouts (Unity snaps container to its slot) ────────
        RestoreAllLayouts();

        // ── 6. Zoom out + idle ────────────────────────────────────────────
        StartCoroutine(ZoomCamera(1f, zoomOutDuration));
        if (!guardingNames.Contains(attackerName))
            SwitchAnim(attackerName, a.idle, loop: true);

        onComplete?.Invoke();
    }

    // ── In-place sequence (mana support / item) ───────────────────────────────

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
        float dur = (clip != null && clip.length > 0f) ? clip.length : inPlaceHoldTime;

        Debug.Log($"[ANIM] InPlaceSequence: {name} key={clipKey} clip={ClipName(clip)} dur={dur:F2}s");

        FreezeLayouts(d.rect);
        Vector3 stayPos = d.rect.position;

        StopAnimOnly(name);

        if (clip != null && clip.length > 0f)
        {
            float e = 0f;
            while (e < clip.length)
            {
                d.rect.position = stayPos;
                SampleSprite(d.image, clip, e);
                e += Time.deltaTime;
                yield return null;
            }
        }
        else
        {
            yield return new WaitForSeconds(dur);
        }

        onEffectPoint?.Invoke();
        yield return new WaitForSeconds(0.08f);

        RestoreAllLayouts();

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

        if (loop) d.animCoroutine = StartCoroutine(LoopClip(d, clip));
        else if (hold) d.animCoroutine = StartCoroutine(PlayOnceHold(d, clip));
        else d.animCoroutine = StartCoroutine(PlayOnceThenIdle(d, clip));
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

    // ── Animation coroutines ──────────────────────────────────────────────────

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
        if (clip != null && clip.length > 0f)
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
        if (clip == null || clip.length <= 0f) yield break;
        Sprite last = null;
        float e = 0f;
        while (e < clip.length)
        {
            last = SampleSprite(d.image, clip, e);
            e += Time.deltaTime;
            yield return null;
        }
        // Freeze last frame — coroutine ends, sprite stays
        if (last != null && d.image != null)
            d.image.sprite = last;
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
        Debug.LogWarning($"[ANIM] SampleSprite: '{ClipName(clip)}' has no m_Sprite " +
            $"object-reference curve. Clip likely targets SpriteRenderer not Image.");
#endif
        return null;
    }

    // ── Movement ──────────────────────────────────────────────────────────────

    IEnumerator SlideWorld(RectTransform rect, Vector3 from, Vector3 to, float speed)
    {
        float dist = Vector3.Distance(from, to);
        float dur = (speed > 0f && dist > 0.1f) ? dist / speed : 0f;
        if (dur <= 0f) { rect.position = to; yield break; }

        float elapsed = 0f;
        while (elapsed < dur)
        {
            elapsed += Time.deltaTime;
            rect.position = Vector3.Lerp(from, to, Mathf.Clamp01(elapsed / dur));
            yield return null;
        }
        rect.position = to;
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
            float tt = Mathf.Clamp01(e / dur);
            float et = (zoomCurve != null && zoomCurve.length > 1)
                ? zoomCurve.Evaluate(tt) : Mathf.SmoothStep(0f, 1f, tt);
            _cam.orthographicSize = Mathf.Lerp(start, target, et);
            yield return null;
        }
        _cam.orthographicSize = target;
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    CombatantData Get(string name) =>
        dataMap.TryGetValue(name, out var d) ? d : null;

    void SetFlipX(Image img, bool flip)
    {
        if (img == null) return;
        Vector3 s = img.rectTransform.localScale;
        s.x = flip ? -Mathf.Abs(s.x) : Mathf.Abs(s.x);
        img.rectTransform.localScale = s;
    }
}