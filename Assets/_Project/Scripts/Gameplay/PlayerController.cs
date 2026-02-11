using System.Collections;
using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
public class PlayerController : MonoBehaviour
{
    [Header("Movement")]
    [SerializeField] private float moveSpeed = 6f;

    // Input (mobile + teclado fallback)
    private float moveInput;
    private float externalMoveInput = 0f;
    private bool externalJumpPressed = false;

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

    [Header("Death / Respawn")]
    [SerializeField] private float respawnDelay = 0.1f;
    private Vector3 spawnPosition;
    private bool isDead;

    [Header("Ledge Climb (Hollow Knight-like)")]
    [SerializeField] private bool enableLedgeClimb = true;
    [SerializeField] private Transform wallCheck;   // peito
    [SerializeField] private Transform ledgeCheck;  // acima da cabeça
    [SerializeField] private float wallCheckDistance = 0.20f;
    [SerializeField] private float ledgeCheckDistance = 0.20f;

    [Tooltip("Quanto tempo o player fica pendurado antes de subir automaticamente (0 = não sobe sozinho).")]
    [SerializeField] private float autoClimbDelay = 0.0f;

    [Tooltip("Duração do movimento de subir (teleporte suave).")]
    [SerializeField] private float climbDuration = 0.12f;

    [Tooltip("Offset do ponto final do climb a partir do ponto detectado na parede. Ajuste fino conforme seu sprite/colisor.")]
    [SerializeField] private Vector2 climbEndOffset = new Vector2(0.35f, 1.10f);

    [Tooltip("Evita regrudar na quina imediatamente após subir.")]
    [SerializeField] private float ledgeRegrabCooldown = 0.15f;
    [SerializeField] private float topCheckDownDistance = 1.5f;   // quanto desce procurando "chão"
    [SerializeField] private float ledgeCornerInset = 0.05f;

    private Rigidbody2D rb;

    // Estado chão/pulo
    private bool isGrounded;
    private float coyoteTimeCounter;
    private float jumpBufferCounter;

    // Facing
    private int facing = 1; // 1 = direita, -1 = esquerda

    // Ledge
    private bool isLedgeHanging;
    private bool isClimbing;
    private float originalGravity;
    private float ledgeCooldownTimer;
    private RaycastHit2D wallHitCached;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        originalGravity = rb.gravityScale;
    }

    private void Start()
    {
        // --- Spawn (do Script 1) ---
        GameObject spawn = GameObject.FindWithTag("SpawnPoint");
        if (spawn != null)
            spawnPosition = spawn.transform.position;
        else
            spawnPosition = transform.position;
    }

    private void Update()
    {
        if (isDead) return;

        // timers (ledge)
        if (ledgeCooldownTimer > 0f)
            ledgeCooldownTimer -= Time.deltaTime;

        HandleInput();

        CheckGround();
        UpdateJumpTimers();

        if (!isClimbing)
        {
            if (!isLedgeHanging)
            {
                TryLedgeGrab();
                TryJump();
            }
            else
            {
                // pendurado: subir com jump (ou auto)
                if (externalJumpPressed || Input.GetButtonDown("Jump"))
                    StartClimb();
            }
        }

        // consume clique externo (mobile)
        externalJumpPressed = false;
    }

    private void FixedUpdate()
    {
        if (isDead) return;
        if (isClimbing) return;

        if (isLedgeHanging)
        {
            // trava o player no lugar
            rb.linearVelocity = Vector2.zero;
            return;
        }

        Move();
    }

    // -----------------------
    // Public API para MobileInputUI
    // -----------------------
    public void SetMoveInput(float value)
    {
        externalMoveInput = Mathf.Clamp(value, -1f, 1f);
    }

    public void JumpPressed()
    {
        externalJumpPressed = true;
    }

    // -----------------------
    // Input / Movement
    // -----------------------
    private void HandleInput()
    {
        float keyboard = Input.GetAxisRaw("Horizontal");

        // Mobile tem prioridade se estiver ativo
        moveInput = (Mathf.Abs(externalMoveInput) > 0.01f) ? externalMoveInput : keyboard;

        // facing
        if (moveInput > 0.01f) facing = 1;
        else if (moveInput < -0.01f) facing = -1;

        // buffer do pulo (teclado + mobile)
        bool jumpDown = Input.GetButtonDown("Jump") || externalJumpPressed;
        if (jumpDown)
            jumpBufferCounter = jumpBufferTime;
    }

    private void Move()
    {
        rb.linearVelocity = new Vector2(moveInput * moveSpeed, rb.linearVelocity.y);
    }

    private void Jump()
    {
        rb.linearVelocity = new Vector2(rb.linearVelocity.x, jumpForce);
    }

    // -----------------------
    // Ground / Jump permissivo
    // -----------------------
    private void CheckGround()
    {
        isGrounded = Physics2D.OverlapCircle(
            groundCheck.position,
            groundCheckRadius,
            groundLayer
        );

        // se tocou o chão, não fica pendurado
        if (isGrounded && isLedgeHanging)
            ExitLedgeHang();
    }

    private void UpdateJumpTimers()
    {
        // Coyote time
        if (isGrounded)
            coyoteTimeCounter = coyoteTime;
        else
            coyoteTimeCounter -= Time.deltaTime;

        // Jump buffer
        if (jumpBufferCounter > 0f)
            jumpBufferCounter -= Time.deltaTime;
    }

    private void TryJump()
    {
        if (jumpBufferCounter > 0f && coyoteTimeCounter > 0f)
        {
            Jump();
            jumpBufferCounter = 0f;
            coyoteTimeCounter = 0f;
        }
    }

    // -----------------------
    // Ledge Grab + Climb
    // -----------------------
    private void TryLedgeGrab()
    {
        if (!enableLedgeClimb) return;
        if (ledgeCooldownTimer > 0f) return;
        if (isGrounded) return;
        if (rb.linearVelocity.y >= 0f) return; // só quando está caindo
        if (wallCheck == null || ledgeCheck == null) return;

        Vector2 dir = Vector2.right * facing;

        // 1) Parede no peito
        RaycastHit2D wallHit = Physics2D.Raycast(wallCheck.position, dir, wallCheckDistance, groundLayer);
        if (!wallHit) return;

        // 2) Espaço livre acima (não pode ter parede na altura da cabeça)
        RaycastHit2D ledgeBlocked = Physics2D.Raycast(ledgeCheck.position, dir, ledgeCheckDistance, groundLayer);
        if (ledgeBlocked) return;

        // 3) Confirma que existe "topo" logo depois da parede
        // Origem: um pouco depois da parede, na altura do ledgeCheck
        Vector2 cornerProbe = new Vector2(
            wallHit.point.x + (ledgeCornerInset * facing),
            ledgeCheck.position.y
        );

        RaycastHit2D topHit = Physics2D.Raycast(cornerProbe, Vector2.down, topCheckDownDistance, groundLayer);
        if (!topHit) return; // sem chão em cima => não é quina (evita subir no meio da parede)

        // Achou quina válida: cache wallHit e também o ponto do topo (opcional)
        wallHitCached = wallHit;

        // Se você quiser, pode usar topHit.point como referência melhor pro climb:
        // (opcional) guardar topHit.point num campo e usar pra posicionar o final do climb
        // ledgeTopCached = topHit.point;

        EnterLedgeHang();

        if (autoClimbDelay > 0f)
            StartCoroutine(AutoClimbAfterDelay(autoClimbDelay));
    }

    private IEnumerator AutoClimbAfterDelay(float delay)
    {
        float t = 0f;
        while (t < delay)
        {
            if (!isLedgeHanging) yield break;
            t += Time.deltaTime;
            yield return null;
        }

        if (isLedgeHanging)
            StartClimb();
    }

    private void EnterLedgeHang()
    {
        isLedgeHanging = true;
        rb.linearVelocity = Vector2.zero;
        rb.gravityScale = 0f;

        // Gruda numa posição estável na quina (evita tremedeira)
        Vector3 p = transform.position;
        p.x = wallHitCached.point.x - (0.05f * facing);
        transform.position = p;

        // quando gruda na quina, zera permissões antigas de pulo
        coyoteTimeCounter = 0f;
        jumpBufferCounter = 0f;
    }

    private void ExitLedgeHang()
    {
        isLedgeHanging = false;
        rb.gravityScale = originalGravity;
    }

    private void StartClimb()
    {
        if (isClimbing) return;
        if (!isLedgeHanging) return;

        StartCoroutine(ClimbRoutine());
    }

    private IEnumerator ClimbRoutine()
    {
        isClimbing = true;

        rb.linearVelocity = Vector2.zero;
        rb.gravityScale = 0f;

        // ponto final do climb baseado na parede detectada
        Vector2 end = wallHitCached.point;
        end.x += climbEndOffset.x * facing;
        end.y += climbEndOffset.y;

        Vector3 startPos = transform.position;
        Vector3 endPos = new Vector3(end.x, end.y, transform.position.z);

        float t = 0f;
        while (t < climbDuration)
        {
            t += Time.deltaTime;
            float a = Mathf.Clamp01(t / climbDuration);
            transform.position = Vector3.Lerp(startPos, endPos, a);
            yield return null;
        }

        transform.position = endPos;

        isLedgeHanging = false;
        isClimbing = false;

        rb.gravityScale = originalGravity;
        ledgeCooldownTimer = ledgeRegrabCooldown;
    }

    // -----------------------
    // Death / Respawn (do Script 1)
    // -----------------------
    public void Die()
    {
        if (isDead) return;

        isDead = true;

        rb.linearVelocity = Vector2.zero;
        rb.simulated = false;

        // limpa estados de climb
        isLedgeHanging = false;
        isClimbing = false;
        rb.gravityScale = originalGravity;

        CancelInvoke(nameof(Respawn));
        StopAllCoroutines();

        Invoke(nameof(Respawn), respawnDelay);
    }

    private void Respawn()
    {
        transform.position = spawnPosition;

        rb.simulated = true;
        rb.gravityScale = originalGravity;

        // zera inputs/timers
        externalMoveInput = 0f;
        externalJumpPressed = false;
        moveInput = 0f;

        coyoteTimeCounter = 0f;
        jumpBufferCounter = 0f;

        isDead = false;
    }

    private void OnDrawGizmosSelected()
    {
        if (groundCheck != null)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(groundCheck.position, groundCheckRadius);
        }

        // OBS: Gizmos usando 'facing' em edit mode pode não refletir seu lado real.
        if (wallCheck != null)
        {
            Gizmos.color = Color.cyan;
            Vector3 dir = Vector3.right * facing;
            Gizmos.DrawLine(wallCheck.position, wallCheck.position + dir * wallCheckDistance);
        }

        if (ledgeCheck != null)
        {
            Gizmos.color = Color.yellow;
            Vector3 dir = Vector3.right * facing;
            Gizmos.DrawLine(ledgeCheck.position, ledgeCheck.position + dir * ledgeCheckDistance);
        }
    }
}
