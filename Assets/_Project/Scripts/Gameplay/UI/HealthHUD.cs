using TMPro;
using UnityEngine;

public class HealthHUD : MonoBehaviour
{
    [Header("UI")]
    [SerializeField] private TextMeshProUGUI hpText;

    [Header("Target")]
    [SerializeField] private PlayerHealth playerHealth;

    private void Awake()
    {
        if (hpText == null)
            hpText = GetComponentInChildren<TextMeshProUGUI>();
    }

    private void Start()
    {
        // Auto-find (sem setup chato)
        if (playerHealth == null)
            playerHealth = FindFirstObjectByType<PlayerHealth>();
    }

    private void Update()
    {
        if (playerHealth == null)
        {
            // Se o player ainda não existia (ou respawn recriou), tenta achar de novo
            playerHealth = FindFirstObjectByType<PlayerHealth>();
            if (playerHealth == null) return;
        }

        hpText.text = $"{playerHealth.CurrentHP}/{playerHealth.MaxHP}";
    }
}
