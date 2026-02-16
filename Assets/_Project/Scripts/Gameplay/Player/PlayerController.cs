using System.Collections;
using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
public class PlayerController : MonoBehaviour
{
    [SerializeField] private PlayerAnimator playerAnimator;
    [Header("Visual / Flip")]
    [SerializeField] private Transform visualRoot;
    [SerializeField] private SpriteRenderer visualSprite;

    [Header("Movement")]
    [SerializeField] private float moveSpeed = 6f;
    public float MoveSpeed => moveSpeed;


    // Input (mobile + teclado fallback)
    private float moveInput;
    private float externalMoveInput = 0f;

    // Botão de pulo (mobile)
    private bool externalJumpDown = false;
    private bool externalJumpHeld = false;
    private bool externalJumpUp = false;

    [Header("Dash")]
    [SerializeField] private bool enableDash = true;

    [Tooltip("Velocidade do dash (horizontal).")]
    [SerializeField] private float dashSpeed = 16f;

    [Tooltip("Duração do dash (segundos).")]
    [SerializeField] private float dashDuration = 0.12f;

    [Tooltip("Cooldown entre dashes.")]
    [SerializeField] private float dashCooldown = 0.35f;

    [Tooltip("Se true, o dash não tem gravidade durante a duração.")]
    [SerializeField] private bool dashNoGravity = true;

    [Tooltip("Se true, trava o controle horizontal durante o dash.")]
    [SerializeField] private bool dashLockMovement = true;

    private bool isDashing;
    public bool IsDashing => isDashing;

    private float dashReadyAt = -999f;
    private Coroutine dashCo;


    [Header("Jump - Base")]
    [SerializeField] private float jumpForce = 12f;

    [Tooltip("Tempo (em segundos) que ainda permite pular após sair do chão.")]
    [SerializeField] private float coyoteTime = 0.12f;

    [Tooltip("Tempo (em segundos) que o pulo fica 'guardado' antes de tocar no chão.")]
    [SerializeField] private float jumpBufferTime = 0.12f;
    [Header("Jump - Double Jump")]
    [SerializeField] private bool enableDoubleJump = true;

    [Tooltip("Força do double jump. Se 0, usa jumpForce.")]
    [SerializeField] private float doubleJumpForce = 12f;

    private bool hasUsedDoubleJump;

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

    // Estado chão/pulo
    private bool isGroundedFixed;
    public bool IsGrounded => isGroundedFixed;

    [Header("Death / Respawn")]
    [SerializeField] private float respawnDelay = 0.1f;
    public Vector3 spawnPosition;
    private bool isDead;
    [Header("Hurt Lock (recebe dano)")]
    [SerializeField] private float hurtLockDuration = 0.15f;
    private bool isHurtLocked;
    [Header("Ledge - Stability")]
    [SerializeField] private float ledgeStartClimbLock = 0.35f; // bloqueia regrab durante climb
    [SerializeField] private float ledgeHangLock = 0.12f;
    private Collider2D col;
    private RaycastHit2D topHitCached;  // bloqueia re-entrada logo ao agarrar


    [Header("Ledge Climb (Hollow Knight-like)")]
    public bool IsClimbingOrHanging => isClimbing || isLedgeHanging;
    public bool IsLedgeHanging => isLedgeHanging;
    public bool IsClimbing => isClimbing;
    private Coroutine autoClimbCo;
    private Coroutine climbCo;
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
    [Header("Jump - Grounding Stability")]
    [SerializeField] private float ungroundedUpVelocity = 0.1f;
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

        if (playerAnimator == null) playerAnimator = GetComponent<PlayerAnimator>();

        col = GetComponent<Collider2D>();

    }

    private void Update()
    {
        if (isDead) return;

        if (isHurtLocked)
        {
            externalJumpDown = false;
            externalJumpUp = false;
            // Não processa input nem ledge/jump
            return;
        }

        // timers (ledge)
        if (ledgeCooldownTimer > 0f)
            ledgeCooldownTimer -= Time.deltaTime;

        HandleInput();

        // ✅ coyote/buffer agora usam isGroundedFixed (fonte única)
        UpdateJumpTimers();

        if (!isClimbing)
        {
            if (!isLedgeHanging)
            {
                TryLedgeGrab();
                TryStartJump();
                ApplyJumpCut();
            }
            else
            {
                if (IsJumpDown())
                    StartClimb();
            }
        }

        externalJumpDown = false;
        externalJumpUp = false;
        externalDashDown = false;
    }

    private void FixedUpdate()
    {
        if (isDead) return;
        if (isClimbing) return;
        if (isDashing) return;

        if (isHurtLocked)
        {
            // trava movimento horizontal durante o hit
            rb.linearVelocity = new Vector2(0f, rb.linearVelocity.y);
            return;
        }

        // ✅ Ground check sincronizado com a física (única fonte)
        isGroundedFixed = Physics2D.OverlapCircle(
            groundCheck.position,
            groundCheckRadius,
            groundLayer
        );


        // Só corta o hold se estiver no chão e NÃO estiver na janela pós-pulo
        if (ShouldTreatAsGrounded())
        {
            isJumping = false;
            jumpHoldCounter = 0f;
            hasUsedDoubleJump = false;
        }

        if (isLedgeHanging)
        {
            rb.linearVelocity = Vector2.zero;
            return;
        }

        var combat = GetComponent<PlayerCombat>();
        if (combat != null && combat.IsAttacking && isGroundedFixed && !isLedgeHanging && !isClimbing)
        {
            ApplyJumpHold();
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

    private bool externalDashDown;

    public void DashPressed()
    {
        externalDashDown = true;
    }

    private bool IsDashDown() => Input.GetButtonDown("Fire3") || Input.GetKeyDown(KeyCode.LeftShift) || externalDashDown;

    public void JumpPressed()
    {
        externalJumpDown = true;
        externalJumpHeld = true;
    }

    public void JumpReleased()
    {
        externalJumpUp = true;
        externalJumpHeld = false;
    }

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
        moveInput = (Mathf.Abs(externalMoveInput) > 0.01f) ? externalMoveInput : keyboard;

        if (!isLedgeHanging && !isClimbing)
        {
            if (moveInput > 0.01f) facing = 1;
            else if (moveInput < -0.01f) facing = -1;
        }

        if (IsJumpDown())
            jumpBufferCounter = jumpBufferTime;

        if (visualSprite != null)
            visualSprite.flipX = (facing == -1);

        TryDash();
    }

    private bool IsJumpDown() => Input.GetButtonDown("Jump") || externalJumpDown;
    private bool IsJumpHeld() => Input.GetButton("Jump") || externalJumpHeld;
    private bool IsJumpUp() => Input.GetButtonUp("Jump") || externalJumpUp;

    private void Move()
    {
        rb.linearVelocity = new Vector2(moveInput * moveSpeed, rb.linearVelocity.y);
    }

    private void Jump()
    {
        JumpWithForce(jumpForce);
    }

    private void JumpWithForce(float force)
    {
        rb.linearVelocity = new Vector2(rb.linearVelocity.x, force);
        isJumping = true;
        jumpHoldCounter = jumpHoldTime;

        // Se você quiser futuramente plugar animação:
        // playerAnimator?.TriggerJump(); ou TriggerDoubleJump();
    }


    // -----------------------
    // Jump permissivo (coyote/buffer)
    // -----------------------
    private void UpdateJumpTimers()
    {
        // ✅ Fonte única: isGroundedFixed
        if (isGroundedFixed)
            coyoteTimeCounter = coyoteTime;
        else
            coyoteTimeCounter -= Time.deltaTime;

        if (jumpBufferCounter > 0f)
            jumpBufferCounter -= Time.deltaTime;
    }

    private void TryStartJump()
    {
        if (jumpBufferCounter <= 0f) return;

        // 1) Pulo normal (chão/coyote)
        if (coyoteTimeCounter > 0f)
        {
            JumpWithForce(jumpForce);

            // ✅ ao usar pulo do chão, libera double jump de novo
            hasUsedDoubleJump = false;

            jumpBufferCounter = 0f;
            coyoteTimeCounter = 0f;
            return;
        }

        // 2) Double jump (no ar)
        if (enableDoubleJump && !hasUsedDoubleJump)
        {
            float force = (doubleJumpForce > 0f) ? doubleJumpForce : jumpForce;
            JumpWithForce(force);

            hasUsedDoubleJump = true;
            jumpBufferCounter = 0f;

            // opcional: dá “peso” e garante que não herda coyote residual
            coyoteTimeCounter = 0f;
        }
    }


    // -----------------------
    // Jump Variable + Jump Cut
    // -----------------------
    private void ApplyJumpHold()
    {
        if (!isJumping) return;

        if (ShouldTreatAsGrounded())
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
        if (!IsJumpUp()) return;

        if (rb.linearVelocity.y > 0.01f)
            rb.linearVelocity = new Vector2(rb.linearVelocity.x, rb.linearVelocity.y * jumpCutMultiplier);

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
        if (isGroundedFixed) return;          // ✅ trocado
        if (rb.linearVelocity.y >= 0f) return;
        if (wallCheck == null || ledgeCheck == null) return;

        Vector2 dir = Vector2.right * facing;

        RaycastHit2D wallHit = Physics2D.Raycast(wallCheck.position, dir, wallCheckDistance, groundLayer);
        if (!wallHit) return;

        RaycastHit2D ledgeBlocked = Physics2D.Raycast(ledgeCheck.position, dir, ledgeCheckDistance, groundLayer);
        if (ledgeBlocked) return;

        Vector2 cornerProbe = new Vector2(
            wallHit.point.x + (ledgeCornerInset * facing),
            ledgeCheck.position.y
        );

        RaycastHit2D topHit = Physics2D.Raycast(cornerProbe, Vector2.down, topCheckDownDistance, groundLayer);
        if (!topHit) return;

        wallHitCached = wallHit;
        topHitCached = topHit;          // ✅ novo: cache do topo
        EnterLedgeHang();

        if (autoClimbDelay > 0f)
        {
            if (autoClimbCo != null) StopCoroutine(autoClimbCo);
            autoClimbCo = StartCoroutine(AutoClimbAfterDelay(autoClimbDelay));
        }
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

        isJumping = false;
        jumpHoldCounter = 0f;
        hasUsedDoubleJump = false;

        rb.linearVelocity = Vector2.zero;
        rb.gravityScale = 0f;

        Vector3 p = transform.position;
        p.x = wallHitCached.point.x - (0.05f * facing);
        transform.position = p;

        coyoteTimeCounter = 0f;
        jumpBufferCounter = 0f;

        ledgeCooldownTimer = Mathf.Max(ledgeCooldownTimer, ledgeHangLock);

        playerAnimator?.TriggerEdgeGrab();
        playerAnimator?.SetHanging(true);
        playerAnimator?.SetClimbing(false);
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

        // ✅ trava imediatamente (não espera o primeiro frame do coroutine)
        isClimbing = true;
        externalJumpDown = false;
        externalJumpUp = false;

        // ✅ mata auto climb pendente (pra não chamar StartClimb de novo)
        if (autoClimbCo != null)
        {
            StopCoroutine(autoClimbCo);
            autoClimbCo = null;
        }

        // ✅ se por algum motivo já tinha coroutine, garante só 1
        if (climbCo != null)
            StopCoroutine(climbCo);

        ledgeCooldownTimer = Mathf.Max(ledgeCooldownTimer, ledgeStartClimbLock);
        playerAnimator?.SetClimbing(true);

        climbCo = StartCoroutine(ClimbRoutine());
    }


    private IEnumerator ClimbRoutine()
    {
        rb.simulated = false;
        rb.linearVelocity = Vector2.zero;
        rb.gravityScale = 0f;

        // X continua usando a parede (ajuste fino com seu offset)
        float endX = wallHitCached.point.x + (climbEndOffset.x * facing);

        // Y agora usa o chão do topo + metade da altura do colisor (pé no chão)
        float endY;

        if (col != null && topHitCached)
        {
            float halfH = col.bounds.extents.y;
            endY = topHitCached.point.y + halfH + 0.02f; // 0.02 = folga pra não enroscar
        }
        else
        {
            // fallback (se algo estiver nulo)
            endY = wallHitCached.point.y + climbEndOffset.y;
        }

        Vector3 endPos = new Vector3(endX, endY, transform.position.z);


        Vector3 startPos = transform.position;

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
        climbCo = null;

        isJumping = false;
        jumpHoldCounter = 0f;

        rb.simulated = true;
        rb.gravityScale = originalGravity;
        ledgeCooldownTimer = ledgeRegrabCooldown;

        StartCoroutine(EndClimbAnimatorCleanup());
    }

    private IEnumerator EndClimbAnimatorCleanup()
    {
        yield return null; // 1 frame
        playerAnimator?.SetHanging(false);
        playerAnimator?.SetClimbing(false);
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
        hasUsedDoubleJump = false;
        isHurtLocked = false;
        CancelInvoke(nameof(ClearHurtLock));
        var hp = GetComponent<PlayerHealth>();
        if (hp != null) hp.ResetFull();
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

    private bool ShouldTreatAsGrounded()
    {
        if (rb.linearVelocity.y > ungroundedUpVelocity)
            return false;

        return isGroundedFixed;
    }

    public void SetHurtLock(float duration)
    {
        if (isDead) return;

        if (duration <= 0f)
        {
            isHurtLocked = false;
            return;
        }

        isHurtLocked = true;
        CancelInvoke(nameof(ClearHurtLock));
        Invoke(nameof(ClearHurtLock), duration);
    }

    private void ClearHurtLock()
    {
        isHurtLocked = false;
    }

    private void TryDash()
    {
        if (!enableDash) return;
        if (!IsDashDown()) return;
        if (isDead) return;
        if (isHurtLocked) return;
        if (isClimbing || isLedgeHanging) return;
        if (isDashing) return;
        if (Time.time < dashReadyAt) return;

        // se estiver atacando, opcional: bloquear dash (ou permitir)
        var combat = GetComponent<PlayerCombat>();
        if (combat != null && combat.IsAttacking) return;

        if (dashCo != null) StopCoroutine(dashCo);
        dashCo = StartCoroutine(DashRoutine());
    }

    private IEnumerator DashRoutine()
    {
        isDashing = true;

        dashReadyAt = Time.time + dashCooldown;

        // animação
        playerAnimator?.TriggerDash();

        float prevGravity = rb.gravityScale;
        if (dashNoGravity)
            rb.gravityScale = 0f;

        float dir = facing; // usa seu facing atual

        // zera Y pra não “subir/baixar” no dash
        rb.linearVelocity = new Vector2(0f, 0f);

        float t = 0f;
        while (t < dashDuration)
        {
            t += Time.deltaTime;

            // trava controle horizontal durante dash
            rb.linearVelocity = new Vector2(dir * dashSpeed, 0f);

            yield return null;
        }

        // finaliza dash
        rb.gravityScale = prevGravity;

        isDashing = false;
        dashCo = null;

        // dá uma pequena “saída” sem grudar
        if (dashLockMovement)
            rb.linearVelocity = new Vector2(0f, rb.linearVelocity.y);
    }
}
