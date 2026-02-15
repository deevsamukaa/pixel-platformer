using UnityEngine;

[RequireComponent(typeof(Collider2D))]
public class DamageOnContact2D : MonoBehaviour
{
    [Header("Dano")]
    [SerializeField] private int damage = 1;
    [SerializeField] private LayerMask targetLayers;

    [Header("Knockback")]
    [SerializeField] private float knockbackForce = 11f;

    [Tooltip("Se true, calcula direção pelo vetor (alvo - hazard). Se false, usa facing fixo.")]
    [SerializeField] private bool useRelativeDirection = true;

    [Tooltip("Empurra um pouco pra cima pra ficar gostoso e evitar 'raspar no chão'.")]
    [SerializeField] private float knockUpBias = 0.35f;

    [Header("Contato")]
    [Tooltip("Se true, aplica dano apenas no Enter. Se false, aplica também no Stay com cooldown.")]
    [SerializeField] private bool onlyOnEnter = true;

    [Tooltip("Cooldown entre danos quando onlyOnEnter = false (útil pra lava).")]
    [SerializeField] private float damageCooldown = 0.35f;

    private float _nextDamageTime;

    private void Reset()
    {
        // Sugestão padrão: spikes costumam ser Trigger
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

        Vector2 dir = Vector2.up;

        if (useRelativeDirection)
        {
            // do hazard -> alvo
            dir = (other.transform.position - transform.position);

            // garante bias pra cima
            dir.y = Mathf.Max(dir.y, knockUpBias);
        }
        else
        {
            // direção fixa caso você queira usar hazard "empurra pra direita", etc
            dir = new Vector2(0f, 1f);
        }

        var info = new DamageInfo(damage, dir.normalized, knockbackForce, gameObject);

        bool applied = damageable.TakeDamage(info);

        // Se aplicou, configura cooldown (pra lava / contato contínuo)
        if (applied && !onlyOnEnter)
            _nextDamageTime = Time.time + Mathf.Max(0.01f, damageCooldown);
    }
}
