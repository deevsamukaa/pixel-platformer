using UnityEngine;

[RequireComponent(typeof(Rigidbody2D), typeof(Collider2D))]
public class Projectile : MonoBehaviour
{
    [SerializeField] private float lifeTime = 3f;
    [SerializeField] private LayerMask hittableLayers;
    [SerializeField] private float knockbackForce = 8f;
    [SerializeField] private float knockUpBias = 0.25f;
    private int _facing = 1;


    private Rigidbody2D _rb;
    private int _damage;

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
        if (((1 << other.gameObject.layer) & hittableLayers) == 0) return;

        var dmg = other.GetComponentInParent<IDamageable>();
        if (dmg != null)
        {
            Vector2 dir = new Vector2(_facing, 0f);
            dir.y = Mathf.Max(dir.y, knockUpBias);

            dmg.TakeDamage(new DamageInfo(_damage, dir.normalized, knockbackForce, gameObject));
        }

        Destroy(gameObject);
    }
}
