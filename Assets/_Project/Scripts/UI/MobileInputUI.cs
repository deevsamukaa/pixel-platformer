using UnityEngine;

public class MobileInputUI : MonoBehaviour
{
    [Header("Buttons")]
    [SerializeField] private HoldButton leftButton;
    [SerializeField] private HoldButton rightButton;

    [Header("Target")]
    [SerializeField] private PlayerController player;

    private void Awake()
    {
        if (player == null)
            player = FindFirstObjectByType<PlayerController>();
    }

    private void Update()
    {
        if (player == null) return;

        float move = 0f;
        if (leftButton != null && leftButton.IsHeld) move -= 1f;
        if (rightButton != null && rightButton.IsHeld) move += 1f;

        player.SetMoveInput(move);
    }

    // Chame isso no OnClick do botão Jump
    public void JumpPressed()
    {
        if (player == null) return;
        player.JumpPressed();
    }
}
