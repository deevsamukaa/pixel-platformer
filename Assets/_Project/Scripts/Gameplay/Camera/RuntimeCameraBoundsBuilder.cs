using System.Collections.Generic;
using UnityEngine;
using Unity.Cinemachine;

/// <summary>
/// Cria um "mega bound" runtime (CompositeCollider2D) a partir de múltiplos PolygonCollider2D.
/// O Confiner sempre aponta para o Composite.
/// </summary>
public class RuntimeCameraBoundsBuilder : MonoBehaviour
{
    [Header("Confiner")]
    [SerializeField] private CinemachineConfiner2D confiner;

    [Header("Runtime Composite Root")]
    [Tooltip("Este objeto precisa ter Rigidbody2D (Static) + CompositeCollider2D.")]
    [SerializeField] private CompositeCollider2D runtimeComposite;

    // cache para evitar rebuild desnecessário
    private readonly List<PolygonCollider2D> _currentSources = new();

    private void Awake()
    {
        if (confiner == null)
            confiner = GetComponent<CinemachineConfiner2D>();

        if (runtimeComposite == null)
            runtimeComposite = GetComponentInChildren<CompositeCollider2D>(true);

        if (confiner == null)
            Debug.LogError("[RuntimeCameraBoundsBuilder] CinemachineConfiner2D não encontrado.");

        if (runtimeComposite == null)
            Debug.LogError("[RuntimeCameraBoundsBuilder] CompositeCollider2D runtime não encontrado.");

        // Confiner sempre aponta para o composite runtime
        if (confiner != null && runtimeComposite != null)
        {
            confiner.BoundingShape2D = runtimeComposite;
            confiner.InvalidateCache();
        }
    }

    public void SetSources(IReadOnlyList<PolygonCollider2D> sources)
    {
        // compara listas para só rebuildar se mudou
        if (SameSources(sources)) return;

        _currentSources.Clear();
        for (int i = 0; i < sources.Count; i++)
            if (sources[i] != null) _currentSources.Add(sources[i]);

        Rebuild();
    }

    private bool SameSources(IReadOnlyList<PolygonCollider2D> sources)
    {
        if (sources == null) return _currentSources.Count == 0;
        if (_currentSources.Count != sources.Count) return false;

        for (int i = 0; i < sources.Count; i++)
            if (_currentSources[i] != sources[i]) return false;

        return true;
    }

    private void Rebuild()
    {
        if (runtimeComposite == null) return;

        // Apaga tiles anteriores
        for (int i = runtimeComposite.transform.childCount - 1; i >= 0; i--)
            Destroy(runtimeComposite.transform.GetChild(i).gameObject);

        // Cria tiles a partir das fontes
        for (int i = 0; i < _currentSources.Count; i++)
            CreateTileFromSource(_currentSources[i], i);

        // Força atualização dos colliders
        Physics2D.SyncTransforms();

        if (confiner != null)
            confiner.InvalidateCache();
    }

    private void CreateTileFromSource(PolygonCollider2D src, int index)
    {
        if (src == null) return;

        // filho que vai alimentar o composite
        var go = new GameObject($"BoundsTile_{index}");
        go.transform.SetParent(runtimeComposite.transform, false);

        // IMPORTANTE: manter o runtimeComposite em scale (1,1,1) e rot 0.
        // Assim, aqui replicamos o "mundo" do src sem distorções.
        go.transform.position = src.transform.position;
        go.transform.rotation = src.transform.rotation;
        go.transform.localScale = src.transform.lossyScale;

        var poly = go.AddComponent<PolygonCollider2D>();
        poly.isTrigger = true;
        poly.usedByComposite = true;

        // copia offset + paths
        poly.offset = src.offset;
        poly.pathCount = src.pathCount;
        for (int p = 0; p < src.pathCount; p++)
            poly.SetPath(p, src.GetPath(p));
    }
}
