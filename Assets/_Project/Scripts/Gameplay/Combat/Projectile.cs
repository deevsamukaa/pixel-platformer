using UnityEngine;

[RequireComponent(typeof(Rigidbody2D), typeof(Collider2D))]
public class Projectile : MonoBehaviour
{
    [SerializeField] private float lifeTime = 3f;
    [SerializeField] private LayerMask hittableLayers;

    private Rigidbody2D _rb;
    private int _damage;

    private void Awake()
    {
        _rb = GetComponent<Rigidbody2D>();
    }

    public void Fire(int facing, float speed, int damage)
    {
        _damage = damage;

        if (_rb != null)
        {
            _rb.linearVelocity = new Vector2(facing * speed, 0f);
        }

        Destroy(gameObject, lifeTime);
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        // se colidir com algo "hittable", aplica dano e destrói
        if (((1 << other.gameObject.layer) & hittableLayers) == 0) return;

        var dmg = other.GetComponentInParent<Damageable>();
        if (dmg != null) dmg.TakeDamage(_damage);

        Destroy(gameObject);
    }
}
