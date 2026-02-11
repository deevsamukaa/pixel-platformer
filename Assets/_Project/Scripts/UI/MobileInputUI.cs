using UnityEngine;

public class MobileInputUI : MonoBehaviour
{
    [Header("Buttons")]
    [SerializeField] private HoldButton leftButton;
    [SerializeField] private HoldButton rightButton;
    [SerializeField] private HoldButton downButton;

    [Header("Target")]
    [SerializeField] private PlayerController player;

    [Header("Camera")]
    [SerializeField] private CameraLookController cameraLook;

    private void Awake()
    {
        if (player == null)
            player = FindFirstObjectByType<PlayerController>();

        if (cameraLook == null)
            cameraLook = FindFirstObjectByType<CameraLookController>();
    }

    private void Update()
    {
        if (player != null)
        {
            float move = 0f;
            if (leftButton != null && leftButton.IsHeld) move -= 1f;
            if (rightButton != null && rightButton.IsHeld) move += 1f;

            player.SetMoveInput(move);
        }

        // 👇 Olhar pra baixo (câmera)
        if (cameraLook != null && downButton != null)
        {
            cameraLook.SetHoldingDown(downButton.IsHeld);
        }
    }

    public void JumpPressed()
    {
        if (player == null) return;
        player.JumpPressed();
    }

    public void JumpReleased()
    {
        if (player == null) return;
        player.JumpReleased();
    }
}
