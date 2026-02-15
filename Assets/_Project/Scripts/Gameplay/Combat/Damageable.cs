using UnityEngine;

public class Damageable : MonoBehaviour, IDamageable
{
    [SerializeField] private int hp = 3;

    [Header("Hit Reaction")]
    [SerializeField] private bool applyKnockback = true;
    [SerializeField] private float defaultHitStun = 0.18f;

    [SerializeField] private Rigidbody2D rb;

    private IStunnable stunnable; // cache

    private void Awake()
    {
        if (rb == null) rb = GetComponent<Rigidbody2D>();

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
        TakeDamage(new DamageInfo(amount, Vector2.zero, 0f, null));
    }

    public bool TakeDamage(DamageInfo info)
    {
        int amount = Mathf.Max(0, info.damage);
        if (amount <= 0) return false;

        hp -= amount;

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
}
