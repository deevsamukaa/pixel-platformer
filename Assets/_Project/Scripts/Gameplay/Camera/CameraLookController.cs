using UnityEngine;
using Unity.Cinemachine;

public class CameraLookController : MonoBehaviour
{
    [Header("Refs")]
    [SerializeField] private CinemachineCamera cinemachineCamera;
    [SerializeField] private PlayerController player;

    [Header("Tuning")]
    [SerializeField] private float holdTime = 0.5f;
    [SerializeField] private float lookDownOffsetY = -0.3f;
    [SerializeField] private float smoothSpeed = 6f;

    private CinemachinePositionComposer composer;

    private float defaultOffsetY;
    private float holdTimer;

    // vem do MobileInputUI
    private bool isHoldingDown;

    private void Awake()
    {
        if (cinemachineCamera == null)
            cinemachineCamera = GetComponent<CinemachineCamera>();

        composer = cinemachineCamera.GetComponent<CinemachinePositionComposer>();

        if (composer == null)
        {
            Debug.LogError("[CameraLookController] CinemachinePositionComposer não encontrado.");
            enabled = false;
            return;
        }

        defaultOffsetY = composer.TargetOffset.y;
    }

    private void Update()
    {
        if (player != null)
        {
            if (!player.IsGrounded || player.IsClimbingOrHanging)
            {
                holdTimer = 0f;
                SmoothTo(defaultOffsetY);
                return;
            }
        }

        bool downPressed = Input.GetKey(KeyCode.S) || Input.GetKey(KeyCode.DownArrow);

        if (isHoldingDown || downPressed)
        {
            holdTimer += Time.deltaTime;
            float targetY = (holdTimer >= holdTime) ? defaultOffsetY + lookDownOffsetY : defaultOffsetY;
            SmoothTo(targetY);
        }
        else
        {
            holdTimer = 0f;
            SmoothTo(defaultOffsetY);
        }
    }

    public void SetHoldingDown(bool holding)
    {
        isHoldingDown = holding;
    }

    private void SmoothTo(float targetY)
    {
        float newY = Mathf.Lerp(composer.TargetOffset.y, targetY, Time.deltaTime * smoothSpeed);
        composer.TargetOffset = new Vector3(composer.TargetOffset.x, newY, composer.TargetOffset.z);
    }
}