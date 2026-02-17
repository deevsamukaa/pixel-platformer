using UnityEngine;

[RequireComponent(typeof(Collider2D))]
public class DamageOnContact2D : MonoBehaviour
{
    [Header("Dano")]
    [SerializeField] private int damage = 1;
    [SerializeField] private LayerMask targetLayers;

    [Header("Knockback")]
    [SerializeField] private float knockbackForce = 11f;

    [Tooltip("Se true, calcula direção pelo vetor (alvo - hazard). Se false, usa direção fixa.")]
    [SerializeField] private bool useRelativeDirection = true;

    [Tooltip("Empurra um pouco pra cima pra ficar gostoso e evitar 'raspar no chão'.")]
    [SerializeField] private float knockUpBias = 0.35f;

    [Header("Contato")]
    [Tooltip("Se true, aplica dano apenas no Enter. Se false, aplica também no Stay com cooldown.")]
    [SerializeField] private bool onlyOnEnter = true;

    [Tooltip("Cooldown entre danos quando onlyOnEnter = false (útil pra lava).")]
    [SerializeField] private float damageCooldown = 0.35f;

    [Header("Hit VFX (enviado via DamageInfo; spawn ocorre no Damageable)")]
    [SerializeField] private GameObject hitVfxPrefab;

    private float _nextDamageTime;

    private void Reset()
    {
        var c = GetComponent<Collider2D>();
        if (c != null) c.isTrigger = true;
    }

    private void OnTriggerEnter2D(Collider2D other) => TryDamage(other, enter: true);
    private void OnTriggerStay2D(Collider2D other) => TryDamage(other, enter: false);

    private void OnCollisionEnter2D(Collision2D collision) => TryDamage(collision.collider, enter: true);
    private void OnCollisionStay2D(Collision2D collision) => TryDamage(collision.collider, enter: false);

    private void TryDamage(Collider2D other, bool enter)
    {
        if (onlyOnEnter && !enter) return;
        if (!onlyOnEnter && Time.time < _nextDamageTime) return;

        if (((1 << other.gameObject.layer) & targetLayers) == 0) return;

        var damageable = other.GetComponentInParent<IDamageable>();
        if (damageable == null) return;

        Vector2 hazardPos = transform.position;

        // ponto real de impacto no collider do alvo
        Vector2 hitPoint = other.ClosestPoint(hazardPos);

        // normal aproximada (hazard -> ponto)
        Vector2 hitNormal = (hitPoint - hazardPos);
        if (hitNormal.sqrMagnitude < 0.0001f) hitNormal = Vector2.up;
        hitNormal.Normalize();

        // direção do knockback
        Vector2 dir;
        if (useRelativeDirection)
        {
            dir = (hitPoint - hazardPos);
            dir.y = Mathf.Max(dir.y, knockUpBias);
            if (dir.sqrMagnitude < 0.0001f) dir = Vector2.up;
        }
        else
        {
            dir = Vector2.up;
        }
        dir.Normalize();

        var info = new DamageInfo(
            damage,
            dir,
            knockbackForce,
            gameObject,
            hitPoint,
            hitNormal,
            hitVfxPrefab
        );

        bool applied = damageable.TakeDamage(info);

        // cooldown (só quando aplicou, pra respeitar invulnerabilidade)
        if (applied && !onlyOnEnter)
            _nextDamageTime = Time.time + Mathf.Max(0.01f, damageCooldown);
    }
}
