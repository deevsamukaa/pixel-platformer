using UnityEngine;

public class HitVFXSpawner : MonoBehaviour
{
    public static HitVFXSpawner I { get; private set; }

    [SerializeField] private GameObject defaultHitVfxPrefab;
    [SerializeField] private float defaultLifetime = 1.5f;

    private void Awake()
    {
        if (I != null) { Destroy(gameObject); return; }
        I = this;
        DontDestroyOnLoad(gameObject);
    }

    public void Spawn(GameObject prefab, Vector2 position, Vector2 normal, float lifetime = -1f)
    {
        var p = prefab != null ? prefab : defaultHitVfxPrefab;
        if (p == null) return;

        float life = lifetime > 0f ? lifetime : defaultLifetime;

        // rotaciona “de leve” na direção da normal (opcional)
        Quaternion rot = normal.sqrMagnitude > 0.0001f
            ? Quaternion.LookRotation(Vector3.forward, normal)
            : Quaternion.identity;

        var vfx = Instantiate(p, position, rot);
        Destroy(vfx, life);
    }
}
