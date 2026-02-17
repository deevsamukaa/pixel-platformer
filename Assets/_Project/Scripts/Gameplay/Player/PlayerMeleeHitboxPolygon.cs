using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerMeleeHitboxPolygon : MonoBehaviour
{
    [Header("Refs")]
    [SerializeField] private SpriteRenderer visualSprite; // pra saber facing (flipX)
    [SerializeField] private Transform hitboxRoot;        // objeto "Hitboxes" (filho do Visual)

    [Header("Hitboxes (1..3) - PolygonCollider2D Trigger")]
    [SerializeField] private PolygonCollider2D[] hitboxes = new PolygonCollider2D[3];

    [Header("Layers")]
    [SerializeField] private LayerMask hittableLayers;

    [Header("Dano/Knockback (1..3)")]
    [SerializeField] private int[] damages = { 1, 1, 2 };
    [SerializeField] private float[] knockbacks = { 9f, 10f, 12f };
    [SerializeField] private float knockUpBias = 0.35f;

    [Header("Janela do hit (segundos)")]
    [SerializeField] private float hitEnableTime = 0.03f; // liga collider por 1-2 frames

    [Header("Hit VFX (enviado via DamageInfo; spawn ocorre no Damageable)")]
    [SerializeField] private GameObject hitVfxPrefab;

    [Header("Debug")]
    [SerializeField] private bool debugLog = false;

    // anti multi-hit por swing
    private readonly HashSet<int> _hitInstanceIdsThisSwing = new HashSet<int>(16);

    // reutiliza lista pra evitar GC
    private readonly List<Collider2D> _results = new List<Collider2D>(16);

    private void Awake()
    {
        if (visualSprite == null)
        {
            var visual = transform.Find("Visual");
            if (visual != null) visualSprite = visual.GetComponentInChildren<SpriteRenderer>();
        }

        if (hitboxRoot == null)
        {
            var hb = transform.Find("Visual/Hitboxes");
            if (hb != null) hitboxRoot = hb;
        }

        // garante que começam desligados
        for (int i = 0; i < hitboxes.Length; i++)
            if (hitboxes[i] != null) hitboxes[i].enabled = false;
    }

    private int GetFacing()
    {
        if (visualSprite != null)
            return visualSprite.flipX ? -1 : 1;

        return transform.localScale.x < 0 ? -1 : 1;
    }

    /// <summary>
    /// Chame via Animation Event no frame do impacto.
    /// attackIndex: 1..3
    /// </summary>
    public void AnimEvent_PolygonHit(int attackIndex)
    {
        _hitInstanceIdsThisSwing.Clear();

        if (debugLog)
            Debug.Log($"[PolygonHit] AnimEvent_PolygonHit idx={attackIndex} em {gameObject.name}");

        int i = Mathf.Clamp(attackIndex, 1, 3) - 1;
        if (i < 0 || i >= hitboxes.Length) return;
        if (hitboxes[i] == null) return;

        // ✅ espelha o hitboxRoot junto com o facing
        if (hitboxRoot != null)
        {
            int facing = GetFacing();
            Vector3 s = hitboxRoot.localScale;
            s.x = Mathf.Abs(s.x) * facing;
            hitboxRoot.localScale = s;
        }

        StartCoroutine(EnableHitboxBriefly(i));
    }

    private IEnumerator EnableHitboxBriefly(int i)
    {
        var hb = hitboxes[i];
        hb.enabled = true;

        try
        {
            DoOverlapDamage(i, hb);
            // Realtime pra não quebrar com HitStop/timeScale
            yield return new WaitForSecondsRealtime(hitEnableTime);
        }
        finally
        {
            hb.enabled = false;
        }
    }

    private void DoOverlapDamage(int i, PolygonCollider2D hb)
    {
        _results.Clear();

        var filter = new ContactFilter2D
        {
            useLayerMask = true,
            layerMask = hittableLayers,
            useTriggers = true
        };

        hb.Overlap(filter, _results);

        if (debugLog)
            Debug.Log($"[PolygonHit] hitbox {i + 1} overlaps={_results.Count}");

        int facing = GetFacing();
        int damage = (i < damages.Length) ? damages[i] : 1;
        float kb = (i < knockbacks.Length) ? knockbacks[i] : 0f;

        // referência do golpe (bom pro ClosestPoint ficar natural)
        Vector2 from = hb.bounds.center;

        foreach (var col in _results)
        {
            if (col == null) continue;

            var dmg = col.GetComponentInParent<IDamageable>();
            if (dmg == null) continue;

            // anti multi-hit por swing no mesmo alvo (usa GO de quem implementa a interface)
            var damageableMb = dmg as MonoBehaviour;
            int key = damageableMb != null ? damageableMb.gameObject.GetInstanceID() : col.gameObject.GetInstanceID();
            if (_hitInstanceIdsThisSwing.Contains(key)) continue;
            _hitInstanceIdsThisSwing.Add(key);

            // direção do knockback
            Vector2 dir = new Vector2(facing, 0f);
            dir.y = Mathf.Max(dir.y, knockUpBias);
            dir.Normalize();

            // ponto/normal do impacto
            Vector2 hitPoint = col.ClosestPoint(from);

            Vector2 hitNormal = (hitPoint - from);
            if (hitNormal.sqrMagnitude < 0.0001f) hitNormal = dir;
            hitNormal.Normalize();

            // DamageInfo com os novos parâmetros (VFX enviada; spawn no Damageable)
            var info = new DamageInfo(
                damage,
                dir,
                kb,
                gameObject,
                hitPoint,
                hitNormal,
                hitVfxPrefab
            );

            dmg.TakeDamage(info);

            // feedback
            HitStop.Do(0.035f, 0.08f);

            var flash = col.GetComponentInParent<FlashOnHit>();
            flash?.Flash();
        }
    }
}
