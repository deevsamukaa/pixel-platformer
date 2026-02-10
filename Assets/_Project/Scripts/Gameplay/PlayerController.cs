using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
public class PlayerController : MonoBehaviour
{
    [Header("Movement")]
    [SerializeField] private float moveSpeed = 6f;

    [Header("Jump")]
    [SerializeField] private float jumpForce = 12f;

    [Tooltip("Tempo (em segundos) que ainda permite pular após sair do chão.")]
    [SerializeField] private float coyoteTime = 0.12f;

    [Tooltip("Tempo (em segundos) que o pulo fica 'guardado' antes de tocar no chão.")]
    [SerializeField] private float jumpBufferTime = 0.12f;

    [Header("Ground Check")]
    [SerializeField] private Transform groundCheck;
    [SerializeField] private float groundCheckRadius = 0.15f;
    [SerializeField] private LayerMask groundLayer;

    private Rigidbody2D rb;
    private float moveInput;

    private bool isGrounded;
    private float coyoteTimeCounter;
    private float jumpBufferCounter;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
    }

    private void Update()
    {
        HandleInput();
        CheckGround();
        UpdateJumpTimers();
        TryJump();
    }

    private void FixedUpdate()
    {
        Move();
    }

    private void HandleInput()
    {
        //TEMPORÁRIO: teclado para teste no editor
        moveInput = Input.GetAxisRaw("Horizontal");


        if (Input.GetButtonDown("Jump") && isGrounded)
        {
            jumpBufferCounter = jumpBufferTime;
        }
    }

    private void CheckGround()
    {
        isGrounded = Physics2D.OverlapCircle(
            groundCheck.position,
            groundCheckRadius,
            groundLayer
        );
    }

    private void UpdateJumpTimers()
    {
        // Coyote time: quando está no chão, reseta; quando sai, vai contando até 0
        if (isGrounded)
            coyoteTimeCounter = coyoteTime;
        else
            coyoteTimeCounter -= Time.deltaTime;

        // Jump buffer: vai contando até 0
        if (jumpBufferCounter > 0f)
            jumpBufferCounter -= Time.deltaTime;
    }

    private void TryJump()
    {
        // Se o jogador apertou pulo recentemente (buffer) e ainda está dentro do coyote time, pula
        if (jumpBufferCounter > 0f && coyoteTimeCounter > 0f)
        {
            Jump();
            jumpBufferCounter = 0f; // consome o buffer
            coyoteTimeCounter = 0f; // evita double jump “grátis”
        }
    }

    private void Move()
    {
        rb.linearVelocity = new Vector2(moveInput * moveSpeed, rb.linearVelocity.y);
    }

    private void Jump()
    {
        rb.linearVelocity = new Vector2(rb.linearVelocity.x, jumpForce);
    }

    private void OnDrawGizmosSelected()
    {
        if (groundCheck == null) return;

        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(groundCheck.position, groundCheckRadius);
    }
}
