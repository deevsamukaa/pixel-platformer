using UnityEngine;

public class PlayerCombat : MonoBehaviour
{
    [Header("Refs")]
    [SerializeField] private Rigidbody2D rb;
    [SerializeField] private PlayerAnimator playerAnimator;

    [Header("Facing (para o lunge)")]
    [SerializeField] private SpriteRenderer visualSprite; // o do Visual

    [Header("Combo")]
    [SerializeField] private float comboResetTime = 0.7f;
    [SerializeField] private int maxBufferedInputs = 2;

    [Header("Failsafe")]
    [SerializeField] private float attackFailSafeTime = 1.3f;
    [SerializeField] private bool debugLogs = false;

    [Header("Lunge (Impulse)")]
    [SerializeField] private float lungeAttack1 = 2.0f;
    [SerializeField] private float lungeAttack3 = 3.5f;

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
    }

    private void Update()
    {
        if (Input.GetButtonDown("Fire1"))
            TryMelee();

        if (!_isAttacking && Time.time - _lastAttackInputTime > comboResetTime)
        {
            _currentAttackIndex = 0;
            _bufferedInputs = 0;
        }

        // failsafe anti-trava
        if (_isAttacking && Time.time > _attackExpireAt)
        {
            if (debugLogs) Debug.LogWarning($"[Combat] FAILSAFE release idx={_currentAttackIndex}");
            ForceEndAttack();
        }
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
    // Animation Events
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

    // ✅ CHAME ESTE EVENTO NO CLIP (Attack1 e Attack3)
    public void AnimEvent_Lunge()
    {
        if (rb == null) return;

        int facing = 1;
        if (visualSprite != null)
            facing = visualSprite.flipX ? -1 : 1;
        else
            facing = transform.localScale.x < 0 ? -1 : 1;

        float impulse = 0f;
        if (_currentAttackIndex == 1) impulse = lungeAttack1;
        else if (_currentAttackIndex == 3) impulse = lungeAttack3;

        if (impulse <= 0f) return;

        // aplica impulso horizontal e zera qualquer drift pra não "patinar"
        rb.linearVelocity = new Vector2(0f, rb.linearVelocity.y);
        rb.AddForce(Vector2.right * facing * impulse, ForceMode2D.Impulse);
    }

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
}
