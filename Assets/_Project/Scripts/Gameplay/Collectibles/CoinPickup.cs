using UnityEngine;

[RequireComponent(typeof(Collider2D))]
public class CoinPickup : MonoBehaviour
{
    [SerializeField] private int value = 1;
    [SerializeField] private bool destroyOnCollect = true;

    private void Reset()
    {
        var col = GetComponent<Collider2D>();
        col.isTrigger = true;
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!other.CompareTag("Player")) return;

        CoinManager.Instance?.Add(value);

        if (destroyOnCollect)
            Destroy(transform.parent.gameObject);
        else
            gameObject.SetActive(false);
    }
}

