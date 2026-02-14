using UnityEngine;

public class PlayerCombat : MonoBehaviour
{
    [Header("Refs")]
    [SerializeField] private Rigidbody2D rb;
    [SerializeField] private Transform attackPoint; // empty na mão do player
    [SerializeField] private LayerMask hittableLayers;

    [Header("Melee Combo (3 hits)")]
    [SerializeField] private float comboResetTime = 0.55f;
    [SerializeField] private float attackCooldown = 0.12f;
    [SerializeField] private float[] hitDurations = { 0.08f, 0.09f, 0.10f };
    [SerializeField] private Vector2[] hitBoxSizes = { new Vector2(1.0f, 0.7f), new Vector2(1.1f, 0.7f), new Vector2(1.2f, 0.8f) };
    [SerializeField] private int[] hitDamages = { 1, 1, 2 };

    [Header("Ranged Skill")]
    [SerializeField] private ProjectileData equippedProjectile;

    // runtime
    private int _comboIndex = 0; // 0..2
    private float _lastAttackTime = -999f;
    private float _nextMeleeAllowedTime = 0f;
    private float _nextRangedAllowedTime = 0f;

    // facing (pega do scale/vel ou do PlayerController se quiser)
    private int _facing = 1;

    private void Awake()
    {
        if (rb == null) rb = GetComponent<Rigidbody2D>();
        if (attackPoint == null)
        {
            var ap = transform.Find("AttackPoint");
            if (ap != null) attackPoint = ap;
        }
    }

    private void Update()
    {
        UpdateFacing();

        // INPUT (teclado por enquanto)
        if (Input.GetButtonDown("Fire1"))
            TryMelee();

        if (Input.GetButtonDown("Fire2"))
            TryRanged();
    }

    private void UpdateFacing()
    {
        // simples: usa velocity (se parado, mantém)
        if (rb == null) return;

        if (rb.linearVelocity.x > 0.1f) _facing = 1;
        else if (rb.linearVelocity.x < -0.1f) _facing = -1;
    }

    public void SetEquippedProjectile(ProjectileData data)
    {
        equippedProjectile = data;
    }

    public void TryMelee()
    {
        if (Time.time < _nextMeleeAllowedTime) return;

        // reset combo se demorou
        if (Time.time - _lastAttackTime > comboResetTime)
            _comboIndex = 0;

        _lastAttackTime = Time.time;
        _nextMeleeAllowedTime = Time.time + attackCooldown;

        int idx = Mathf.Clamp(_comboIndex, 0, 1);

        DoMeleeHit(idx);

        _comboIndex++;
        if (_comboIndex > 1) _comboIndex = 0;
    }

    private void DoMeleeHit(int idx)
    {
        if (attackPoint == null)
        {
            Debug.LogWarning("[PlayerCombat] AttackPoint não setado.");
            return;
        }

        Vector2 size = hitBoxSizes[idx];
        float duration = hitDurations[idx];
        int damage = hitDamages[idx];

        // offset pra frente
        Vector3 center = attackPoint.position + new Vector3(_facing * (size.x * 0.35f), 0f, 0f);

        // detecta
        var hits = Physics2D.OverlapBoxAll(center, size, 0f, hittableLayers);
        foreach (var h in hits)
        {
            if (h == null) continue;
            var dmg = h.GetComponentInParent<Damageable>();
            if (dmg != null) dmg.TakeDamage(damage);
        }

        // Debug gizmo rápido (opcional)
        StartCoroutine(DebugHitbox(center, size, duration));
    }

    private System.Collections.IEnumerator DebugHitbox(Vector3 center, Vector2 size, float duration)
    {
        float t = 0f;
        while (t < duration)
        {
            t += Time.deltaTime;
            DebugDrawRect(center, size);
            yield return null;
        }
    }

    private void DebugDrawRect(Vector3 center, Vector2 size)
    {
        // desenha no Scene view
        Vector3 a = center + new Vector3(-size.x, -size.y) * 0.5f;
        Vector3 b = center + new Vector3(-size.x, +size.y) * 0.5f;
        Vector3 c = center + new Vector3(+size.x, +size.y) * 0.5f;
        Vector3 d = center + new Vector3(+size.x, -size.y) * 0.5f;

        Debug.DrawLine(a, b, Color.red, 0f, false);
        Debug.DrawLine(b, c, Color.red, 0f, false);
        Debug.DrawLine(c, d, Color.red, 0f, false);
        Debug.DrawLine(d, a, Color.red, 0f, false);
    }

    public void TryRanged()
    {
        if (equippedProjectile == null || equippedProjectile.prefab == null) return;
        if (Time.time < _nextRangedAllowedTime) return;

        _nextRangedAllowedTime = Time.time + equippedProjectile.cooldown;

        Vector2 off = equippedProjectile.spawnOffset;
        Vector3 spawnPos = transform.position + new Vector3(off.x * _facing, off.y, 0f);

        var go = Instantiate(equippedProjectile.prefab, spawnPos, Quaternion.identity);
        var proj = go.GetComponent<Projectile>();
        if (proj != null)
            proj.Fire(_facing, equippedProjectile.speed, equippedProjectile.damage);
    }
}
