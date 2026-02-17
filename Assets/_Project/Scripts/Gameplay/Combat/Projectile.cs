using UnityEngine;

[RequireComponent(typeof(Rigidbody2D), typeof(Collider2D))]
public class Projectile : MonoBehaviour
{
    [SerializeField] private float lifeTime = 3f;
    [SerializeField] private LayerMask hittableLayers;

    [Header("Damage")]
    [SerializeField] private float knockbackForce = 8f;
    [SerializeField] private float knockUpBias = 0.25f;

    [Header("Hit VFX (enviado via DamageInfo; spawn ocorre no Damageable)")]
    [SerializeField] private GameObject hitVfxPrefab;

    private Rigidbody2D _rb;
    private int _damage;
    private int _facing = 1;

    private void Awake()
    {
        _rb = GetComponent<Rigidbody2D>();
    }

    public void Fire(int facing, float speed, int damage)
    {
        _facing = facing;
        _damage = damage;

        if (_rb != null)
        {
#if UNITY_6000_0_OR_NEWER
            _rb.linearVelocity = new Vector2(facing * speed, 0f);
#else
            _rb.velocity = new Vector2(facing * speed, 0f);
#endif
        }

        Destroy(gameObject, lifeTime);
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (((1 << other.gameObject.layer) & hittableLayers) == 0)
            return;

        var dmg = other.GetComponentInParent<IDamageable>();
        if (dmg != null)
        {
            // direção do knockback
            Vector2 dir = new Vector2(_facing, 0f);
            dir.y = Mathf.Max(dir.y, knockUpBias);
            dir.Normalize();

            // ponto/normal do impacto
            Vector2 from = transform.position;
            Vector2 hitPoint = other.ClosestPoint(from);

            Vector2 hitNormal = (hitPoint - from);
            if (hitNormal.sqrMagnitude < 0.0001f) hitNormal = dir;
            hitNormal.Normalize();

            var info = new DamageInfo(
                _damage,
                dir,
                knockbackForce,
                gameObject,
                hitPoint,
                hitNormal,
                hitVfxPrefab
            );

            dmg.TakeDamage(info);
        }

        Destroy(gameObject);
    }
}
