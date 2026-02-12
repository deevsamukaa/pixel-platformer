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
    private bool isHoldingDownMobile;

    // Para resetar timer quando o estado muda
    private bool wasHoldingCombined;

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
        if (!Application.isFocused)
        {
            ResetLook();
            return;
        }

        if (player != null && (!player.IsGrounded || player.IsClimbingOrHanging))
        {
            ResetLook();
            return;
        }

        bool downPressedKeyboard = Input.GetKey(KeyCode.S) || Input.GetKey(KeyCode.DownArrow);
        bool holdingCombined = isHoldingDownMobile || downPressedKeyboard;

        // Se começou/parou de segurar, reseta o timer pra não “herdar” meio segundo
        if (holdingCombined != wasHoldingCombined)
        {
            holdTimer = 0f;
            wasHoldingCombined = holdingCombined;
        }

        if (holdingCombined)
        {
            holdTimer += Time.deltaTime;

            float targetY = (holdTimer >= holdTime)
                ? defaultOffsetY + lookDownOffsetY
                : defaultOffsetY;

            SmoothTo(targetY);
        }
        else
        {
            ResetLook();
        }
    }

    public void SetHoldingDown(bool holding)
    {
        isHoldingDownMobile = holding;
    }

    private void ResetLook()
    {
        holdTimer = 0f;
        SmoothTo(defaultOffsetY);
    }

    private void SmoothTo(float targetY)
    {
        float newY = Mathf.Lerp(composer.TargetOffset.y, targetY, Time.deltaTime * smoothSpeed);

        // Clamp pra garantir que não extrapola por drift
        float minY = defaultOffsetY + lookDownOffsetY;
        float maxY = defaultOffsetY;
        newY = Mathf.Clamp(newY, minY, maxY);

        composer.TargetOffset = new Vector3(composer.TargetOffset.x, newY, composer.TargetOffset.z);
    }
}