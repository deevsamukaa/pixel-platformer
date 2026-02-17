using UnityEngine;

public class PlayerCombat : MonoBehaviour
{
    [Header("Refs")]
    [SerializeField] private Rigidbody2D rb;
    [SerializeField] private PlayerAnimator playerAnimator;

    [Header("Facing (para o lunge)")]
    [SerializeField] private SpriteRenderer visualSprite; // SpriteRenderer do Visual

    [Header("Melee Hitbox")]
    [SerializeField] private Transform attackPoint;        // child "AttackPoint"
    [SerializeField] private LayerMask hittableLayers;

    [Tooltip("Tamanho do hitbox por hit (1..3).")]
    [SerializeField]
    private Vector2[] hitBoxSizes =
    {
        new Vector2(1.00f, 0.70f),
        new Vector2(1.10f, 0.75f),
        new Vector2(1.20f, 0.80f),
    };

    [Tooltip("Centro do hitbox (offset LOCAL a partir do AttackPoint) por hit (1..3). " +
             "O X é espelhado automaticamente pelo facing.")]
    [SerializeField]
    private Vector2[] hitBoxCenterOffsets =
    {
        new Vector2(0.55f, 0.05f),  // Attack1
        new Vector2(0.60f, 0.10f),  // Attack2
        new Vector2(0.70f, 0.08f),  // Attack3
    };

    [Tooltip("Dano por hit (1..3).")]
    [SerializeField] private int[] hitDamages = { 1, 1, 2 };

    [Header("Knockback (por hit 1..3)")]
    [SerializeField] private float[] hitKnockbackForces = { 9f, 10f, 12f };

    [Tooltip("Empurra um pouco pra cima pra ficar gostoso.")]
    [SerializeField] private float knockUpBias = 0.35f;

    [Header("Combo")]
    [SerializeField] private float comboResetTime = 0.7f;
    [SerializeField] private int maxBufferedInputs = 3;

    [Header("Failsafe")]
    [SerializeField] private float attackFailSafeTime = 1.3f;

    [Header("Lunge (Impulse)")]
    [SerializeField] private float lungeAttack1 = 2.0f;
    [SerializeField] private float lungeAttack3 = 3.5f;

    [Header("Debug")]
    [SerializeField] private bool debugHitbox = false;
    [SerializeField] private float debugHitboxDuration = 0.06f;


    public bool IsAttacking => _isAttacking;

    private bool _isAttacking;
    private bool _comboWindowOpen;
    private int _currentAttackIndex;   // 1..3
    private int _bufferedInputs;
    private float _lastAttackInputTime = -999f;
    private float _attackExpireAt = -999f;

    private void Awake()
    {
        if (rb == null) rb = GetComponent<Rigidbody2D>();
        if (playerAnimator == null) playerAnimator = GetComponent<PlayerAnimator>();

        if (visualSprite == null)
        {
            var visual = transform.Find("Visual");
            if (visual != null) visualSprite = visual.GetComponentInChildren<SpriteRenderer>();
        }

        if (attackPoint == null)
        {
            var ap = transform.Find("AttackPoint");
            if (ap != null) attackPoint = ap;
        }
    }

    private void Update()
    {
        if (Input.GetButtonDown("Fire1"))
            TryMelee();

        // reset combo se ficou muito tempo sem atacar e não está atacando
        if (!_isAttacking && Time.time - _lastAttackInputTime > comboResetTime)
        {
            _currentAttackIndex = 0;
            _bufferedInputs = 0;
        }

        // failsafe anti-trava
        if (_isAttacking && Time.time > _attackExpireAt)
            ForceEndAttack();
    }

    public void TryMelee()
    {
        _lastAttackInputTime = Time.time;

        if (_isAttacking)
        {
            _bufferedInputs = Mathf.Clamp(_bufferedInputs + 1, 0, maxBufferedInputs);
            return;
        }

        StartAttack(1);
    }

    private void StartAttack(int index)
    {
        index = Mathf.Clamp(index, 1, 3);

        _isAttacking = true;
        _comboWindowOpen = false;
        _currentAttackIndex = index;

        if (_bufferedInputs > 0) _bufferedInputs--;

        _attackExpireAt = Time.time + attackFailSafeTime;

        playerAnimator?.PlayAttack(index);
    }

    // -------------------
    // Animation Events (coloque nos clips)
    // -------------------

    public void AnimEvent_ComboOpen()
    {
        if (!_isAttacking) return;
        _comboWindowOpen = true;
    }

    public void AnimEvent_ComboWindow()
    {
        if (!_isAttacking) return;
        if (!_comboWindowOpen) return;
        TryAdvanceCombo();
    }

    public void AnimEvent_AttackEnd()
    {
        if (!_isAttacking) return;

        if (_currentAttackIndex < 3 && _bufferedInputs > 0)
        {
            TryAdvanceCombo(force: true);
            return;
        }

        ForceEndAttack();
    }

    public void AnimEvent_Lunge()
    {
        if (rb == null) return;

        int facing = GetFacing();

        float impulse = 0f;
        if (_currentAttackIndex == 1) impulse = lungeAttack1;
        else if (_currentAttackIndex == 3) impulse = lungeAttack3;

        if (impulse <= 0f) return;

        rb.linearVelocity = new Vector2(0f, rb.linearVelocity.y);
        rb.AddForce(Vector2.right * facing * impulse, ForceMode2D.Impulse);
    }

    // -------------------
    // Internals
    // -------------------

    private void TryAdvanceCombo(bool force = false)
    {
        if (_currentAttackIndex >= 3) return;
        if (_bufferedInputs <= 0) return;

        int next = _currentAttackIndex + 1;
        StartAttack(next);
    }

    private void ForceEndAttack()
    {
        _isAttacking = false;
        _comboWindowOpen = false;
        _bufferedInputs = 0;
        _currentAttackIndex = 0;
        _attackExpireAt = -999f;
    }

    private int GetFacing()
    {
        if (visualSprite != null)
            return visualSprite.flipX ? -1 : 1;

        return transform.localScale.x < 0 ? -1 : 1;
    }

    private void DebugDrawRect(Vector3 center, Vector2 size, float dur)
    {
        Vector3 a = center + new Vector3(-size.x, -size.y) * 0.5f;
        Vector3 b = center + new Vector3(-size.x, +size.y) * 0.5f;
        Vector3 c = center + new Vector3(+size.x, +size.y) * 0.5f;
        Vector3 d = center + new Vector3(+size.x, -size.y) * 0.5f;

        Debug.DrawLine(a, b, Color.red, dur, false);
        Debug.DrawLine(b, c, Color.red, dur, false);
        Debug.DrawLine(c, d, Color.red, dur, false);
        Debug.DrawLine(d, a, Color.red, dur, false);
    }
}
