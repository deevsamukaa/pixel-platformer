using UnityEngine;

public class PlayerSquashStretch : MonoBehaviour
{
    [Header("Refs")]
    [SerializeField] private Transform visualRoot;
    [SerializeField] private PlayerController controller;
    [SerializeField] private Rigidbody2D rb;

    [Header("Tuning")]
    [Tooltip("Quanto estica ao iniciar o pulo (Y > 1).")]
    [SerializeField] private float jumpStretchY = 1.08f;
    [Tooltip("Quanto afina ao iniciar o pulo (X < 1).")]
    [SerializeField] private float jumpStretchX = 0.94f;
    [Tooltip("Quanto 'amassa' ao aterrissar (Y < 1).")]
    [SerializeField] private float landSquashY = 0.90f;
    [Tooltip("Quanto 'alarga' ao aterrissar (X > 1).")]
    [SerializeField] private float landSquashX = 1.08f;

    [Tooltip("Velocidade do lerp pra chegar no alvo.")]
    [SerializeField] private float respondSpeed = 18f;
    [Tooltip("Velocidade do retorno pro normal.")]
    [SerializeField] private float returnSpeed = 14f;

    [Header("Detection")]
    [Tooltip("Velocidade mínima pra considerar que foi um pulo (evita acionar em degraus).")]
    [SerializeField] private float jumpVSpeedThreshold = 0.8f;
    [Tooltip("Velocidade mínima de queda pra aplicar squash ao aterrissar.")]
    [SerializeField] private float landVSpeedThreshold = -2.5f;

    private Vector3 _targetScale = Vector3.one;

    private bool _wasGrounded;
    private bool _didFallMeaningfully;
    private float _lastYVel;

    private void Awake()
    {
        if (controller == null) controller = GetComponent<PlayerController>();
        if (rb == null) rb = GetComponent<Rigidbody2D>();

        if (visualRoot == null)
            visualRoot = transform.Find("Visual"); // seu padrão já usa "Visual"
    }

    private void Update()
    {
        if (visualRoot == null || controller == null || rb == null) return;

        bool grounded = controller.IsGrounded;

        // Detecta "início do pulo": saiu do chão com velocidade pra cima
        if (_wasGrounded && !grounded && rb.linearVelocity.y > jumpVSpeedThreshold)
        {
            _targetScale = new Vector3(jumpStretchX, jumpStretchY, 1f);
            _didFallMeaningfully = false;
        }

        // Marca que teve queda (pra só squash se foi queda de verdade)
        if (!grounded && rb.linearVelocity.y < landVSpeedThreshold)
            _didFallMeaningfully = true;

        // Detecta aterrissagem
        if (!_wasGrounded && grounded)
        {
            if (_didFallMeaningfully || _lastYVel < landVSpeedThreshold)
            {
                _targetScale = new Vector3(landSquashX, landSquashY, 1f);
            }
            else
            {
                // aterrissagem leve -> não squash
                _targetScale = Vector3.one;
            }
        }

        // No ar subindo/descendo: volta pro normal gradualmente (pra não ficar preso esticado)
        if (!grounded && rb.linearVelocity.y <= 0.1f)
        {
            // ao começar a cair, relaxa
            _targetScale = Vector3.one;
        }

        // Interpola escala
        float speed = (_targetScale == Vector3.one) ? returnSpeed : respondSpeed;
        visualRoot.localScale = Vector3.Lerp(visualRoot.localScale, _targetScale, speed * Time.deltaTime);

        // Se já chegou perto e o target era squash/esticar, retorna ao normal automaticamente
        if (_targetScale != Vector3.one)
        {
            if ((visualRoot.localScale - _targetScale).sqrMagnitude < 0.0001f)
                _targetScale = Vector3.one;
        }

        _lastYVel = rb.linearVelocity.y;
        _wasGrounded = grounded;
    }

    private void OnDisable()
    {
        if (visualRoot != null)
            visualRoot.localScale = Vector3.one;
    }
}
