using UnityEngine;

/// <summary>
/// Coloque no GameObject "CameraBounds" (PolygonCollider2D) dentro do prefab do Segment.
/// </summary>
[RequireComponent(typeof(PolygonCollider2D))]
public class CameraBoundsProvider : MonoBehaviour
{
    public PolygonCollider2D Poly => GetComponent<PolygonCollider2D>();
}
