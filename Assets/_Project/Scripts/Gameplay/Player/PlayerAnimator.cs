using UnityEngine;

public class PlayerAnimator : MonoBehaviour
{
    [Header("Refs")]
    [SerializeField] private Animator animator;
    [SerializeField] private Rigidbody2D rb;
    [SerializeField] private PlayerController controller;
    [SerializeField] private PlayerCombat combat;

    private static readonly int H_Speed = Animator.StringToHash("Speed");
    private static readonly int H_VSpeed = Animator.StringToHash("VSpeed");
    private static readonly int H_Grounded = Animator.StringToHash("Grounded");
    private static readonly int H_Hanging = Animator.StringToHash("Hanging");
    private static readonly int H_Climbing = Animator.StringToHash("Climbing");
    private static readonly int H_AttackIndex = Animator.StringToHash("AttackIndex");
    private static readonly int H_Attack = Animator.StringToHash("Attack");
    private static readonly int H_Attacking = Animator.StringToHash("Attacking");
    private static readonly int H_Dash = Animator.StringToHash("Dash");
    private static readonly int H_Dashing = Animator.StringToHash("Dashing");


    private void Awake()
    {
        if (animator == null) animator = GetComponentInChildren<Animator>(true);
        if (rb == null) rb = GetComponentInParent<Rigidbody2D>();
        if (controller == null) controller = GetComponentInParent<PlayerController>();
        if (combat == null) combat = GetComponentInParent<PlayerCombat>();
    }

    private void Update()
    {
        if (!animator) return;

        float normalizedSpeed = Mathf.Clamp01(Mathf.Abs(rb.linearVelocity.x) / controller.MoveSpeed);
        animator.SetFloat(H_Speed, normalizedSpeed);
        animator.SetFloat(H_VSpeed, rb.linearVelocity.y);
        animator.SetBool(H_Grounded, controller.IsGrounded);
        animator.SetBool(H_Hanging, controller.IsLedgeHanging);
        animator.SetBool(H_Climbing, controller.IsClimbing);

        animator.SetBool(H_Attacking, combat != null && combat.IsAttacking);

        if (controller != null)
            animator.SetBool(H_Dashing, controller.IsDashing);

    }

    // ====== Ataque ======
    public void PlayAttack(int index)
    {
        animator.SetInteger(H_AttackIndex, index);
        animator.SetTrigger(H_Attack);
    }

    // ====== Ledge ======
    public void TriggerEdgeGrab()
    {
        animator.SetTrigger("EdgeGrab");
    }

    public void SetHanging(bool value)
    {
        animator.SetBool(H_Hanging, value);
    }

    public void SetClimbing(bool value)
    {
        animator.SetBool(H_Climbing, value);
    }

    public void TriggerDash()
    {
        animator.SetTrigger(H_Dash);
    }
}
