using System.Collections;
using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
public class EnemyCharger : MonoBehaviour, IStunnable
{
    [Header("Patrol")]
    [SerializeField] private float patrolSpeed = 2f;
    [SerializeField] private float patrolDistance = 3f;

    [Header("Detection")]
    [SerializeField] private float detectionRange = 4f;
    [SerializeField] private LayerMask playerLayer;

    [Header("Attack")]
    [SerializeField] private float windUpTime = 0.25f;
    [SerializeField] private float dashSpeed = 10f;
    [SerializeField] private float dashDuration = 0.35f;
    [SerializeField] private float recoveryTime = 0.4f;

    [Header("Fairness")]
    [Tooltip("Intervalo mínimo entre investidas (evita spam injusto).")]
    [SerializeField] private float minAttackInterval = 0.9f;

    [Tooltip("Quanto tempo fica travado ao receber hit (interrompe ataque).")]
    [SerializeField] private float hitStunOnDamage = 0.18f;

    private Rigidbody2D rb;
    private Vector2 startPos;
    private int facing = 1;

    private bool isAttacking;
    private bool isStunned;
    private float nextAttackTime;
    private Coroutine attackCo;

    private Transform player;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        startPos = transform.position;
    }

    private void Update()
    {
        if (isStunned) return;
        if (isAttacking) return;

        DetectPlayer();
        if (!isAttacking)
            Patrol();
    }

    private void Patrol()
    {
#if UNITY_6000_0_OR_NEWER
        rb.linearVelocity = new Vector2(facing * patrolSpeed, rb.linearVelocity.y);
#else
        rb.velocity = new Vector2(facing * patrolSpeed, rb.velocity.y);
#endif
        float dist = transform.position.x - startPos.x;
        if (Mathf.Abs(dist) >= patrolDistance)
            Flip();
    }

    private void DetectPlayer()
    {
        if (Time.time < nextAttackTime) return;

        Collider2D hit = Physics2D.OverlapCircle(transform.position, detectionRange, playerLayer);
        if (hit == null) return;

        player = hit.transform;
        attackCo = StartCoroutine(AttackRoutine());
    }

    private IEnumerator AttackRoutine()
    {
        isAttacking = true;

#if UNITY_6000_0_OR_NEWER
        rb.linearVelocity = Vector2.zero;
#else
        rb.velocity = Vector2.zero;
#endif
        facing = (player.position.x > transform.position.x) ? 1 : -1;

        // wind-up (telegraph)
        float t0 = 0f;
        while (t0 < windUpTime)
        {
            if (isStunned) { EndAttack(); yield break; }
            t0 += Time.deltaTime;
            yield return null;
        }

        // dash
        float t = 0f;
        while (t < dashDuration)
        {
            if (isStunned) { EndAttack(); yield break; }

#if UNITY_6000_0_OR_NEWER
            rb.linearVelocity = new Vector2(facing * dashSpeed, rb.linearVelocity.y);
#else
            rb.velocity = new Vector2(facing * dashSpeed, rb.velocity.y);
#endif
            t += Time.deltaTime;
            yield return null;
        }

#if UNITY_6000_0_OR_NEWER
        rb.linearVelocity = Vector2.zero;
#else
        rb.velocity = Vector2.zero;
#endif

        // recovery
        float tr = 0f;
        while (tr < recoveryTime)
        {
            if (isStunned) { EndAttack(); yield break; }
            tr += Time.deltaTime;
            yield return null;
        }

        EndAttack();
    }

    private void EndAttack()
    {
        isAttacking = false;
        nextAttackTime = Time.time + minAttackInterval;
        attackCo = null;
    }

    public void Stun(float duration)
    {
        if (duration <= 0f) return;

        // cancela ataque atual
        if (attackCo != null)
        {
            StopCoroutine(attackCo);
            attackCo = null;
        }

        isAttacking = false;
        isStunned = true;

#if UNITY_6000_0_OR_NEWER
        rb.linearVelocity = Vector2.zero;
#else
        rb.velocity = Vector2.zero;
#endif

        StopCoroutine(nameof(StunRoutine));
        StartCoroutine(StunRoutine(duration));
    }

    private IEnumerator StunRoutine(float duration)
    {
        yield return new WaitForSeconds(duration);
        isStunned = false;
        nextAttackTime = Time.time + 0.1f; // mini folga pós-stun
    }

    private void Flip() => facing *= -1;

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, detectionRange);
    }

    // ✅ Esse método é opcional: você pode chamar via Damageable (abaixo)
    public float GetDefaultHitStun() => hitStunOnDamage;
}
