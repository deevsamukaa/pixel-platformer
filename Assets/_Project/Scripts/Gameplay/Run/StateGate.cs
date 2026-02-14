using UnityEngine;

[RequireComponent(typeof(Collider2D))]
public class StageGate : MonoBehaviour
{
    private bool _used;

    private void Reset()
    {
        var c = GetComponent<Collider2D>();
        c.isTrigger = true;
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (_used) return;
        if (!other.CompareTag("Player")) return;

        _used = true;
        RunManager.I?.OnStageCompleted(); // sem reload! só incrementa contador/vitória
    }
}
