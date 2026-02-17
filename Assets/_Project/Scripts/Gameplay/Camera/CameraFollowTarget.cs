using UnityEngine;

/// <summary>
/// Um alvo intermediário para a câmera seguir.
/// Segue o player com suavização opcional.
/// Isso permite fazer "portal blend" e outras correções sem brigar com o CinemachineBrain.
/// </summary>
public class CameraFollowTarget : MonoBehaviour
{
    [Header("Refs")]
    [SerializeField] private Transform player;

    [Header("Follow")]
    [Tooltip("Se 0, segue cravado no player. Se > 0, suaviza.")]
    [SerializeField] private float followSmooth = 0.0f;

    [Tooltip("Offset aplicado ao alvo (geralmente 0).")]
    [SerializeField] private Vector3 offset = Vector3.zero;

    private Vector3 _vel;

    public Transform Player => player;

    private void Awake()
    {
        if (player == null)
            player = GameObject.FindGameObjectWithTag("Player")?.transform;

        if (player != null)
            transform.position = player.position + offset;
    }

    private void LateUpdate()
    {
        if (player == null) return;

        Vector3 target = player.position + offset;

        if (followSmooth <= 0f)
        {
            transform.position = target;
            return;
        }

        transform.position = Vector3.SmoothDamp(
            transform.position,
            target,
            ref _vel,
            followSmooth,
            Mathf.Infinity,
            Time.unscaledDeltaTime
        );
    }

    // Usado pelo CameraBoundsManager durante o portal blend
    public void ForcePosition(Vector3 worldPos)
    {
        transform.position = worldPos;
        _vel = Vector3.zero;
    }
}
