using System.Collections;
using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
public class PlayerController : MonoBehaviour
{
    [Header("Visual / Flip")]
    [SerializeField] private Transform visualRoot;
    [SerializeField] private SpriteRenderer visualSprite;

    [Header("Movement")]
    [SerializeField] private float moveSpeed = 6f;

    // Input (mobile + teclado fallback)
    private float moveInput;
    private float externalMoveInput = 0f;

    // Botão de pulo (mobile)
    private bool externalJumpDown = false;
    private bool externalJumpHeld = false;
    private bool externalJumpUp = false;

    [Header("Jump - Base")]
    [SerializeField] private float jumpForce = 12f;

    [Tooltip("Tempo (em segundos) que ainda permite pular após sair do chão.")]
    [SerializeField] private float coyoteTime = 0.12f;

    [Tooltip("Tempo (em segundos) que o pulo fica 'guardado' antes de tocar no chão.")]
    [SerializeField] private float jumpBufferTime = 0.12f;

    [Header("Jump - Variable Height (Hollow Knight-like)")]
    [Tooltip("Tempo máximo em que segurar o botão ainda aumenta a altura do pulo.")]
    [SerializeField] private float jumpHoldTime = 0.16f;

    [Tooltip("Aceleração extra para 'sustentar' o pulo enquanto segura (somado à física).")]
    [SerializeField] private float jumpHoldAcceleration = 35f;

    [Tooltip("Se soltar o botão durante a subida, reduz a velocidade vertical multiplicando por este fator (0.3~0.6 costuma ficar bom).")]
    [Range(0.05f, 0.95f)]
    [SerializeField] private float jumpCutMultiplier = 0.45f;

    [Tooltip("Velocidade máxima de subida permitida durante o hold (evita foguete em rampas/forças).")]
    [SerializeField] private float maxJumpUpVelocity = 16f;

    [Header("Ground Check")]
    [SerializeField] private Transform groundCheck;
    [SerializeField] private float groundCheckRadius = 0.15f;
    [SerializeField] private LayerMask groundLayer;

    [Header("Death / Respawn")]
    [SerializeField] private float respawnDelay = 0.1f;
    public Vector3 spawnPosition;
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
    private bool isGroundedFixed;

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

    // Jump variable runtime
    private bool isJumping;
    private float jumpHoldCounter;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        originalGravity = rb.gravityScale;

        if (visualRoot == null)
            visualRoot = transform.Find("Visual");

        if (visualSprite == null && visualRoot != null)
            visualSprite = visualRoot.GetComponentInChildren<SpriteRenderer>();
    }

    private void Start()
    {
        GameObject spawn = GameObject.FindGameObjectWithTag("SpawnPoint");
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
                TryStartJump();      // usa buffer+coyote e seta isJumping
                ApplyJumpCut();      // corta quando solta
            }
            else
            {
                // pendurado: subir com jump (ou auto)
                if (IsJumpDown())
                    StartClimb();
            }
        }

        // consome flags externas (mobile) - Down/Up são "1 frame"
        externalJumpDown = false;
        externalJumpUp = false;
    }

    private void FixedUpdate()
    {
        if (isDead) return;
        if (isClimbing) return;

        // ✅ Ground check sincronizado com a física
        isGroundedFixed = Physics2D.OverlapCircle(
            groundCheck.position,
            groundCheckRadius,
            groundLayer
        );

        // Se encostou no chão, corta qualquer hold residual
        if (isGroundedFixed)
        {
            isJumping = false;
            jumpHoldCounter = 0f;
        }

        if (isLedgeHanging)
        {
            rb.linearVelocity = Vector2.zero;
            return;
        }

        Move();
        ApplyJumpHold();
    }

    // -----------------------
    // Public API para MobileInputUI
    // -----------------------
    public void SetMoveInput(float value)
    {
        externalMoveInput = Mathf.Clamp(value, -1f, 1f);
    }

    // Down (início)
    public void JumpPressed()
    {
        externalJumpDown = true;
        externalJumpHeld = true;
    }

    // Up (soltou)
    public void JumpReleased()
    {
        externalJumpUp = true;
        externalJumpHeld = false;
    }

    // (opcional) caso você queira setar held direto por UI
    public void SetJumpHeld(bool held)
    {
        externalJumpHeld = held;
    }

    // -----------------------
    // Input / Movement
    // -----------------------
    private void HandleInput()
    {
        float keyboard = Input.GetAxisRaw("Horizontal");

        // Mobile tem prioridade se estiver ativo
        moveInput = (Mathf.Abs(externalMoveInput) > 0.01f) ? externalMoveInput : keyboard;

        // facing (não muda enquanto pendurado/subindo)
        if (!isLedgeHanging && !isClimbing)
        {
            if (moveInput > 0.01f) facing = 1;
            else if (moveInput < -0.01f) facing = -1;
        }

        // buffer do pulo
        if (IsJumpDown())
            jumpBufferCounter = jumpBufferTime;

        if (visualSprite != null)
            visualSprite.flipX = (facing == -1);
    }

    private bool IsJumpDown()
    {
        return Input.GetButtonDown("Jump") || externalJumpDown;
    }

    private bool IsJumpHeld()
    {
        return Input.GetButton("Jump") || externalJumpHeld;
    }

    private bool IsJumpUp()
    {
        return Input.GetButtonUp("Jump") || externalJumpUp;
    }

    private void Move()
    {
        rb.linearVelocity = new Vector2(moveInput * moveSpeed, rb.linearVelocity.y);
    }

    private void Jump()
    {
        rb.linearVelocity = new Vector2(rb.linearVelocity.x, jumpForce);
        isJumping = true;
        jumpHoldCounter = jumpHoldTime;
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

        // reset do estado de pulo ao tocar no chão (para o hold não ficar ativo)
        if (isGrounded)
            isJumping = false;
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

    private void TryStartJump()
    {
        if (jumpBufferCounter > 0f && coyoteTimeCounter > 0f)
        {
            Jump();
            jumpBufferCounter = 0f;
            coyoteTimeCounter = 0f;
        }
    }

    // -----------------------
    // Jump Variable (hold) + Jump Cut (soltar)
    // -----------------------
    private void ApplyJumpHold()
    {
        if (!isJumping) return;

        // ✅ se está no chão (pela checagem do Fixed), nunca aplica hold
        if (isGroundedFixed)
        {
            isJumping = false;
            jumpHoldCounter = 0f;
            return;
        }

        if (!IsJumpHeld()) return;

        if (rb.linearVelocity.y <= 0f)
        {
            isJumping = false;
            return;
        }

        if (jumpHoldCounter <= 0f)
        {
            isJumping = false;
            return;
        }

        rb.AddForce(Vector2.up * jumpHoldAcceleration, ForceMode2D.Force);

        if (rb.linearVelocity.y > maxJumpUpVelocity)
            rb.linearVelocity = new Vector2(rb.linearVelocity.x, maxJumpUpVelocity);

        jumpHoldCounter -= Time.fixedDeltaTime;
    }

    private void ApplyJumpCut()
    {
        // soltar durante a subida corta a velocidade
        if (!IsJumpUp()) return;

        if (rb.linearVelocity.y > 0.01f)
        {
            rb.linearVelocity = new Vector2(rb.linearVelocity.x, rb.linearVelocity.y * jumpCutMultiplier);
        }

        // encerra hold de vez ao soltar
        isJumping = false;
        jumpHoldCounter = 0f;
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
        Vector2 cornerProbe = new Vector2(
            wallHit.point.x + (ledgeCornerInset * facing),
            ledgeCheck.position.y
        );

        RaycastHit2D topHit = Physics2D.Raycast(cornerProbe, Vector2.down, topCheckDownDistance, groundLayer);
        if (!topHit) return;

        wallHitCached = wallHit;

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

        // zera pulo/hold
        isJumping = false;
        jumpHoldCounter = 0f;

        rb.linearVelocity = Vector2.zero;
        rb.gravityScale = 0f;

        Vector3 p = transform.position;
        p.x = wallHitCached.point.x - (0.05f * facing);
        transform.position = p;

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

        isJumping = false;
        jumpHoldCounter = 0f;

        rb.gravityScale = originalGravity;
        ledgeCooldownTimer = ledgeRegrabCooldown;
    }

    // -----------------------
    // Death / Respawn
    // -----------------------
    public void Die()
    {
        if (isDead) return;

        isDead = true;

        rb.linearVelocity = Vector2.zero;
        rb.simulated = false;

        isLedgeHanging = false;
        isClimbing = false;
        rb.gravityScale = originalGravity;

        // limpa pulo variável
        isJumping = false;
        jumpHoldCounter = 0f;

        CancelInvoke(nameof(Respawn));
        StopAllCoroutines();

        Invoke(nameof(Respawn), respawnDelay);
    }

    private void Respawn()
    {
        transform.position = spawnPosition;

        rb.simulated = true;
        rb.gravityScale = originalGravity;

        externalMoveInput = 0f;
        externalJumpDown = false;
        externalJumpUp = false;
        externalJumpHeld = false;

        moveInput = 0f;
        coyoteTimeCounter = 0f;
        jumpBufferCounter = 0f;

        isJumping = false;
        jumpHoldCounter = 0f;

        isDead = false;
    }

    private void OnDrawGizmosSelected()
    {
        if (groundCheck != null)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(groundCheck.position, groundCheckRadius);
        }

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

    public void SetSpawnPosition(Vector3 newSpawn)
    {
        spawnPosition = newSpawn;
    }
}
