using UnityEngine;

public class RunExit : MonoBehaviour
{
    private bool _used;

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (_used) return;
        if (!other.CompareTag("Player")) return;

        _used = true;
        RunManager.I.OnStageCompleted();
    }
}
