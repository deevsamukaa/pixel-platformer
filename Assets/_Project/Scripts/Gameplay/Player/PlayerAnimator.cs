using UnityEngine;

public class PlayerAnimator : MonoBehaviour
{
    [Header("Refs")]
    [SerializeField] private Animator animator;
    [SerializeField] private Rigidbody2D rb;
    [SerializeField] private PlayerController controller;
    [SerializeField] private PlayerCombat combat;

    [Header("Tuning")]
    [SerializeField] private float speedDeadzone = 0.05f;
    [SerializeField] private float vSpeedDeadzone = 0.05f;

    // hashes (mais rápido e evita typo)
    private static readonly int H_Speed = Animator.StringToHash("Speed");
    private static readonly int H_VSpeed = Animator.StringToHash("VSpeed");
    private static readonly int H_Grounded = Animator.StringToHash("Grounded");
    private static readonly int H_Hanging = Animator.StringToHash("Hanging");
    private static readonly int H_Climbing = Animator.StringToHash("Climbing");
    private static readonly int H_AttackIndex = Animator.StringToHash("AttackIndex");
    private static readonly int H_Attack = Animator.StringToHash("Attack");
    private static readonly int H_Hurt = Animator.StringToHash("Hurt");
    private static readonly int H_Death = Animator.StringToHash("Death");

    private void Awake()
    {
        if (animator == null) animator = GetComponentInChildren<Animator>(true);
        if (rb == null) rb = GetComponentInParent<Rigidbody2D>();
        if (controller == null) controller = GetComponentInParent<PlayerController>();
        if (combat == null) combat = GetComponentInParent<PlayerCombat>();
    }

    private void Update()
    {
        if (animator == null || rb == null || controller == null) return;

        float rawSpeed = Mathf.Abs(rb.linearVelocity.x);

        float normalizedSpeed = 0f;

        if (controller.MoveSpeed > 0.01f)
            normalizedSpeed = rawSpeed / controller.MoveSpeed;

        normalizedSpeed = Mathf.Clamp01(normalizedSpeed);

        // opcional: suavização leve
        float current = animator.GetFloat(H_Speed);
        float smooth = Mathf.Lerp(current, normalizedSpeed, 15f * Time.deltaTime);
        animator.SetFloat(H_Speed, smooth);

        float vSpeed = rb.linearVelocity.y;
        if (Mathf.Abs(vSpeed) < 0.05f) vSpeed = 0f;
        animator.SetFloat(H_VSpeed, vSpeed);

        animator.SetBool(H_Grounded, controller.IsGrounded);
        animator.SetBool(H_Hanging, controller.IsLedgeHanging);
        animator.SetBool(H_Climbing, controller.IsClimbing);
    }

    // Chamados por outros scripts (combat/damage)
    public void PlayAttack(int attackIndex)
    {
        if (animator == null) return;
        animator.SetInteger(H_AttackIndex, attackIndex);
        animator.SetTrigger(H_Attack);
    }

    public void PlayHurt()
    {
        if (animator == null) return;
        animator.SetTrigger(H_Hurt);
    }

    public void PlayDeath()
    {
        if (animator == null) return;
        animator.SetTrigger(H_Death);
    }
}
