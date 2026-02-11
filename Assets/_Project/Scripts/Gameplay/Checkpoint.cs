using UnityEngine;

[RequireComponent(typeof(Collider2D))]
public class Checkpoint : MonoBehaviour
{
    [Header("Spawn")]
    [SerializeField] private Transform spawnMarker; // opcional: onde exatamente o player vai nascer

    [Header("Visual Feedback (opcional)")]
    [SerializeField] private GameObject inactiveVisual;
    [SerializeField] private GameObject activeVisual;

    private bool activated;

    private void Reset()
    {
        // garantir trigger
        var col = GetComponent<Collider2D>();
        col.isTrigger = true;
    }

    private void Awake()
    {
        SetVisual(false);
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (activated) return;

        PlayerController player = other.GetComponentInParent<PlayerController>();
        if (player == null) return;

        Vector3 spawnPos = spawnMarker != null ? spawnMarker.position : transform.position;
        player.SetSpawnPosition(spawnPos);

        activated = true;
        SetVisual(true);
    }

    private void SetVisual(bool isActive)
    {
        if (inactiveVisual != null) inactiveVisual.SetActive(!isActive);
        if (activeVisual != null) activeVisual.SetActive(isActive);
    }
}

