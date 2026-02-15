using System.Collections;
using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
public class PlayerHealth : MonoBehaviour, IDamageable
{
    [Header("HP")]
    [SerializeField] private int maxHP = 5;
    [SerializeField] private int currentHP;

    [Header("I-Frames")]
    [SerializeField] private float iFrameDuration = 0.8f;
    private bool invulnerable;
    private Coroutine iFrameRoutine;

    [Header("Knockback")]
    [SerializeField] private bool resetVelocityBeforeKnockback = true;

    [Header("Hurt Lock (conecta no PlayerController)")]
    [SerializeField] private float hurtLockDuration = 0.15f;

    [Header("Feedback Visual")]
    [SerializeField] private SpriteRenderer sprite;
    [SerializeField] private float flashInterval = 0.07f;

    private Rigidbody2D rb;
    private PlayerController controller;

    public int CurrentHP => currentHP;
    public int MaxHP => maxHP;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        controller = GetComponent<PlayerController>();
        if (sprite == null) sprite = GetComponentInChildren<SpriteRenderer>();

        currentHP = maxHP;
    }

    public bool TakeDamage(DamageInfo info)
    {
        // se o controller está morto, não toma mais dano
        // (seu PlayerController já controla isso)
        if (invulnerable) return false;

        int dmg = Mathf.Max(0, info.damage);
        if (dmg <= 0) return false;

        currentHP = Mathf.Max(0, currentHP - dmg);

        ApplyKnockback(info);

        // trava input/ledge/jump por um instante
        if (controller != null)
            controller.SetHurtLock(hurtLockDuration);

        // i-frames + flash
        if (iFrameRoutine != null) StopCoroutine(iFrameRoutine);
        iFrameRoutine = StartCoroutine(IFrames());

        // morte (usa teu fluxo atual)
        if (currentHP <= 0)
        {
            if (controller != null) controller.Die();
            else
            {
#if UNITY_6000_0_OR_NEWER
                rb.linearVelocity = Vector2.zero;
#else
                rb.velocity = Vector2.zero;
#endif
                rb.simulated = false;
            }
        }

        return true;
    }

    private void ApplyKnockback(DamageInfo info)
    {
        if (info.knockbackForce <= 0f) return;

        Vector2 dir = info.hitDirection;
        if (dir.sqrMagnitude < 0.0001f) dir = Vector2.up;
        dir.Normalize();

        if (resetVelocityBeforeKnockback)
        {
#if UNITY_6000_0_OR_NEWER
            rb.linearVelocity = Vector2.zero;
#else
            rb.velocity = Vector2.zero;
#endif
        }

#if UNITY_6000_0_OR_NEWER
        rb.linearVelocity = dir * info.knockbackForce;
#else
        rb.velocity = dir * info.knockbackForce;
#endif
    }

    private IEnumerator IFrames()
    {
        invulnerable = true;

        float t = 0f;
        bool visible = true;

        while (t < iFrameDuration)
        {
            t += flashInterval;
            visible = !visible;
            if (sprite != null) sprite.enabled = visible;
            yield return new WaitForSeconds(flashInterval);
        }

        if (sprite != null) sprite.enabled = true;
        invulnerable = false;
    }

    // chama isso no Respawn, ou quando quiser resetar run
    public void ResetFull()
    {
        currentHP = maxHP;
        invulnerable = false;

        if (iFrameRoutine != null) StopCoroutine(iFrameRoutine);
        if (sprite != null) sprite.enabled = true;
    }
}
