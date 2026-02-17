using UnityEngine;

public class Damageable : MonoBehaviour, IDamageable
{
    [SerializeField] private int hp = 3;

    [Header("HP Bar")]
    [SerializeField] private GameObject hpBarPrefab;

    private EnemyHPBar hpBarInstance;
    private int maxHp;


    [Header("Hit Reaction")]
    [SerializeField] private bool applyKnockback = true;
    [SerializeField] private float defaultHitStun = 0.18f;

    [SerializeField] private Rigidbody2D rb;

    [Header("Hit VFX (fallback)")]
    [Tooltip("Se DamageInfo.hitVfxPrefab vier null, usa este prefab (opcional).")]
    [SerializeField] private GameObject defaultHitVfxPrefab;

    [Tooltip("Tempo de vida da VFX instanciada (se não for auto-destruída).")]
    [SerializeField] private float hitVfxLifetime = 1.2f;
    [Header("Floating Damage")]
    [SerializeField] private GameObject floatingDamagePrefab;


    private IStunnable stunnable; // cache

    private void Awake()
    {
        if (rb == null) rb = GetComponent<Rigidbody2D>();
        maxHp = hp;

        // cache seguro pra interface (sem depender de GetComponent<IStunnable>())
        var monos = GetComponents<MonoBehaviour>();
        for (int i = 0; i < monos.Length; i++)
        {
            if (monos[i] is IStunnable s)
            {
                stunnable = s;
                break;
            }
        }
    }

    public void TakeDamage(int amount)
    {
        // Fallback simples (sem ponto/normal/vfx)
        TakeDamage(new DamageInfo(amount, Vector2.zero, 0f, null, Vector2.zero, Vector2.zero, null));
    }

    public bool TakeDamage(DamageInfo info)
    {
        int amount = Mathf.Max(0, info.damage);
        if (amount <= 0) return false;

        hp -= amount;

        // ✅ Spawn VFX (centralizado aqui)
        SpawnHitVfx(info);
        // ✅ Mostrar dano apenas se for Enemy
        if (gameObject.layer == LayerMask.NameToLayer("Enemy"))
        {
            if (floatingDamagePrefab != null)
            {
                Vector2 pos = info.hitPoint != Vector2.zero
                    ? info.hitPoint
                    : (Vector2)transform.position;

                var dmgText = Instantiate(floatingDamagePrefab, pos, Quaternion.identity);
                dmgText.GetComponentInChildren<FloatingDamageText>()?.SetValue(amount);
            }

            if (hpBarInstance == null && hpBarPrefab != null)
            {
                var bar = Instantiate(hpBarPrefab, transform.position + Vector3.up * 1.2f, Quaternion.identity);
                bar.transform.SetParent(transform);
                hpBarInstance = bar.GetComponent<EnemyHPBar>();
            }

            if (hpBarInstance != null)
            {
                hpBarInstance.SetFill((float)hp / maxHp);
            }
        }


        // ✅ Knockback consistente
        if (applyKnockback && rb != null && info.knockbackForce > 0f)
        {
            Vector2 dir = info.hitDirection;
            if (dir.sqrMagnitude < 0.0001f) dir = Vector2.up;
            dir.Normalize();

#if UNITY_6000_0_OR_NEWER
            rb.linearVelocity = dir * info.knockbackForce;
#else
            rb.velocity = dir * info.knockbackForce;
#endif
        }

        // ✅ Stun/Interrupt
        if (stunnable != null)
            stunnable.Stun(defaultHitStun);

        if (hp <= 0)
            Destroy(gameObject);

        return true;
    }

    private void SpawnHitVfx(DamageInfo info)
    {
        GameObject prefab = info.hitVfxPrefab != null ? info.hitVfxPrefab : defaultHitVfxPrefab;
        if (prefab == null) return;

        // ponto de impacto (fallback: centro do alvo)
        Vector2 p = info.hitPoint != Vector2.zero ? info.hitPoint : (Vector2)transform.position;

        // normal (fallback: -direção do hit / ou para cima)
        Vector2 n = info.hitNormal;
        if (n.sqrMagnitude < 0.0001f)
        {
            n = (info.hitDirection.sqrMagnitude > 0.0001f) ? -info.hitDirection : Vector2.up;
        }
        n.Normalize();

        float angle = Mathf.Atan2(n.y, n.x) * Mathf.Rad2Deg;
        var vfx = Instantiate(prefab, p, Quaternion.Euler(0f, 0f, angle));

        // Se seu prefab já se auto-destroi (ParticleSystem StopAction), isso não atrapalha.
        Destroy(vfx, hitVfxLifetime);
    }
}
