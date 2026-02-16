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

    [Header("Debug")]
    [SerializeField] private bool debugLog = false;

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
        Debug.Log($"AnimEvent_PolygonHit chamado! idx={attackIndex} em {gameObject.name}");
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

        // liga por micro-janela (evita “acertar atrás” e evita multi-hit)
        StartCoroutine(EnableHitboxBriefly(i));
    }

    private System.Collections.IEnumerator EnableHitboxBriefly(int i)
    {
        var hb = hitboxes[i];
        hb.enabled = true;
        Debug.Log($"Hitbox {(i + 1)} enabled = {hb.enabled} ({hb.name})");

        // coleta overlaps no momento exato
        DoOverlapDamage(i, hb);

        yield return new WaitForSeconds(hitEnableTime);

        hb.enabled = false;
    }

    private void DoOverlapDamage(int i, PolygonCollider2D hb)
    {
        _results.Clear();

        ContactFilter2D filter = new ContactFilter2D
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

        foreach (var col in _results)
        {
            if (col == null) continue;

            var dmg = col.GetComponentInParent<IDamageable>();
            if (dmg == null) continue;

            Vector2 dir = new Vector2(facing, 0f);
            dir.y = Mathf.Max(dir.y, knockUpBias);

            dmg.TakeDamage(new DamageInfo(damage, dir.normalized, kb, gameObject));

            HitStop.Do(0.035f, 0.08f);

            var flash = col.transform.GetComponentInChildren<FlashOnHit>();
            flash?.Flash();
        }
    }
}
